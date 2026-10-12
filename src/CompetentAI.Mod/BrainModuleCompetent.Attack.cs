using System;
using System.Collections.Generic;
using CompetentAI.Core;
using ThunderRoad;
using UnityEngine;
using CoreZone = CompetentAI.Core.Zone;

namespace CompetentAI.Mod
{
    internal sealed partial class BrainModuleCompetent
    {
        // stance clone + per-motion knowledge
        private MeleeStanceData _curatedData;
        private Dictionary<AttackMotion, MotionInfo> _infos = new Dictionary<AttackMotion, MotionInfo>();
        private WeaponProfile _wRight, _wLeft;
        private AttackMemory _memory;

        // variety plan
        private AttackMotion _planned;
        private float _planTime;
        private float _nextPlanAttempt;
        private float _lastPoseSwitch;
        private readonly List<AttackCandidate> _candScratch = new List<AttackCandidate>(24);
        private float[] _weightScratch = new float[24];

        // feints
        private AttackMotion _feintFor;
        private int _feintCount;
        private float _feintAt;
        private bool _feintDouble;
        private bool _pendingDouble;
        private int _feintsInRow;

        // combos / counters
        private bool _suppressEnd;
        private float _counterUntil;
        private bool _counterRolled;

        // calibration sampling
        private Vector3 _advStart;
        private bool _advTracking;
        private Collider _calCollider;
        private Vector3 _calStart;
        private Vector3 _calAxis;
        private bool _calHasStart;

        // ---------------------------------------------------------------- stance clone curation

        private void OnStanceChange(BrainModuleStance.Stance stance, StanceData data)
        {
            if (!_loaded || _disabled) return;
            try
            {
                MeleeStanceData md = data as MeleeStanceData;
                if (md == null || md == _curatedData) return;
                if (!Runtime.Flags.Enabled) return;

                _wRight = WeaponInspector.Build(HeldOrChosen(Side.Right), Runtime.Cfg.Rules);
                _wLeft = WeaponInspector.Build(HeldOrChosen(Side.Left), Runtime.Cfg.Rules);

                bool rules = Runtime.Flags.WeaponRules;
                _infos = StanceCurator.Curate(md, _wRight, _wLeft, Runtime.Cfg, Runtime.Rules, Runtime.Dirs,
                    _instance.difficulty, rules, out CurationReport report);
                _curatedData = md;
                _memory.Clear();
                ClearPlan();

                Log.Info($"{_tag} stance '{md.id}' right={_wRight} left={_wLeft} curation: {report}");
                if (Log.Enabled(LogLevel.Debug)) Log.Debug_(_tag + " " + StanceCurator.Describe(md, _infos));
            }
            catch (Exception ex)
            {
                Fail("OnStanceChange", ex);
            }
        }

        private bool TryInfo(AttackMotion m, out MotionInfo mi)
        {
            if (m == null) { mi = null; return false; }
            if (_infos.TryGetValue(m, out mi)) return true;
            mi = StanceCurator.MakeInfo(m, Runtime.Rules, Runtime.Dirs, _curatedData != null && _curatedData.chainAttacks.Contains(m));
            _infos[m] = mi;
            return true;
        }

        // ---------------------------------------------------------------- variety director

        private void ClearPlan()
        {
            if (_planned != null || _curatedData != null) RestoreWeights();
            _planned = null;
        }

        /// <summary>Puts every opener back to its own base weight (stock behaviour).</summary>
        private void RestoreWeights()
        {
            if (_curatedData == null) return;
            foreach (AttackMotion m in _curatedData.openingAttacks)
                if (_infos.TryGetValue(m, out MotionInfo mi)) m.weight = mi.BaseWeight;
            _planned = null;
        }

        private void PlanTick(float now, Creature target)
        {
            if (!Runtime.Flags.Variety || _curatedData == null || _profile.VarietyStrength <= 0f) return;
            if (_brain.isAttacking || _brain.isDodging) return;
            if (now < _nextPlanAttempt) return;
            if (_planned != null && now - _planTime < 2.5f) return;
            _nextPlanAttempt = now + 0.4f; // never re-plan every frame (e.g. while the target is out of range)
            PlanNext(now, target);
        }

        private void PlanNext(float now, Creature target)
        {
            List<AttackMotion> openers = _curatedData.openingAttacks;
            if (openers.Count == 0) return;
            float distance = DistanceTo(target);
            string poseId = _stance.currentPose != null ? _stance.currentPose.id : "";
            ReadGuard(target);

            _candScratch.Clear();
            for (int i = 0; i < openers.Count; i++)
            {
                AttackMotion m = openers[i];
                if (!TryInfo(m, out MotionInfo mi)) continue;
                if (!RangeFits(m, distance)) continue;
                StanceCurator.RefreshDirection(mi, Runtime.Dirs);
                float w = mi.BaseWeight;
                if (m.sourceIdle != "Any" && m.sourceIdle != poseId) w *= 0.35f; // switching pose costs ~0.8 s
                if (mi.Candidate.Advances && now - _lastParryTime < Runtime.Cfg.Katas.PressureWindow)
                    w *= Runtime.Cfg.Katas.AdvanceBias * _profile.PressureBias; // just parried: press the advantage
                mi.Candidate.Weight = w * GuardFactor(mi.Candidate);
                _candScratch.Add(mi.Candidate);
            }
            if (_candScratch.Count == 0) return;
            // Changing the guard pose between attacks is the visible "reset" (the arms re-arrange, and the game refuses to
            // attack for ~0.8 s meanwhile): only do it when nothing is available from the current pose.
            bool anyHere = false;
            foreach (AttackCandidate c in _candScratch)
                if (c.SourceIdle == "Any" || c.SourceIdle == poseId) { anyHere = true; break; }
            if (anyHere)
                for (int i = _candScratch.Count - 1; i >= 0; i--)
                    if (_candScratch[i].SourceIdle != "Any" && _candScratch[i].SourceIdle != poseId) _candScratch.RemoveAt(i);
            if (_candScratch.Count == 1)
            {
                // nothing to choose from; leave stock weights alone
                RestoreWeights();
                _planned = null;
                _planTime = now;
                return;
            }

            if (_weightScratch.Length < _candScratch.Count) _weightScratch = new float[_candScratch.Count * 2];
            VarietyScorer.ComputeWeights(_candScratch, null, _memory, Runtime.Cfg.Variety, _profile.VarietyStrength, _weightScratch);
            int idx = WeightedPicker.Pick(_weightScratch, _candScratch.Count, _rng);
            if (idx < 0) return;
            AttackMotion planned = (AttackMotion)_candScratch[idx].Tag;

            // steer the stock picker: planned attack keeps its weight, the other feasible ones are damped
            float damp = Runtime.Cfg.Variety.NonPlannedWeight;
            for (int i = 0; i < _candScratch.Count; i++)
            {
                var m = (AttackMotion)_candScratch[i].Tag;
                MotionInfo mi = _infos[m];
                m.weight = m == planned ? mi.BaseWeight : mi.BaseWeight * damp;
            }
            _planned = planned;
            _planTime = now;

            if (planned.sourceIdle != "Any" && planned.sourceIdle != poseId && now - _lastPoseSwitch > 1f &&
                !_stance.switching && _curatedData.GetIdleByID(planned.sourceIdle) != null)
            {
                _lastPoseSwitch = now;
                _stance.ChangeIdlePose(planned.sourceIdle, false, true);
            }

            MotionInfo pi = _infos[planned];
            Log.Attack($"{_tag} plan -> {planned.id} [{pi.Candidate.Kind}/{pi.Candidate.Direction}] among {_candScratch.Count} at {distance:0.00} m, pose={poseId}{(_guard.Sources > 0 ? " guard=" + _guard + " openest=" + _guard.MostOpen() : "")}");
        }

        // ---------------------------------------------------------------- attack lifecycle (called by the script's event dispatcher)

        public void OnAttackStage(BrainModuleAttack.AttackStage stage, Creature targetCreature, Transform targetTransform)
        {
            if (!_loaded || _disabled || !Runtime.Flags.Enabled) return;
            try
            {
                AttackMotion atk = _melee.attack ?? _melee.lastAttack;
                Log.Attack($"{_tag} stage {stage} {(atk != null ? atk.id : "-")} attackCount={_melee.attackCount} followingThrough={_melee.followingThrough}");
                switch (stage)
                {
                    case BrainModuleAttack.AttackStage.WindUp: OnWindUp(atk, targetCreature); break;
                    case BrainModuleAttack.AttackStage.Attack: CalibrationBegin(atk); break;
                    case BrainModuleAttack.AttackStage.FollowThrough: CalibrationEnd(atk); break;
                    case BrainModuleAttack.AttackStage.End:
                        if (!_suppressEnd) OnAttackEnd(atk, targetCreature);
                        break;
                    case BrainModuleAttack.AttackStage.Cancel:
                        _calHasStart = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                Fail("OnAttackStage(" + stage + ")", ex);
            }
        }

        private void OnWindUp(AttackMotion atk, Creature target)
        {
            if (atk == null) return;
            TryInfo(atk, out MotionInfo mi);
            StanceCurator.RefreshDirection(mi, Runtime.Dirs);
            float now = Time.time;
            _memory.Record(atk.id, mi.Candidate.Direction, mi.Candidate.Kind, now);
            if (atk == _planned) _planned = null; // consumed; next plan after this attack
            RestoreWeights();

            float window = atk.GetRealStartTime(_melee.GetBrainSpeedMultiplier(atk.attackSide));
            bool forced = _pendingDouble;
            _pendingDouble = false;
            string feintInfo = "";
            _advStart = creature.transform.position;
            _advTracking = true;

            // Follow-ups of a sequence are never feints (the sequence is its own commitment); a plain attack may start one.
            bool followUp = _kataExpectStep;
            if (followUp) KataWindUp(atk, target, out _);

            if (!followUp && Runtime.Flags.Feints && target != null && target.state == Creature.State.Alive)
            {
                FeintPlan plan;
                if (forced)
                {
                    float f = Runtime.Cfg.Feints.CancelFractionMin + (Runtime.Cfg.Feints.CancelFractionMax - Runtime.Cfg.Feints.CancelFractionMin) * _rng.Value();
                    plan = new FeintPlan { Feint = window >= Runtime.Cfg.Feints.MinWindowSeconds, CancelFraction = f, Why = "double" };
                }
                else
                {
                    var ctx = new FeintContext
                    {
                        Kind = mi.Candidate.Kind,
                        Direction = mi.Candidate.Direction,
                        IsLunge = mi.Candidate.IsLunge,
                        WindowSeconds = window,
                        TargetGuardsEndZone = TargetGuards(target, mi.Candidate.Direction),
                        FeintsInRow = _feintsInRow,
                        TargetDistance = DistanceTo(target)
                    };
                    plan = FeintPlanner.Plan(_profile, Runtime.Cfg.Feints, ctx, _rng);
                }
                if (plan.Feint)
                {
                    _feintFor = atk;
                    _feintCount = _melee.attackCount;
                    _feintAt = now + window * plan.CancelFraction;
                    _feintDouble = plan.Double;
                    feintInfo = $" FEINT({plan.Why}) at +{window * plan.CancelFraction:0.00}s{(plan.Double ? " double" : "")}";
                }
            }
            if (!followUp)
            {
                if (_feintFor == atk) _kataActive = false; // a feint and a sequence do not mix
                else KataWindUp(atk, target, out _);
            }
            ArmMutation(mi);
            ArmRecoilGuard(followUp);
            Log.Attack($"{_tag} windup {atk.id} [{mi.Candidate.Kind}/{mi.Candidate.Direction}/{mi.DirectionSource}{(mi.Candidate.Advances ? "/advancing" : "")}] window={window:0.00}s{feintInfo}");
        }

        private void OnAttackEnd(AttackMotion atk, Creature target)
        {
            _calHasStart = false;
            if (_feintFor == atk) return; // a feint is handled by FeintTick
            if (_ignoreEndFor == atk && Time.time < _ignoreEndUntil) { _ignoreEndFor = null; return; } // replaced by a thrust
            _feintsInRow = 0;

            KataAttackEnd(atk, target);
        }

        // ---------------------------------------------------------------- feints

        private void ClearFeint()
        {
            _feintFor = null;
            _feintDouble = false;
        }

        private void FeintTick(float now, Creature target)
        {
            if (_feintFor == null || now < _feintAt) return;

            AttackMotion first = _feintFor;
            bool stillThatAttack = _melee.attack == first && _melee.attackCount == _feintCount && _brain.isAttacking && !_brain.canDamage;
            bool dbl = _feintDouble;
            ClearFeint();
            if (!stillThatAttack || !Runtime.Flags.Feints || _curatedData == null) return;

            TryInfo(first, out MotionInfo firstInfo);
            AttackMotion redirect = ChooseRedirect(firstInfo, target);
            if (redirect == null)
            {
                Log.Attack($"{_tag} feint of {first.id} skipped: no redirect candidate in range");
                return;
            }

            EndKata("feint");
            Log.Attack($"{_tag} FEINT {first.id} -> {redirect.id}");
            Hud.Show(creature, $"[{_profileName}] feint: {first.id} > {redirect.id}");
            _feintsInRow++;
            _pendingDouble = dbl; // read by OnWindUp of the redirected attack, which starts synchronously below
            ChainInto(first, redirect, target.transform, false, true); // cross-fades into the redirect instead of resetting
            _pendingDouble = false; // consumed by OnWindUp when the redirect started; never leave it armed otherwise
        }

        private static readonly System.Reflection.FieldInfo AttackWindowField =
            typeof(BrainModuleMelee).GetField("attackWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        /// <summary>
        /// Starts <paramref name="next"/> straight from the attack that is playing, WITHOUT calling the game's StopAttack.
        /// StopAttack resets the arms' pin forces and the animation (the visible "reset" between strikes); the game's own
        /// animation layer is built to cross-fade between consecutive one-shots (two alternating start slots), which is
        /// what PlayAnimation does when an animation is already playing. Falls back to the stop-then-start path if the
        /// running attack window cannot be stopped cleanly.
        /// </summary>
        private bool ChainInto(AttackMotion current, AttackMotion next, Transform target, bool chain, bool windowStillRunning)
        {
            if (windowStillRunning)
            {
                // The old AttackWindow coroutine would go on to apply its damage logic to the attack we are replacing.
                bool stopped = false;
                try
                {
                    if (AttackWindowField != null)
                    {
                        var running = AttackWindowField.GetValue(_melee) as Coroutine;
                        if (running != null) _brain.StopCoroutine(running);
                        AttackWindowField.SetValue(_melee, null);
                        stopped = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug_(_tag + " could not stop the attack window: " + ex.Message);
                }
                if (!stopped)
                {
                    AbortAttack(current);
                    _melee.TriggerAttack(0f, next, _melee, target, chain);
                    return false;
                }
            }
            _melee.TriggerAttack(0f, next, _melee, target, chain);
            return true;
        }

        /// <summary>Stops the running attack without letting our own End-stage handler treat it as a finished attack.</summary>
        private void AbortAttack(AttackMotion atk)
        {
            _suppressEnd = true;
            try
            {
                _melee.StopAttack(_melee, atk, _melee.attackCount);
            }
            finally
            {
                _suppressEnd = false;
            }
        }

        private AttackMotion ChooseRedirect(MotionInfo first, Creature target)
        {
            float distance = DistanceTo(target);
            ReadGuard(target);
            _candScratch.Clear();
            foreach (AttackMotion m in _curatedData.openingAttacks)
            {
                if (m == first.Motion || !TryInfo(m, out MotionInfo mi)) continue;
                if (mi.Candidate.Kind == AttackKind.Bash) continue;
                if (mi.Candidate.IsLunge && !Runtime.Cfg.Feints.AllowOnLunges) continue;
                if (m.animationClip == null || !RangeFits(m, distance)) continue;
                StanceCurator.RefreshDirection(mi, Runtime.Dirs);
                mi.Candidate.Weight = mi.BaseWeight * VarietyScorer.Freshness(mi.Candidate, _memory, Runtime.Cfg.Variety, _profile.VarietyStrength) * GuardFactor(mi.Candidate);
                _candScratch.Add(mi.Candidate);
            }
            if (_candScratch.Count == 0) return null;
            int idx = FeintPlanner.ChooseRedirect(first.Candidate.Direction, first.Candidate.Id, _candScratch, null, _rng);
            return idx < 0 ? null : (AttackMotion)_candScratch[idx].Tag;
        }

        /// <summary>True when the target's weapon already sits in the zone this attack heads for (so a feint is worth more).</summary>
        private bool TargetGuards(Creature target, CutDirection dir)
        {
            if (dir == CutDirection.Unknown || target == null || target.equipment == null) return false;
            Item weapon = target.equipment.GetHeldWeapon(Side.Right) ?? target.equipment.GetHeldWeapon(Side.Left);
            if (weapon == null) return false;
            weapon.GetFurthestDamagerCollider(out Damager d, out Collider c);
            Vector3 world = c != null ? c.transform.position : weapon.transform.position;
            Vector3 local = target.transform.InverseTransformPoint(world);
            if (local.z < 0.05f) return false; // weapon behind the body: not a guard
            float eye = Mathf.Max(0.5f, target.transform.InverseTransformPoint(target.centerEyes.position).y);
            float x = local.x / 0.5f;
            float y = (local.y - 0.7f * eye) / (0.3f * eye);
            CoreZone guard = Zones.FromNormalized(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
            return CoreZone.Distance(guard, Zones.EndZone(dir)) == 0;
        }

        // ---------------------------------------------------------------- counter after dodge

        private void CounterTick(float now, Creature target)
        {
            if (now > _counterUntil || _counterRolled) return;
            if (!Runtime.Flags.CounterAfterDodge || _profile.CounterAfterDodgeChance <= 0f) { _counterRolled = true; return; }
            if (_brain.isAttacking || _brain.isDodging || creature.IsAnimatorBusy() || _brain.currentStagger != Brain.Stagger.None) return;

            if (_stance.switching) return; // the stock attack node refuses while the pose switches; retry next frame
            _counterRolled = true;
            if (DistanceTo(target) > Runtime.Cfg.Defense.CounterMaxDistance) return;
            if (_brain.GetHorizontalAngle(target.transform) > _melee.attackMaxAngle) return;
            _detection.CheckAllies(target);
            if (_detection.allyAttackingCount >= _melee.meleeMax) return; // respect the group's attacker limit
            if (_rng.Value() >= _profile.CounterAfterDodgeChance) return;
            Log.Defense($"{_tag} counter-attack after dodge");
            Hud.Show(creature, $"[{_profileName}] counter-attack after dodging");
            _melee.AttackTarget(target.transform);
        }

        // ---------------------------------------------------------------- direction calibration

        private bool TipLocal(AttackMotion atk, out Vector3 local, out Collider collider)
        {
            local = Vector3.zero;
            collider = null;
            if (atk == null) return false;
            Item weapon = creature.equipment.GetHeldItem(atk.attackSide == Interactable.HandSide.Left ? Side.Left : Side.Right);
            if (weapon == null) return false;
            weapon.GetFurthestDamagerCollider(out Damager d, out Collider c);
            if (c == null) return false;
            collider = c;
            local = creature.transform.InverseTransformPoint(c.transform.position);
            return true;
        }

        /// <summary>Records whether the animation moved the body forward (root motion), to tell stepping attacks from standing ones.</summary>
        private void MeasureAdvance(AttackMotion atk)
        {
            if (!_advTracking) return;
            _advTracking = false;
            float forward = Vector3.Dot(creature.transform.position - _advStart, creature.transform.forward);
            bool advanced = forward > 0.4f;
            Runtime.Calibration.AddAdvance(atk.address, advanced);
            Runtime.Calibration.AddAdvance(atk.id, advanced);
            Runtime.CalibrationDirty = true;
            Runtime.CalibrationRevision++;
            Log.Debug_($"{_tag} measured {atk.id}: {(advanced ? "advances" : "stays")} ({forward:0.00} m forward)");
        }

        private Vector3 BladeBaseLocal(AttackMotion atk)
        {
            RagdollHand hand = atk.attackSide == Interactable.HandSide.Left ? creature.handLeft : creature.handRight;
            return hand != null ? creature.transform.InverseTransformPoint(hand.transform.position) : Vector3.zero;
        }

        private void CalibrationBegin(AttackMotion atk)
        {
            _calHasStart = false;
            if (!Runtime.Flags.Calibration) return;
            if (TipLocal(atk, out Vector3 p, out Collider c))
            {
                _calCollider = c;
                _calStart = p;
                _calAxis = p - BladeBaseLocal(atk); // where the blade points: grip -> tip
                _calHasStart = true;
            }
        }

        private void CalibrationEnd(AttackMotion atk)
        {
            if (!_calHasStart || atk == null || _calCollider == null) return;
            _calHasStart = false;
            Vector3 end = creature.transform.InverseTransformPoint(_calCollider.transform.position);
            Vector3 motion = end - _calStart;
            MeasureAdvance(atk);
            if (motion.magnitude < Directions.MinMeasurableTravel) return;

            CutDirection d = Directions.ClassifyMeasured(new Vec3(motion.x, motion.y, motion.z), new Vec3(_calAxis.x, _calAxis.y, _calAxis.z));
            if (d == CutDirection.Unknown) return;
            Runtime.Calibration.Add(atk.address, d);
            Runtime.Calibration.Add(atk.id, d);
            Runtime.CalibrationDirty = true;
            Runtime.CalibrationRevision++;
            Log.Debug_($"{_tag} measured {atk.id}: {d} (travel {motion.magnitude:0.00} m, local ({motion.x:0.00},{motion.y:0.00},{motion.z:0.00}), blade ({_calAxis.x:0.00},{_calAxis.y:0.00},{_calAxis.z:0.00}))");
        }
    }
}
