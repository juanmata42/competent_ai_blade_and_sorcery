using System;
using System.IO;
using CompetentAI.Core;
using ThunderRoad;
using UnityEngine;

namespace CompetentAI.Mod
{
    /// <summary>Entry point. The game's mod manager instantiates this class by reflection.</summary>
    public class CompetentAIScript : ThunderScript
    {
        public const string Version = "0.1.0";

        private float _nextScan;
        private float _nextRefresh;
        private float _nextSave;
        private bool _subscribed;

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            try
            {
                string folder = modData != null ? modData.fullPath : "";
                var warnings = new System.Collections.Generic.List<string>();
                string path = Path.Combine(folder ?? "", "CompetentAI.config.json");
                string json = File.Exists(path) ? File.ReadAllText(path) : null;
                ModConfig cfg = ConfigIO.Parse(json, warnings);
                Runtime.Init(cfg, folder);
                Log.Info($"v{Version} loaded from '{folder}' (config {(json != null ? "found" : "NOT found, using defaults")})");
                foreach (string w in warnings) Log.Warn(w);
                if (!ApiSelfCheck.Run() && !Runtime.Cfg.Modules.ForceRunOnApiMismatch)
                {
                    Log.Error("mod disabled because the game's API does not match (set Modules.ForceRunOnApiMismatch=true to override)");
                    Runtime.Cfg.Modules.Enabled = false;
                }
                TryWriteEffectiveConfig(folder, cfg);
            }
            catch (Exception ex)
            {
                Log.Error("startup failed, the mod stays inactive: " + ex);
                Runtime.Cfg.Modules.Enabled = false;
            }
        }

        public override void ScriptEnable()
        {
            base.ScriptEnable();
            if (_subscribed) return;
            EventManager.onCreatureAttacking += OnCreatureAttacking;
            EventManager.onCreatureAttackParry += OnCreatureAttackParry;
            EventManager.onLevelUnload += OnLevelUnload;
            _subscribed = true;
        }

        public override void ScriptDisable()
        {
            Unsubscribe();
            base.ScriptDisable();
        }

        public override void ScriptUnload()
        {
            Unsubscribe();
            Runtime.SaveCalibrationIfDirty();
            base.ScriptUnload();
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventManager.onCreatureAttacking -= OnCreatureAttacking;
            EventManager.onCreatureAttackParry -= OnCreatureAttackParry;
            EventManager.onLevelUnload -= OnLevelUnload;
            _subscribed = false;
        }

        public override void ScriptUpdate()
        {
            try
            {
                float t = Time.unscaledTime;
                if (t >= _nextRefresh)
                {
                    _nextRefresh = t + 1f;
                    Runtime.Refresh();
                }
                if (t >= _nextScan)
                {
                    _nextScan = t + 0.5f;
                    if (Runtime.Flags.Enabled) AttachToNewCreatures();
                }
                if (t >= _nextSave)
                {
                    _nextSave = t + 30f;
                    Runtime.SaveCalibrationIfDirty();
                }
            }
            catch (Exception ex)
            {
                Log.Error("update failed: " + ex);
            }
        }

        private static void AttachToNewCreatures()
        {
            var list = Creature.allActive;
            for (int i = 0; i < list.Count; i++)
            {
                Creature c = list[i];
                if (c == null || c.isPlayer || c.isKilled || c.brain == null) continue;
                BrainData instance = c.brain.instance;
                if (instance == null || instance.creature == null || instance.modules == null) continue;
                if (instance.HasModule<BrainModuleCompetent>()) continue;
                if (!Runtime.Cfg.Assignment.ApplyToAllies && Player.currentCreature != null && !c.IsEnemy(Player.currentCreature)) continue;
                try
                {
                    var module = new BrainModuleCompetent();
                    instance.modules.Add(module);
                    module.Load(c);
                    if (instance.isActive) module.OnBrainStart();
                }
                catch (Exception ex)
                {
                    Log.Error("could not attach to a creature: " + ex);
                }
            }
        }

        private static void OnCreatureAttacking(Creature attacker, Creature target, Transform targetTransform,
            BrainModuleAttack.AttackType type, BrainModuleAttack.AttackStage stage)
        {
            if (type != BrainModuleAttack.AttackType.Melee || attacker == null) return;
            if (Runtime.Modules.TryGetValue(attacker, out BrainModuleCompetent m)) m.OnAttackStage(stage, target, targetTransform);
        }

        private static void OnCreatureAttackParry(Creature parried, Item parriedItem, Creature parrying, Item parryingItem, CollisionInstance collision)
        {
            if (parrying == null) return;
            if (Runtime.Modules.TryGetValue(parrying, out BrainModuleCompetent m)) m.OnParrySuccess();
        }

        private static void OnLevelUnload(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
        {
            if (eventTime != EventTime.OnEnd) return;
            Runtime.Modules.Clear();
            WeaponInspector.ClearCache();
            Runtime.SaveCalibrationIfDirty();
        }

        private static void TryWriteEffectiveConfig(string folder, ModConfig cfg)
        {
            try
            {
                if (string.IsNullOrEmpty(folder)) return;
                File.WriteAllText(Path.Combine(folder, "CompetentAI.config.effective.json"), ConfigIO.Serialize(cfg));
            }
            catch (Exception ex)
            {
                Log.Debug_("could not write effective config: " + ex.Message);
            }
        }
    }
}
