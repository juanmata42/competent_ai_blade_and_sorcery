using System;
using CompetentAI.Core;
using ThunderRoad;
using UnityEngine;
using CoreZone = CompetentAI.Core.Zone;

namespace CompetentAI.Mod
{
    /// <summary>
    /// Aims at the gaps in the opponent's guard (where its weapon and shield are NOT) and does the classic cut-then-thrust:
    /// a cut that gets parried turns into a thrust that slips past the parry.
    /// </summary>
    internal sealed partial class BrainModuleCompetent
    {
        private readonly GuardMap _guard = new GuardMap();

        // cut -> thrust on parry
        private bool _mutationArmed;
        private int _mutationCount;
        private AttackMotion _mutationFrom;
        private AttackMotion _mutationNext;
        private float _lastMutation = -100f;
        private AttackMotion _ignoreEndFor;
        private float _ignoreEndUntil;

        /// <summary>Maps the target's held weapon(s) onto the nine zones. Leaves the map empty (= no steering) when unknown.</summary>
        private void ReadGuard(Creature target)
        {
            _guard.Clear();
            if (!Runtime.Flags.Openings || target == null || target.equipment == null) return;
            OpeningConfig cfg = Runtime.Cfg.Openings;
            float eye = Mathf.Max(0.5f, target.transform.InverseTransformPoint(target.centerEyes.position).y);
            Item right = target.equipment.GetHeldWeapon(Side.Right);
            Item left = target.equipment.GetHeldWeapon(Side.Left);
            AddGuardSource(target, right, eye, cfg);
            if (left != null && left != right) AddGuardSource(target, left, eye, cfg);
        }

        private void AddGuardSource(Creature target, Item item, float eye, OpeningConfig cfg)
        {
            if (item == null) return;
            bool shield = item.data != null && item.data.type == ItemData.Type.Shield;
            Vector3 world;
            if (shield)
            {
                world = item.transform.position;
            }
            else
            {
                item.GetFurthestDamagerCollider(out Damager d, out Collider c);
                world = c != null ? c.transform.position : item.transform.position;
            }
            Vector3 local = target.transform.InverseTransformPoint(world);
            if (local.z < 0.05f) return; // behind the body: not a guard
            float x = local.x / 0.5f;
            float y = (local.y - 0.7f * eye) / (0.3f * eye);
            CoreZone zone = Zones.FromNormalized(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
            if (shield) _guard.AddShield(zone, cfg);
            else _guard.AddWeapon(zone, cfg);
        }

        /// <summary>Weight multiplier of a candidate given the guard read by <see cref="ReadGuard"/> (1 when the module is off).</summary>
        private float GuardFactor(AttackCandidate c)
        {
            return Runtime.Flags.Openings ? OpeningPlanner.WeightFactor(c, _guard, _profile, Runtime.Cfg.Openings) : 1f;
        }

        // ---------------------------------------------------------------- cut -> thrust

        /// <summary>Called when a cut starts: rolls whether this cut will turn into a thrust if it gets parried.</summary>
        private void ArmMutation(MotionInfo mi)
        {
            _mutationArmed = false;
            if (!Runtime.Flags.Openings || _feintFor != null || mi == null) return;
            if (mi.Candidate.Kind != AttackKind.Swing || _profile.ParryMutationChance <= 0f) return;
            if (Time.time - _lastMutation < Runtime.Cfg.Openings.MutationCooldown) return;
            if (_kataActive && _kataRemaining <= 0) return; // the sequence is on its last strike
            _mutationArmed = _rng.Value() < _profile.ParryMutationChance;
            _mutationCount = _melee.attackCount;
        }

        /// <summary>This creature's attack was parried (the game raises the event just before applying its own recoil).</summary>
        public void OnAttackParried(Creature parrier)
        {
            if (!_loaded || _disabled || !_mutationArmed) return;
            try
            {
                _mutationArmed = false;
                AttackMotion current = _melee.attack;
                Creature target = Target;
                if (current == null || !_brain.isAttacking || _curatedData == null || target == null || parrier != target) return;
                if (_melee.attackCount != _mutationCount) return;
                AttackMotion thrust = ChooseThrust(current, target);
                if (thrust == null)
                {
                    Log.Attack($"{_tag} parried on {current.id}: no thrust available to change into");
                    return;
                }
                _melee.recoilMinWeaponSpeed = NoRecoil; // the recoil check comes right after this event
                _mutationFrom = current;
                _mutationNext = thrust;
            }
            catch (Exception ex)
            {
                Fail("OnAttackParried", ex);
            }
        }

        private void MutationTick(float now)
        {
            if (_mutationNext == null) return;
            AttackMotion next = _mutationNext, from = _mutationFrom;
            _mutationNext = null;
            Creature target = Target;
            if (target == null || target.state != Creature.State.Alive || !_brain.isAttacking) return;
            if (_melee.attack != from || _melee.attackCount != _mutationCount || _brain.currentStagger != Brain.Stagger.None) return;

            _lastMutation = now;
            if (!_kataActive)
            {
                _kataActive = true;
                _kataStep = 0;
                _kataRemaining = 1;
                _gate.Reset();
            }
            _kataRemaining--;
            _kataExpectStep = true;
            _ignoreEndFor = from;
            _ignoreEndUntil = now + 1f;
            Log.Info($"{_tag} parried: cut {from.id} turns into thrust {next.id}");
            Hud.Show(creature, $"[{_profileName}] cut parried, changes to a thrust");
            ChainInto(from, next, target.transform, true, true);
            if (_kataExpectStep)
            {
                _kataExpectStep = false;
                EndKata("thrust change could not start");
            }
        }

        private AttackMotion ChooseThrust(AttackMotion current, Creature target)
        {
            float distance = DistanceTo(target);
            float tol = Runtime.Cfg.Openings.MutationRangeTolerance;
            string poseId = _stance.currentPose != null ? _stance.currentPose.id : "";
            ReadGuard(target);

            _candScratch.Clear();
            AddThrusts(_curatedData.chainAttacks, current, poseId, distance, tol, true);
            AddThrusts(_curatedData.openingAttacks, current, poseId, distance, tol, false);
            if (_candScratch.Count == 0) return null;

            if (_weightScratch.Length < _candScratch.Count) _weightScratch = new float[_candScratch.Count * 2];
            for (int i = 0; i < _candScratch.Count; i++)
            {
                AttackCandidate c = _candScratch[i];
                _weightScratch[i] = c.Weight * VarietyScorer.Freshness(c, _memory, Runtime.Cfg.Variety, _profile.VarietyStrength) * GuardFactor(c);
            }
            int idx = WeightedPicker.Pick(_weightScratch, _candScratch.Count, _rng);
            return idx < 0 ? null : (AttackMotion)_candScratch[idx].Tag;
        }

        private void AddThrusts(System.Collections.Generic.IEnumerable<AttackMotion> source, AttackMotion current, string poseId,
            float distance, float tol, bool chain)
        {
            foreach (AttackMotion m in source)
            {
                if (m == current || m.animationClip == null || (m.sourceIdle != "Any" && m.sourceIdle != poseId)) continue;
                if (!TryInfo(m, out MotionInfo mi) || mi.Candidate.Kind != AttackKind.Thrust) continue;
                if (!chain && !RangeFits(m, distance, tol)) continue;
                StanceCurator.RefreshDirection(mi, Runtime.Dirs);
                mi.Candidate.IsChain = chain;
                mi.Candidate.IsAuthoredFollowUp = false;
                mi.Candidate.Weight = mi.BaseWeight;
                _candScratch.Add(mi.Candidate);
            }
        }
    }
}
