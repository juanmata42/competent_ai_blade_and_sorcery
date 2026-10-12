using System;
using System.Collections.Generic;
using CompetentAI.Core;
using ThunderRoad;
using UnityEngine;

namespace CompetentAI.Mod
{
    /// <summary>
    /// "Demonstrator" behaviour of the weapon practice NPCs (spell "Spar: weapons", mode Demo): the NPC draws its weapon,
    /// then stands still facing the player and performs EVERY attack of its stance one after another (openers, chains,
    /// ripostes), with a pause in between and a label on screen, so the player can watch each animation and practise
    /// parrying it. The behaviour tree is frozen while demonstrating (the game does the same for fear: updateTree=false).
    /// </summary>
    internal sealed partial class BrainModuleCompetent
    {
        private enum DemoPhase
        {
            Off,
            Preparing,
            Showing
        }

        private DemoPhase _demoPhase = DemoPhase.Off;
        private float _demoPrepStart;
        private float _demoNext;
        private int _demoIndex;
        private bool _demoRunning;
        private bool _demoWarned;
        private bool _labelsBroken;
        private Vector3 _demoHome;
        private Quaternion _demoHomeRot;
        private BrainModuleMove _move;
        private readonly List<AttackMotion> _demoList = new List<AttackMotion>();

        private bool IsDemo =>
            _role == SparringRole.Attacker &&
            string.Equals(Runtime.Cfg.Sparring.AttackerMode, "Demo", StringComparison.OrdinalIgnoreCase);

        private void DemoTick(float now)
        {
            if (creature == null || creature.state != Creature.State.Alive) return;
            SparringConfig cfg = Runtime.Cfg.Sparring;
            Creature target = Target;

            if (_demoPhase == DemoPhase.Off)
            {
                _demoPhase = DemoPhase.Preparing;
                _demoPrepStart = now;
            }

            if (_demoPhase == DemoPhase.Preparing)
            {
                _melee.meleeEnabled = false; // let the tree draw the weapon, but never attack on its own
                if (target == null) return;
                if (_curatedData == null && _stance.currentStanceData is MeleeStanceData existing) OnStanceChange(_stance.stance, existing);

                bool held = creature.equipment.GetHeldItem(Side.Right) != null;
                bool ready = held && _curatedData != null && !_stance.switching && (_equipment == null || !_equipment.isDrawing);
                if (!ready)
                {
                    if (now - _demoPrepStart > cfg.DemoReadyTimeoutSeconds && !_demoWarned)
                    {
                        _demoWarned = true;
                        Log.Warn($"{_tag} demo: still not ready after {cfg.DemoReadyTimeoutSeconds:0}s (weapon held={held}, stance ready={_curatedData != null}); will keep waiting");
                    }
                    return;
                }

                BuildDemoList(cfg);
                if (_demoList.Count == 0)
                {
                    if (!_demoWarned)
                    {
                        _demoWarned = true;
                        Log.Warn($"{_tag} demo: this stance has no attacks to show");
                    }
                    return;
                }
                _move = _instance.GetModule<BrainModuleMove>(false);
                if (_move != null) _move.StopMove();
                _instance.updateTree = false; // freeze the behaviour tree: the NPC stays where it was summoned
                _melee.meleeEnabled = true;
                _demoHome = creature.transform.position;
                _demoHomeRot = creature.transform.rotation;
                _demoPhase = DemoPhase.Showing;
                _demoNext = now + 0.5f + (float)_rng.Value() * cfg.DemoPauseSeconds; // desynchronise the demonstrators
                Log.Info($"{_tag} demo ready: {_demoList.Count} attacks to show with {_wRight}");
                return;
            }

            // ---- showing
            if (target == null || target.state != Creature.State.Alive) return;
            if (_move != null && !_brain.isAttacking) _move.UpdateTurn(target.transform, 1f);

            if (_demoRunning)
            {
                if (_brain.isAttacking) return;
                _demoRunning = false;
                _demoNext = now + cfg.DemoPauseSeconds;
                if ((creature.transform.position - _demoHome).sqrMagnitude > 0.04f)
                    creature.Teleport(_demoHome, creature.transform.rotation); // step/lunge attacks move the body: back to the spot
                return;
            }
            if (now < _demoNext) return;
            if (_brain.isAttacking || _brain.currentStagger != Brain.Stagger.None || creature.IsAnimatorBusy() || _stance.switching) return;
            if (DistanceTo(target) > cfg.DemoActiveRange) return; // only the demonstrators near the player perform

            AttackMotion m = _demoList[_demoIndex % _demoList.Count];
            string poseId = _stance.currentPose != null ? _stance.currentPose.id : "";
            if (m.sourceIdle != "Any" && m.sourceIdle != poseId && _curatedData.GetIdleByID(m.sourceIdle) != null)
            {
                _stance.ChangeIdlePose(m.sourceIdle, true, true); // authentic start pose; the attack goes next frame
                return;
            }

            int shown = _demoIndex % _demoList.Count;
            _demoIndex++;
            TryInfo(m, out MotionInfo mi);
            StanceCurator.RefreshDirection(mi, Runtime.Dirs);
            string text = DemoLabel(m, mi, shown + 1, _demoList.Count);
            Log.Info($"{_tag} demo: {text}");
            if (cfg.DemoShowLabels && !_labelsBroken && IsNearestDemonstrator(target)) ShowLabel(text);

            _demoRunning = true;
            _melee.TriggerAttack(0f, m, _melee, target.transform);
            if (!_brain.isAttacking)
            {
                _demoRunning = false;
                _demoNext = now + cfg.DemoPauseSeconds;
            }
        }

        private void BuildDemoList(SparringConfig cfg)
        {
            _demoList.Clear();
            var entries = new List<KeyValuePair<AttackMotion, int>>();
            AddGroup(entries, _curatedData.openingAttacks, 0);
            if (cfg.DemoIncludeChains) AddGroup(entries, _curatedData.chainAttacks, 1);
            if (cfg.DemoIncludeRipostes) AddGroup(entries, _curatedData.ripostes, 2);
            entries.Sort((a, b) =>
            {
                if (a.Value != b.Value) return a.Value.CompareTo(b.Value);
                MotionInfo ia = _infos[a.Key], ib = _infos[b.Key];
                int c = ((int)ia.Candidate.Kind).CompareTo((int)ib.Candidate.Kind);
                if (c != 0) return c;
                c = ((int)ia.Candidate.Direction).CompareTo((int)ib.Candidate.Direction);
                if (c != 0) return c;
                c = ia.Candidate.Advances.CompareTo(ib.Candidate.Advances);
                return c != 0 ? c : string.CompareOrdinal(a.Key.id, b.Key.id);
            });
            foreach (var e in entries) _demoList.Add(e.Key);
        }

        private void AddGroup(List<KeyValuePair<AttackMotion, int>> entries, List<AttackMotion> list, int group)
        {
            foreach (AttackMotion m in list)
            {
                if (m == null || m.animationClip == null) continue;
                if (!TryInfo(m, out MotionInfo mi)) continue;
                StanceCurator.RefreshDirection(mi, Runtime.Dirs);
                entries.Add(new KeyValuePair<AttackMotion, int>(m, group));
            }
        }

        private string DemoLabel(AttackMotion m, MotionInfo mi, int n, int total)
        {
            string group = _curatedData.chainAttacks.Contains(m) ? "chain" : (_curatedData.ripostes.Contains(m) ? "riposte" : "opening");
            string dir = mi.Candidate.Direction == CutDirection.Unknown ? "direction not measured yet" : "to " + mi.Candidate.Direction;
            string label;
            Runtime.Labels.TryGetValue(creature, out label);
            return $"{label ?? "Practice"} {n}/{total}: {m.id} - {mi.Candidate.Kind} {dir}{(mi.Candidate.Advances ? ", advancing" : "")} ({group})";
        }

        private bool IsNearestDemonstrator(Creature target)
        {
            float mine = (creature.transform.position - target.transform.position).sqrMagnitude;
            foreach (var kv in Runtime.Roles)
            {
                Creature other = kv.Key;
                if (other == null || other == creature || kv.Value != SparringRole.Attacker || other.state != Creature.State.Alive) continue;
                if ((other.transform.position - target.transform.position).sqrMagnitude < mine) return false;
            }
            return true;
        }

        private void ShowLabel(string text)
        {
            try
            {
                if (DisplayMessage.instance == null || Player.local == null) return;
                DisplayMessage.instance.ShowMessage(new DisplayMessage.MessageData(
                    text, 0, 0f, null, null, false, false, false, false,
                    MessageAnchorType.HandLeft, null, true,
                    Mathf.Max(1.5f, Runtime.Cfg.Sparring.DemoPauseSeconds), null, true, this));
            }
            catch (Exception ex)
            {
                _labelsBroken = true;
                Log.Warn("on-screen labels disabled (the log still lists every demo attack): " + ex.Message);
            }
        }
    }
}
