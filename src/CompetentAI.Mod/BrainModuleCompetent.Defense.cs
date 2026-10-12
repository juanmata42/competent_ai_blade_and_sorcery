using System;
using CompetentAI.Core;
using ThunderRoad;
using UnityEngine;

namespace CompetentAI.Mod
{
    internal sealed partial class BrainModuleCompetent
    {
        private FeintReader _reader;
        private ParryFatigue _fatigue;
        private StrikeTracker _tracker;
        private bool _wasDodging;
        private bool _fatigueApplied;
        private float _lastDangerTime;
        private float _lastParryEvent = -10f;

        /// <summary>
        /// Runs right after the stock defence module in the same fixed step. Three jobs:
        /// 1) fatigue (a creature that parries too much in a short time is overwhelmed for a moment),
        /// 2) feint reading (a sharp change of the strike direction either fools the guard or is read in time),
        /// 3) bookkeeping for the counter-attack that follows a dodge.
        /// </summary>
        private void DefenseTick(float now)
        {
            if (_defense == null || _detection == null) return;
            bool reading = Runtime.Flags.DefenseReading;

            // 1) fatigue gate on parrying
            if (reading && _fatigue.IsOverwhelmed(now))
            {
                // The behaviour tree rewrites parryEnabled every AI cycle (SetDefenseState), so keep asserting it.
                if (!_fatigueApplied)
                {
                    _fatigueApplied = true;
                    Log.Defense($"{_tag} overwhelmed: parry disabled for a moment");
                }
                _defense.parryEnabled = false;
            }
            else if (_fatigueApplied)
            {
                _fatigueApplied = false;
                if (!_defense.parryEnabled) _defense.parryEnabled = _origCaptured ? _orig.parryEnabled : true;
            }

            // 3) dodge end → counter window
            bool dodging = _brain.isDodging;
            if (_wasDodging && !dodging)
            {
                _counterUntil = now + Runtime.Cfg.Defense.CounterWindow;
                _counterRolled = false;
            }
            _wasDodging = dodging;

            if (!reading) return;

            // 2) feint reading
            if (_detection.attackDangerLevel > 0f)
            {
                _lastDangerTime = now;
                ParryTarget.ParryInfo info = _detection.attackInfoRight;
                if (_detection.attackInfoLeft.parryTarget != null &&
                    (info.parryTarget == null || _detection.attackInfoLeft.targetPointVelocity.sqrMagnitude > info.targetPointVelocity.sqrMagnitude))
                    info = _detection.attackInfoLeft;

                Vector3 toDefender = creature.transform.position - info.targetPoint;
                if (info.parryTarget != null &&
                    _tracker.Update(now, ToVec(info.targetPointVelocity), ToVec(toDefender), Runtime.Cfg.Defense, out float angle))
                {
                    FeintOutcome outcome = _reader.OnReversal(now);
                    Log.Defense($"{_tag} strike changed direction by {angle:0}° -> {outcome}");
                    if (outcome == FeintOutcome.Fooled && _reader.RollFooledDodge() && now >= _defense.nextAllowedDodgeTime && _defense.canDodge)
                    {
                        Log.Defense($"{_tag} fooled: commits to a dodge");
                        _defense.TryToDodge(info);
                    }
                }
            }
            else if (now - _lastDangerTime > 0.4f)
            {
                _tracker.Reset();
            }

            float baseAdjust = _tuned ? _baseAdjust : _orig.parryAdjust;
            _defense.parryAdjustSpeed = baseAdjust * _reader.SpeedFactor(now);
        }

        /// <summary>Called by the script when this creature's weapon successfully parried an enemy attack.</summary>
        public void OnParrySuccess()
        {
            if (!_loaded || _disabled || !Runtime.Flags.Enabled) return;
            _lastParryTime = Time.time; // used by the pressure bias of the attack director
            if (!Runtime.Flags.DefenseReading) return;
            try
            {
                float t = Time.time;
                if (t - _lastParryEvent < 0.15f) return; // one parry raises several collision events
                _lastParryEvent = t;
                if (_fatigue.RegisterParry(t))
                    Log.Defense($"{_tag} has parried too much in a short time (fatigue)");
            }
            catch (Exception ex)
            {
                Fail("OnParrySuccess", ex);
            }
        }

        private static Vec3 ToVec(Vector3 v) => new Vec3(v.x, v.y, v.z);
    }
}
