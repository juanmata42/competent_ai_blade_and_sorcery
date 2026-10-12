using System;
using System.Collections.Generic;
using CompetentAI.Core;
using ThunderRoad;
using UnityEngine;

namespace CompetentAI.Mod
{
    /// <summary>
    /// Chained strike sequences ("katas"): several attacks thrown back to back on the assumption that the first one will be
    /// parried, each follow-up chosen so that it flows from the previous one, and every follow-up interruptible when the
    /// opponent becomes dangerous. A sequence ends on stagger, on a body hit the stance does not chain on, when the target is
    /// gone, or when the creature breaks off to defend.
    /// </summary>
    internal sealed partial class BrainModuleCompetent
    {
        private bool _kataActive;
        private int _kataStep;        // 0 = the opening strike
        private int _kataRemaining;   // follow-ups still to throw
        private bool _kataExpectStep; // the next WindUp is one of our follow-ups
        private readonly InterruptGate _gate = new InterruptGate();
        private float _lastParryTime = -10f;

        private bool KataWindUp(AttackMotion atk, Creature target, out bool isFollowUp)
        {
            isFollowUp = _kataExpectStep;
            if (_kataExpectStep)
            {
                _kataExpectStep = false;
                _kataStep++;
                return true;
            }

            // A fresh attack that is not one of our follow-ups: maybe start a new sequence.
            _kataActive = false;
            if (!Runtime.Flags.Combos || target == null || target.state != Creature.State.Alive) return false;
            if (!KataPlanner.ShouldStart(_profile, _rng)) return false;
            int length = KataPlanner.PlanLength(_profile, Runtime.Cfg.Katas, _rng);
            _kataActive = true;
            _kataStep = 0;
            _kataRemaining = length - 1;
            _gate.Reset();
            Log.Info($"{_tag} kata planned: {length} strikes, opening with {atk.id} (profile {_profileName})");
            Hud.Show(creature, $"[{_profileName}] sequence of {length} strikes: {atk.id}");
            return true;
        }

        /// <summary>Called from the End stage of every attack. Returns true when the sequence handled it.</summary>
        private bool KataAttackEnd(AttackMotion atk, Creature target)
        {
            if (!_kataActive || atk == null) return false;
            if (_kataRemaining <= 0) { EndKata("completed"); return true; }
            if (target == null || target.state != Creature.State.Alive) { EndKata("target gone"); return true; }
            if (_melee.attack != atk || !_brain.isAttacking || _brain.currentStagger != Brain.Stagger.None) { EndKata("stopped by the game"); return true; }

            MeleeStanceData md = _stance.currentStanceData as MeleeStanceData;
            if (_melee.lastHitType == BrainModuleMelee.HitType.Body && md != null && md.chainAttacksOnBody == MeleeStanceData.BodyChainBehaviour.Disabled)
            {
                EndKata("hit the body (this stance does not chain on bodies)");
                return true;
            }
            if (InterruptNow(Time.time)) { EndKata("interrupted: the opponent became dangerous"); return true; }

            AttackMotion next = ChooseKataNext(atk, target);
            if (next == null) { EndKata("no flowing follow-up in range"); return true; }

            _kataRemaining--;
            _kataExpectStep = true;
            Log.Info($"{_tag} kata step {_kataStep + 1}: {atk.id} -> {next.id}");
            Hud.Show(creature, $"[{_profileName}] strike {_kataStep + 2}: {next.id}");
            // The damage window of this strike just ended, so its coroutine is finishing on its own: no need to stop it.
            ChainInto(atk, next, target.transform, true, false);
            if (_kataExpectStep)
            {
                _kataExpectStep = false;
                EndKata("follow-up could not start");
            }
            return true;
        }

        private void KataTick(float now)
        {
            if (!_kataActive) return;
            if (!Runtime.Flags.Combos) { EndKata("module off"); return; }
            if (!_brain.isAttacking && !_kataExpectStep)
            {
                EndKata("attack finished");
                return;
            }
            // A follow-up still winding up (no damage yet) can be abandoned at will: this is what makes a long sequence safe.
            if (_kataStep >= 1 && _brain.isAttacking && !_brain.canDamage && _melee.attack != null && InterruptNow(now))
            {
                AttackMotion current = _melee.attack;
                EndKata("interrupted during wind-up of " + current.id);
                AbortAttack(current);
            }
        }

        private bool InterruptNow(float now)
        {
            return _detection != null && _gate.Evaluate(now, _detection.attackDangerLevel, _profile, _rng);
        }

        private void EndKata(string why)
        {
            if (_kataActive)
            {
                Log.Info($"{_tag} kata ended after {_kataStep + 1} strike(s): {why}");
                if (why.StartsWith("interrupted")) Hud.Show(creature, $"[{_profileName}] breaks off the sequence to defend");
            }
            _kataActive = false;
            _kataExpectStep = false;
            _kataRemaining = 0;
        }

        private AttackMotion ChooseKataNext(AttackMotion prevMotion, Creature target)
        {
            if (_curatedData == null) return null;
            float distance = DistanceTo(target);
            float tol = Runtime.Cfg.Katas.StepRangeTolerance;
            string poseId = _stance.currentPose != null ? _stance.currentPose.id : "";
            TryInfo(prevMotion, out MotionInfo prevInfo);

            // The follow-up the game's authors paired with the previous attack, if any.
            MeleeStanceData md = _stance.currentStanceData as MeleeStanceData;
            AttackMotion authored = prevMotion.GetChainAttack(poseId, prevMotion.attackSide, md != null && md.alternateAttackSides);

            _candScratch.Clear();
            foreach (AttackMotion m in _curatedData.chainAttacks)
            {
                if (m.animationClip == null || (m.sourceIdle != "Any" && m.sourceIdle != poseId)) continue;
                if (!TryInfo(m, out MotionInfo mi) || mi.Candidate.Kind == AttackKind.Bash) continue;
                StanceCurator.RefreshDirection(mi, Runtime.Dirs);
                mi.Candidate.IsChain = true;
                mi.Candidate.IsAuthoredFollowUp = authored != null && authored.id == m.id;
                mi.Candidate.Weight = mi.BaseWeight;
                _candScratch.Add(mi.Candidate);
            }
            foreach (AttackMotion m in _curatedData.openingAttacks)
            {
                if (m.animationClip == null || (m.sourceIdle != "Any" && m.sourceIdle != poseId)) continue;
                if (!TryInfo(m, out MotionInfo mi) || mi.Candidate.Kind == AttackKind.Bash) continue;
                if (!RangeFits(m, distance, tol)) continue;
                StanceCurator.RefreshDirection(mi, Runtime.Dirs);
                mi.Candidate.IsChain = false;
                mi.Candidate.IsAuthoredFollowUp = false;
                mi.Candidate.Weight = mi.BaseWeight;
                _candScratch.Add(mi.Candidate);
            }

            int idx = KataPlanner.ChooseNext(prevInfo != null ? prevInfo.Candidate : null, _candScratch, _memory,
                Runtime.Cfg.Variety, Runtime.Cfg.Katas, _profile, true, _rng);
            return idx < 0 ? null : (AttackMotion)_candScratch[idx].Tag;
        }
    }
}
