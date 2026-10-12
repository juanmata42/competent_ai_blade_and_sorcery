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

                string name = Runtime.Resolver.ResolveName(creatureId, brainId, faction, _instance.difficulty);
                if (Runtime.Flags.LevelShift != 0) name = Runtime.Resolver.Shift(name, Runtime.Flags.LevelShift);
                _profileName = name;
                int seed = creature.GetInstanceID() * 7919 + 13;
                _rng = new SystemRandomSource(seed);
                float noise = ((seed & 0xFFFF) / 65535f);
                _profile = Runtime.Resolver.Get(name).WithJitter(noise);

                _memory = new AttackMemory(Runtime.Cfg.Variety.Memory);
                _reader = new FeintReader(_profile, Runtime.Cfg.Defense, _rng);
                _fatigue = new ParryFatigue(_profile);
                _tracker = new StrikeTracker();

                _stance.OnStanceChange += OnStanceChange;
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
                try { RestoreTuning(); RestoreWeights(); } catch { /* best effort */ }
            }
        }

        private bool Active => _loaded && !_disabled && Runtime.Flags.Enabled && creature != null && creature.state == Creature.State.Alive;

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
                if (!Active) return;
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
            FeintTick(now, target);
            CounterTick(now, target);
            PlanTick(now, target);
        }

        private void ClearTransientState()
        {
            ClearPlan();
            ClearFeint();
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

        private bool RangeFits(AttackMotion m, float distance)
        {
            float scale = creature.transform.lossyScale.y;
            float reach = m.includeWeaponReach ? _melee.GetWeaponReach(m.attackSide) : 0f;
            float d = distance - reach;
            float tol = Mathf.Max(0.15f, _melee.maxRangeDelta);
            float min = m.minRange * scale;
            if (Mathf.Abs(min - d) <= tol) return true;
            return m.sweep && m.maxRange * scale > d && min <= d + tol;
        }
    }
}
