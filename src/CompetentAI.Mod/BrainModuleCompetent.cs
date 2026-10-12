using System;
using System.Collections.Generic;
using CompetentAI.Core;
using Newtonsoft.Json;
using ThunderRoad;
using UnityEngine;

namespace CompetentAI.Mod
{
    /// <summary>
    /// Per-creature controller. It is APPENDED to the creature's own brain instance (so it runs after the stock
    /// modules in the same tick) and never replaces or patches a stock module: it adjusts public fields of the
    /// creature's private brain copy and drives the stock public API (StopAttack / TriggerAttack / TryToDodge).
    /// Every entry point is wrapped: an exception disables this controller for that creature and the stock AI carries on.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed partial class BrainModuleCompetent : BrainData.Module
    {
        private const int MaxErrorsBeforeDisable = 4;

        private Brain _brain;
        private BrainData _instance;
        private BrainModuleMelee _melee;
        private BrainModuleDefense _defense;
        private BrainModuleStance _stance;
        private BrainModuleDetection _detection;
        private BrainModuleEquipment _equipment;

        private SkillProfile _profile;
        private string _profileName = "?";
        private IRandom _rng;
        private int _errors;
        private bool _disabled;
        private bool _loaded;
        private float _nextSync;
        private string _tag = "?";
        private string _creatureId, _brainId;
        private int _faction;
        private float _noise;
        private int _appliedLevel = -1, _appliedShift = int.MinValue;

        public BrainData Instance => _instance;
        public string ProfileName => _profileName;

        // ---------------------------------------------------------------- lifecycle

        public override void Load(Creature creature)
        {
            base.Load(creature);
            try
            {
                _brain = creature.brain;
                _instance = _brain != null ? _brain.instance : null;
                if (_instance == null) { _disabled = true; return; }

                _melee = _instance.GetModule<BrainModuleMelee>(false);
                _defense = _instance.GetModule<BrainModuleDefense>(false);
                _stance = _instance.GetModule<BrainModuleStance>(false);
                _detection = _instance.GetModule<BrainModuleDetection>(false);
                _equipment = _instance.GetModule<BrainModuleEquipment>(false);
                if (_melee == null || _defense == null || _stance == null || _detection == null)
                {
                    _disabled = true; // not a melee humanoid brain (animals, golems, shopkeepers…): leave it alone
                    return;
                }

                CaptureOriginals();
                string brainId = _instance.id;
                int faction = creature.factionId;
                string creatureId = creature.creatureId;
                _tag = (creatureId ?? "?") + "#" + creature.GetInstanceID();

                _creatureId = creatureId;
                _brainId = brainId;
                _faction = faction;
                int seed = creature.GetInstanceID() * 7919 + 13;
                _rng = new SystemRandomSource(seed);
                _noise = ((seed & 0xFFFF) / 65535f);
                ChooseProfile();

                _memory = new AttackMemory(Runtime.Cfg.Variety.Memory);
                _tracker = new StrikeTracker();

                _stance.OnStanceChange += OnStanceChange;
                creature.OnDamageEvent += OnDamaged;
                Runtime.Register(creature, this);
                _loaded = true;

                Log.Info($"attached to {_tag} brain={brainId} difficulty={_instance.difficulty} faction={faction} profile={_profileName}");
                if (_stance.currentStanceData != null) OnStanceChange(_stance.stance, _stance.currentStanceData);
                SyncTuning(true);
            }
            catch (Exception ex)
            {
                Fail("Load", ex);
            }
        }

        public override void Unload()
        {
            try
            {
                if (_stance != null) _stance.OnStanceChange -= OnStanceChange;
                if (creature != null) creature.OnDamageEvent -= OnDamaged;
                Runtime.Unregister(creature, this);
                _loaded = false;
            }
            catch (Exception ex)
            {
                Log.Warn("unload " + _tag + ": " + ex.Message);
            }
            base.Unload();
        }

        public override void OnBrainStop()
        {
            ClearTransientState();
        }

        private void Fail(string where, Exception ex)
        {
            _errors++;
            Log.Error($"{_tag} error in {where} ({_errors}/{MaxErrorsBeforeDisable}): {ex}");
            if (_errors >= MaxErrorsBeforeDisable)
            {
                Log.Error($"{_tag}: controller disabled for this creature, stock AI continues");
                _disabled = true;
                try
                {
                    if (_fatigueApplied && _defense != null) _defense.parryEnabled = _origCaptured ? _orig.parryEnabled : true;
                    _fatigueApplied = false;
                    RestoreTuning();
                    RestoreWeights();
                }
                catch { /* best effort */ }
            }
        }

        private bool Active => _loaded && !_disabled && Runtime.Flags.Enabled && creature != null && creature.state == Creature.State.Alive;

        /// <summary>
        /// Picks the skill profile: the menu's forced level if any, otherwise by creature / faction / brain / difficulty
        /// plus the menu's shift. Called at load and again whenever the menu changes (live).
        /// </summary>
        private void ChooseProfile()
        {
            EffectiveFlags f = Runtime.Flags;
            var ladder = Runtime.Cfg.Assignment.Ladder;
            string name;
            if (f.SkillLevel >= 1 && f.SkillLevel <= ladder.Count && Runtime.Cfg.Profiles.ContainsKey(ladder[f.SkillLevel - 1]))
                name = ladder[f.SkillLevel - 1];
            else
            {
                name = Runtime.Resolver.ResolveName(_creatureId, _brainId, _faction, _instance.difficulty);
                if (f.LevelShift != 0) name = Runtime.Resolver.Shift(name, f.LevelShift);
            }
            _appliedLevel = f.SkillLevel;
            _appliedShift = f.LevelShift;
            if (_profile != null && name == _profileName) return;

            _profileName = name;
            _profile = Runtime.Resolver.Get(name).WithJitter(_noise);
            _reader = new FeintReader(_profile, Runtime.Cfg.Defense, _rng);
            _fatigue = new ParryFatigue(_profile);
            _gate.Reset();
        }

        // ---------------------------------------------------------------- per-frame entry points

        public override void Update()
        {
            if (!_loaded || _disabled) return;
            try
            {
                float now = Time.time;
                if (now >= _nextSync)
                {
                    _nextSync = now + 0.5f;
                    SyncTuning(false);
                }
                if (!Active)
                {
                    if (_kataActive) EndKata("mod off");
                    if (_feintFor != null) ClearFeint();
                    return;
                }
                Tick(now);
            }
            catch (Exception ex)
            {
                Fail("Update", ex);
            }
        }

        public override void FixedUpdate()
        {
            if (!Active) return;
            try
            {
                DefenseTick(Time.time);
            }
            catch (Exception ex)
            {
                Fail("FixedUpdate", ex);
            }
        }

        private Creature Target => _brain != null ? _brain.currentTarget : null;

        private void Tick(float now)
        {
            Creature target = Target;
            if (target == null || target.state != Creature.State.Alive)
            {
                if (_planned != null) ClearPlan();
                return;
            }
            AggressionTick(now);
            if (!_brain.isAttacking) DisarmRecoilGuard();
            FeintTick(now, target);
            KataTick(now);
            CounterTick(now, target);
            PlanTick(now, target);
        }

        private void ClearTransientState()
        {
            ClearPlan();
            ClearFeint();
            EndKata("brain stopped");
            _counterUntil = 0f;
        }

        // ---------------------------------------------------------------- helpers

        private Item HeldOrChosen(Side side)
        {
            Item it = null;
            if (_equipment != null) it = side == Side.Right ? _equipment.chosenRightHandItem : _equipment.chosenLeftHandItem;
            if (it == null && creature.equipment != null) it = creature.equipment.GetHeldItem(side);
            return it;
        }

        private float DistanceTo(Creature target) => _brain.GetHorizontalDistance(target.transform);

        private bool RangeFits(AttackMotion m, float distance, float toleranceOverride = 0f)
        {
            float scale = creature.transform.lossyScale.y;
            float reach = m.includeWeaponReach ? _melee.GetWeaponReach(m.attackSide) : 0f;
            float d = distance - reach;
            float tol = toleranceOverride > 0f ? toleranceOverride : Mathf.Max(0.15f, _melee.maxRangeDelta);
            float min = m.minRange * scale;
            if (Mathf.Abs(min - d) <= tol) return true;
            return m.sweep && m.maxRange * scale > d && min <= d + tol;
        }
    }
}
