using System;
using System.Collections.Generic;
using CompetentAI.Core;
using ThunderRoad;
using ThunderRoad.AI.Action;
using UnityEngine;

namespace CompetentAI.Mod
{
    /// <summary>
    /// Keeps the creature on the offensive: attacks that are not cut short when the opponent parries them (the stock recoil
    /// is what ends most chains), no hesitation caused by the attack node's "defence preference", and reactive dodges
    /// to strikes at the legs and straight thrusts. Also logs every hit the creature takes, to diagnose its defence.
    /// </summary>
    internal sealed partial class BrainModuleCompetent
    {
        private const float NoRecoil = 99f;

        private readonly List<AttackMelee> _attackNodes = new List<AttackMelee>(4);
        private readonly Dictionary<AttackMelee, bool> _nodeOriginal = new Dictionary<AttackMelee, bool>();
        private float _nextNodeScan;
        private float _nextNodeRoll;
        private bool _nodeOverrideLogged;
        private float _lastReactAt = -10f;

        /// <summary>Called when an attack starts: decides whether a parry may interrupt it.</summary>
        private void ArmRecoilGuard(bool followUp)
        {
            if (!_origCaptured) return;
            bool resist = followUp || _kataActive || _rng.Value() < _profile.ParryRecoilResist;
            _melee.recoilMinWeaponSpeed = resist ? NoRecoil : _orig.recoilMin;
        }

        /// <summary>Back to the stock recoil threshold (it also gates the creature's own riposte after ITS parries).</summary>
        private void DisarmRecoilGuard()
        {
            if (_origCaptured && !_kataExpectStep) _melee.recoilMinWeaponSpeed = _orig.recoilMin;
        }

        private void AggressionTick(float now)
        {
            if (!_tuned || _profile.AttackDespiteDanger <= 0f) return;
            if (now >= _nextNodeScan)
            {
                _nextNodeScan = now + 3f; // some child trees are created lazily, so look again from time to time
                TreeTweaks.FindAttackNodes(_instance.tree, _attackNodes);
                foreach (AttackMelee n in _attackNodes)
                    if (!_nodeOriginal.ContainsKey(n)) _nodeOriginal[n] = n.defensePreferenceChance;
            }
            if (now >= _nextNodeRoll && _attackNodes.Count > 0)
            {
                _nextNodeRoll = now + 0.5f;
                bool ignore = _rng.Value() < _profile.AttackDespiteDanger;
                foreach (AttackMelee n in _attackNodes) n.defensePreferenceChance = !ignore && _nodeOriginal[n];
                if (!_nodeOverrideLogged)
                {
                    _nodeOverrideLogged = true;
                    Log.Debug_($"{_tag} attack-despite-danger active on {_attackNodes.Count} attack node(s), chance {_profile.AttackDespiteDanger:0.00}");
                }
            }
        }

        private void RestoreDefensePreference()
        {
            foreach (var kv in _nodeOriginal)
                if (kv.Key != null) kv.Key.defensePreferenceChance = kv.Value;
        }

        /// <summary>Dodge reactively to leg strikes and straight thrusts that are about to land.</summary>
        private void ReactiveDodge(float now, ParryTarget.ParryInfo info)
        {
            if (info.parryTarget == null || !info.onHitCourse || now - _lastReactAt < 0.4f) return;
            if (now < _defense.nextAllowedDodgeTime || !_defense.canDodge || _brain.isAttacking) return;
            float speed = info.targetPointVelocity.magnitude;
            if (speed < Runtime.Cfg.Defense.MinStrikeSpeed) return;
            float timeToImpact = info.distance / Mathf.Max(speed, 0.01f);
            if (timeToImpact > _defense.aboutToBeHitTiming) return;

            float eye = Mathf.Max(0.5f, creature.transform.InverseTransformPoint(creature.centerEyes.position).y);
            float height = (info.colliderPoint.y - creature.transform.position.y) / eye;
            bool low = height < 0.5f;
            bool thrust = Mathf.Abs(Vector3.Dot(info.targetPointVelocity.normalized, info.parryTarget.transform.up)) > 0.8f;
            float chance = Mathf.Max(low ? _profile.LegDodgeChance : 0f, thrust ? _profile.ThrustDodgeChance : 0f);
            if (chance <= 0f) return;

            _lastReactAt = now; // one decision per strike
            if (_rng.Value() >= chance) return;
            Log.Defense($"{_tag} reactive dodge ({(low ? "leg strike" : "")}{(low && thrust ? " + " : "")}{(thrust ? "thrust" : "")}, height {height:0.00}, impact in {timeToImpact:0.00}s)");
            _defense.TryToDodge(info);
        }

        private void OnDamaged(CollisionInstance collision, EventTime time)
        {
            if (time != EventTime.OnEnd || collision == null || _disabled) return;
            try
            {
                float damage = collision.damageStruct.damage;
                if (damage <= 0f) return;
                Damager damager = collision.damageStruct.damager;
                string kind = damager != null ? damager.type.ToString() : "?";
                float eye = Mathf.Max(0.5f, creature.transform.InverseTransformPoint(creature.centerEyes.position).y);
                float height = (collision.contactPoint.y - creature.transform.position.y) / eye;
                Log.Defense($"{_tag} HIT {damage:0.0} ({kind}) at height {height:0.00} (guard {creature.animator.GetFloat(Brain.hashBlock):0.00}, dodging={_brain.isDodging}, attacking={_brain.isAttacking}, overwhelmed={_fatigueApplied})");
            }
            catch (Exception ex)
            {
                Log.Debug_(_tag + " hit log failed: " + ex.Message);
            }
        }
    }
}
