using System;
using System.Collections.Generic;
using System.IO;
using CompetentAI.Core;
using ThunderRoad;

namespace CompetentAI.Mod
{
    /// <summary>The module switches actually in force: JSON config AND in-game menu.</summary>
    internal sealed class EffectiveFlags
    {
        public bool Enabled, ProfileTuning, WeaponRules, Variety, Feints, Combos, DefenseReading, CounterAfterDodge,
            OneHandIK, SideDodge, Calibration, ExtraInfo;
        public int LevelShift;
        /// <summary>0 = Auto (per brain); 1..4 forces Ladder[level-1] for every NPC.</summary>
        public int SkillLevel;

        public bool SameAs(EffectiveFlags o) =>
            o != null && Enabled == o.Enabled && ProfileTuning == o.ProfileTuning && WeaponRules == o.WeaponRules &&
            Variety == o.Variety && Feints == o.Feints && Combos == o.Combos && DefenseReading == o.DefenseReading &&
            CounterAfterDodge == o.CounterAfterDodge && OneHandIK == o.OneHandIK && SideDodge == o.SideDodge &&
            Calibration == o.Calibration && LevelShift == o.LevelShift && SkillLevel == o.SkillLevel && ExtraInfo == o.ExtraInfo;
    }

    /// <summary>Process-wide state shared by every creature controller.</summary>
    internal static class Runtime
    {
        public static ModConfig Cfg = new ModConfig();
        public static AttackRules Rules;
        public static DirectionResolver Dirs;
        public static DirectionCalibration Calibration = new DirectionCalibration();
        public static ProfileResolver Resolver;
        public static EffectiveFlags Flags = new EffectiveFlags();
        public static string ModFolder = "";
        public static bool CalibrationDirty;
        /// <summary>Incremented whenever a new direction measurement arrives, so cached labels can be refreshed lazily.</summary>
        public static int CalibrationRevision;
        public static readonly Dictionary<Creature, BrainModuleCompetent> Modules = new Dictionary<Creature, BrainModuleCompetent>();

        public static string ConfigPath => Path.Combine(ModFolder, "CompetentAI.config.json");
        public static string CalibrationPath => Path.Combine(ModFolder, Cfg.Calibration.File);

        public static void Init(ModConfig cfg, string modFolder)
        {
            Cfg = cfg;
            ModFolder = modFolder ?? "";
            Rules = new AttackRules(cfg.Rules);
            Resolver = new ProfileResolver(cfg);
            Calibration = LoadCalibration();
            Dirs = new DirectionResolver(cfg.Rules, cfg.Calibration, Calibration);
            Refresh();
        }

        /// <summary>Recomputes the effective flags from the JSON and the in-game menu. Cheap; call about once a second.</summary>
        public static void Refresh()
        {
            ModulesConfig m = Cfg.Modules;
            var f = new EffectiveFlags
            {
                Enabled = m.Enabled && ModOptions.Enabled,
                ProfileTuning = m.ProfileTuning && ModOptions.ProfileTuning,
                WeaponRules = m.WeaponRules && ModOptions.WeaponRules,
                Variety = m.Variety && ModOptions.Variety,
                Feints = m.Feints && ModOptions.Feints,
                Combos = m.Combos && ModOptions.Combos,
                DefenseReading = m.DefenseReading && ModOptions.DefenseReading,
                CounterAfterDodge = m.CounterAfterDodge && ModOptions.CounterAfterDodge,
                OneHandIK = m.OneHandIK && ModOptions.OneHandIK,
                SideDodge = m.SideDodge && ModOptions.SideDodge,
                Calibration = m.Calibration && ModOptions.Calibration,
                ExtraInfo = Cfg.Hud.Enabled && ModOptions.ExtraInfo,
                LevelShift = ModOptions.DifficultyShift,
                SkillLevel = ModOptions.SkillLevel
            };
            bool changed = !f.SameAs(Flags);
            Flags = f;

            LogLevel cfgLevel = Cfg.Logging.Level;
            LogLevel opt = (LogLevel)Math.Max(0, Math.Min(5, ModOptions.LogLevelOption));
            Log.Level = (LogLevel)Math.Max((int)cfgLevel, (int)opt);
            Log.AttackLogs = Cfg.Logging.LogAttacks;
            Log.DefenseLogs = Cfg.Logging.LogDefense;
            Log.MaxLinesPerSecond = Cfg.Logging.MaxLinesPerSecond;
            if (changed) Log.Info("effective settings: " + Describe(f));
        }

        public static string Describe(EffectiveFlags f) =>
            $"enabled={f.Enabled} tuning={f.ProfileTuning} rules={f.WeaponRules} variety={f.Variety} feints={f.Feints} combos={f.Combos} " +
            $"defenseReading={f.DefenseReading} counterAfterDodge={f.CounterAfterDodge} ik={f.OneHandIK} sideDodge={f.SideDodge} " +
            $"calibration={f.Calibration} extraInfo={f.ExtraInfo} shift={f.LevelShift} skillLevel={(f.SkillLevel == 0 ? "auto" : f.SkillLevel.ToString())}";

        private static DirectionCalibration LoadCalibration()
        {
            try
            {
                string path = CalibrationPath;
                if (File.Exists(path)) return DirectionCalibration.FromJson(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Log.Warn("could not read calibration file: " + ex.Message);
            }
            return new DirectionCalibration();
        }

        public static void SaveCalibrationIfDirty()
        {
            if (!CalibrationDirty) return;
            try
            {
                File.WriteAllText(CalibrationPath, Calibration.ToJson());
                CalibrationDirty = false;
                Log.Debug_("calibration saved (" + Calibration.Keys + " attacks measured)");
            }
            catch (Exception ex)
            {
                CalibrationDirty = false; // do not retry forever (read-only folder)
                Log.Warn("could not save calibration file: " + ex.Message);
            }
        }

        public static void PruneDeadCreatures()
        {
            List<Creature> dead = null;
            foreach (var kv in Modules)
                if (kv.Key == null) (dead ?? (dead = new List<Creature>())).Add(kv.Key);
            if (dead != null) foreach (Creature c in dead) Modules.Remove(c);

        }

        public static void Register(Creature c, BrainModuleCompetent m) => Modules[c] = m;

        public static void Unregister(Creature c, BrainModuleCompetent m)
        {
            if (c != null && Modules.TryGetValue(c, out BrainModuleCompetent cur) && cur == m) Modules.Remove(c);
        }
    }
}
