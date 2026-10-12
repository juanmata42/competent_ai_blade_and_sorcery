using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CompetentAI.Core
{
    public enum LogLevel
    {
        Off = 0,
        Error = 1,
        Warn = 2,
        Info = 3,
        Debug = 4,
        Trace = 5
    }

    /// <summary>Independent switches for every module of the mod. Turning one off never affects the others.</summary>
    public sealed class ModulesConfig
    {
        /// <summary>Master switch: false = the mod does nothing at all.</summary>
        public bool Enabled = true;
        /// <summary>Apply archetype tuning (reaction speed, riposte chance, attack tempo…) to each NPC's brain modules.</summary>
        public bool ProfileTuning = true;
        /// <summary>Filter attacks by what the weapon can really do (edge / point / blunt head).</summary>
        public bool WeaponRules = true;
        /// <summary>Choose attacks with memory (no more A-B-A-B) and steer the stance to match.</summary>
        public bool Variety = true;
        /// <summary>NPCs start attacks they abort and redirect (feints).</summary>
        public bool Feints = true;
        /// <summary>NPCs chain a second attack after an unparried one.</summary>
        public bool Combos = true;
        /// <summary>NPCs can fall for (or read) the target's feints and get overwhelmed by sustained parrying.</summary>
        public bool DefenseReading = true;
        /// <summary>NPCs counter-attack right after a successful dodge.</summary>
        public bool CounterAfterDodge = true;
        /// <summary>Allow archetypes to enable active weapon placement while parrying (stock: off).</summary>
        public bool OneHandIK = true;
        /// <summary>Allow archetypes to use sidesteps (needs the game's side-dodge animations).</summary>
        public bool SideDodge = true;
        /// <summary>Measure the real direction of every attack animation at runtime.</summary>
        public bool Calibration = true;
    }

    public sealed class AssignmentConfig
    {
        /// <summary>Profile used when nothing else matches.</summary>
        public string DefaultProfile = "Soldier";

        /// <summary>BrainData id → profile name (stock ids pre-filled). Case-insensitive.</summary>
        public Dictionary<string, string> ByBrainId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HumanEasy"] = "Recruit",
            ["HumanMedium"] = "Soldier",
            ["HumanHard"] = "Veteran",
            ["HumanHardHeavy"] = "Veteran",
            ["HumanVIP"] = "Duelist"
        };

        /// <summary>BrainData.difficulty (1,2,3…) → profile name, used when the brain id is unknown (modded brains).</summary>
        public Dictionary<string, string> ByDifficulty = new Dictionary<string, string>
        {
            ["1"] = "Recruit",
            ["2"] = "Soldier",
            ["3"] = "Veteran",
            ["4"] = "Duelist"
        };

        /// <summary>Creature faction id (as string) → profile name. Takes precedence over brain id.</summary>
        public Dictionary<string, string> ByFactionId = new Dictionary<string, string>();

        /// <summary>Creature data id (e.g. "HumanMale") → profile name. Takes precedence over faction.</summary>
        public Dictionary<string, string> ByCreatureId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Shifts every assignment along Recruit→Soldier→Veteran→Duelist (e.g. +1 = one step harder, -1 = easier).</summary>
        public int GlobalLevelShift = 0;

        /// <summary>Ordered list used by GlobalLevelShift.</summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<string> Ladder = new List<string> { "Recruit", "Soldier", "Veteran", "Duelist" };

        /// <summary>Also tune NPCs that are friendly to the player.</summary>
        public bool ApplyToAllies = true;
    }

    public sealed class RulesConfig
    {
        public bool Enabled = true;

        /// <summary>When a weapon's damagers cannot be read (modded items), keep the game's stock attack list.</summary>
        public bool UnknownWeaponsAreUnrestricted = true;

        /// <summary>Reclassify attacks as Swing/Thrust/Bash from their id/address instead of trusting the declared type.</summary>
        public bool ReclassifyKindsById = true;

        /// <summary>Give the creature the attacks the stock filter dropped only because of a wrong declared type.</summary>
        public bool ReadmitReclassified = true;

        /// <summary>Attacks whose minRange is at least this are treated as lunges (step attacks).</summary>
        public float LungeMinRange = 1.8f;

        /// <summary>Weapon class name → multipliers. Class names: Sword, Dagger, Rapier, Axe, Mace, Spear, Polearm, Staff, Shield, Tool.</summary>
        public Dictionary<string, ClassBias> ClassBias = new Dictionary<string, ClassBias>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sword"] = new ClassBias(1.0f, 1.0f, 1.0f),
            ["Dagger"] = new ClassBias(0.8f, 1.3f, 0.6f),
            ["Rapier"] = new ClassBias(0.25f, 2.0f, 1.2f),
            ["Axe"] = new ClassBias(1.0f, 1.0f, 0.9f),
            ["Mace"] = new ClassBias(1.0f, 1.0f, 0.9f),
            ["Spear"] = new ClassBias(1.0f, 1.0f, 1.1f),
            ["Polearm"] = new ClassBias(1.0f, 1.0f, 1.0f),
            ["Staff"] = new ClassBias(1.0f, 1.0f, 1.0f),
            ["Tool"] = new ClassBias(1.0f, 1.0f, 0.8f)
        };

        /// <summary>Item id → class name, to fix the classification of modded weapons.</summary>
        public Dictionary<string, string> ClassOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Item id → comma list of Edge,Point,Blunt, to declare what a modded weapon can really do.</summary>
        public Dictionary<string, string> TraitOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Attack id (or address) → direction name (Down, DownLeft, Left, UpLeft, Up, UpRight, Right, DownRight, Thrust).</summary>
        public Dictionary<string, string> DirectionOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class FeintConfig
    {
        /// <summary>Feints are not started when fewer seconds than this remain before the damage window opens.</summary>
        public float MinWindowSeconds = 0.28f;
        /// <summary>The abort happens at this fraction (min,max) of the time before the damage window.</summary>
        public float CancelFractionMin = 0.35f;
        public float CancelFractionMax = 0.7f;
        /// <summary>Extra decay of the feint chance per consecutive feint.</summary>
        public float ChainDecay = 0.5f;
        /// <summary>Allow feints on lunge/step attacks (they move the body; looks worse).</summary>
        public bool AllowOnLunges = false;
        /// <summary>Only feint when the target is closer than this (metres).</summary>
        public float MaxTargetDistance = 4f;
    }

    public sealed class DefenseConfig
    {
        /// <summary>A change of strike direction larger than this (degrees) within ReversalWindow counts as a feint.</summary>
        public float ReversalAngle = 75f;
        public float ReversalWindow = 0.35f;
        /// <summary>Strike speed (m/s) below which direction changes are ignored.</summary>
        public float MinStrikeSpeed = 1.2f;
        /// <summary>When fooled, the guard is slowed by this factor (0 = frozen).</summary>
        public float FooledSpeedFactor = 0.12f;
        /// <summary>When a feint is read, the guard follows this much faster for a moment.</summary>
        public float ReadSpeedFactor = 1.6f;
        /// <summary>Chance that a fooled NPC also commits to a dodge (baiting dodges).</summary>
        public float FooledDodgeChance = 0.35f;
        /// <summary>Counter-attack only if the target is within this many metres after the dodge.</summary>
        public float CounterMaxDistance = 2.6f;
        /// <summary>Seconds after the dodge animation ends during which a counter may start.</summary>
        public float CounterWindow = 0.6f;
    }

    public sealed class CalibrationConfig
    {
        public int MinSamples = 3;
        public float Agreement = 0.7f;
        /// <summary>File (inside the mod folder) where measured directions are stored between sessions.</summary>
        public string File = "calibration.json";
    }

    public sealed class LoggingConfig
    {
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public LogLevel Level = LogLevel.Info;
        /// <summary>Log one line per attack decision (very chatty; use Debug level or higher).</summary>
        public bool LogAttacks = true;
        /// <summary>Log defence events (parries, dodges, fooled/read feints, fatigue).</summary>
        public bool LogDefense = true;
        /// <summary>Hard cap of lines per second so a bug can never flood the log.</summary>
        public int MaxLinesPerSecond = 30;
    }

    public sealed class ModConfig
    {
        public int ConfigVersion = 1;
        public ModulesConfig Modules = new ModulesConfig();
        public AssignmentConfig Assignment = new AssignmentConfig();
        public Dictionary<string, SkillProfile> Profiles = SkillProfile.CreateDefaults();
        public RulesConfig Rules = new RulesConfig();
        public VarietyConfig Variety = new VarietyConfig();
        public FeintConfig Feints = new FeintConfig();
        public DefenseConfig Defense = new DefenseConfig();
        public CalibrationConfig Calibration = new CalibrationConfig();
        public LoggingConfig Logging = new LoggingConfig();

        /// <summary>Fills missing pieces and clamps every value into a safe range.</summary>
        public void Normalize()
        {
            Modules = Modules ?? new ModulesConfig();
            Assignment = Assignment ?? new AssignmentConfig();
            Rules = Rules ?? new RulesConfig();
            Variety = Variety ?? new VarietyConfig();
            Feints = Feints ?? new FeintConfig();
            Defense = Defense ?? new DefenseConfig();
            Calibration = Calibration ?? new CalibrationConfig();
            Logging = Logging ?? new LoggingConfig();

            var defaults = SkillProfile.CreateDefaults();
            if (Profiles == null) Profiles = new Dictionary<string, SkillProfile>(StringComparer.OrdinalIgnoreCase);
            else Profiles = new Dictionary<string, SkillProfile>(Profiles, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in defaults)
                if (!Profiles.ContainsKey(kv.Key)) Profiles[kv.Key] = kv.Value;
            foreach (var kv in new List<KeyValuePair<string, SkillProfile>>(Profiles))
            {
                if (kv.Value == null) Profiles[kv.Key] = defaults.ContainsKey(kv.Key) ? defaults[kv.Key] : new SkillProfile();
                Profiles[kv.Key].Clamp();
            }

            Assignment.ByBrainId = ToCI(Assignment.ByBrainId);
            Assignment.ByCreatureId = ToCI(Assignment.ByCreatureId);
            Assignment.ByDifficulty = Assignment.ByDifficulty ?? new Dictionary<string, string>();
            Assignment.ByFactionId = Assignment.ByFactionId ?? new Dictionary<string, string>();
            if (Assignment.Ladder == null || Assignment.Ladder.Count == 0)
                Assignment.Ladder = new List<string> { "Recruit", "Soldier", "Veteran", "Duelist" };
            if (string.IsNullOrEmpty(Assignment.DefaultProfile) || !Profiles.ContainsKey(Assignment.DefaultProfile))
                Assignment.DefaultProfile = "Soldier";
            Assignment.GlobalLevelShift = Math.Max(-6, Math.Min(6, Assignment.GlobalLevelShift));

            Rules.ClassBias = ToCI(Rules.ClassBias);
            Rules.ClassOverrides = ToCI(Rules.ClassOverrides);
            Rules.TraitOverrides = ToCI(Rules.TraitOverrides);
            Rules.DirectionOverrides = ToCI(Rules.DirectionOverrides);
            Rules.LungeMinRange = Math.Max(0.5f, Math.Min(5f, Rules.LungeMinRange));

            Variety.Memory = Math.Max(1, Math.Min(Variety.Memory, 20));
            Variety.SameAttackPenalty = Clamp(Variety.SameAttackPenalty, 0.01f, 1f);
            Variety.SameDirectionPenalty = Clamp(Variety.SameDirectionPenalty, 0.01f, 1f);
            Variety.NeighbourDirectionPenalty = Clamp(Variety.NeighbourDirectionPenalty, 0.01f, 1f);
            Variety.NoveltyBonus = Clamp(Variety.NoveltyBonus, 1f, 4f);
            Variety.NonPlannedWeight = Clamp(Variety.NonPlannedWeight, 0f, 1f);

            Feints.MinWindowSeconds = Clamp(Feints.MinWindowSeconds, 0.1f, 2f);
            Feints.CancelFractionMin = Clamp(Feints.CancelFractionMin, 0.1f, 0.9f);
            Feints.CancelFractionMax = Clamp(Feints.CancelFractionMax, Feints.CancelFractionMin, 0.95f);
            Feints.ChainDecay = Clamp(Feints.ChainDecay, 0f, 1f);
            Feints.MaxTargetDistance = Clamp(Feints.MaxTargetDistance, 1f, 12f);

            Defense.ReversalAngle = Clamp(Defense.ReversalAngle, 30f, 150f);
            Defense.ReversalWindow = Clamp(Defense.ReversalWindow, 0.1f, 1.5f);
            Defense.MinStrikeSpeed = Clamp(Defense.MinStrikeSpeed, 0.3f, 10f);
            Defense.FooledSpeedFactor = Clamp(Defense.FooledSpeedFactor, 0f, 1f);
            Defense.ReadSpeedFactor = Clamp(Defense.ReadSpeedFactor, 1f, 3f);
            Defense.FooledDodgeChance = Clamp(Defense.FooledDodgeChance, 0f, 1f);
            Defense.CounterMaxDistance = Clamp(Defense.CounterMaxDistance, 0.5f, 6f);
            Defense.CounterWindow = Clamp(Defense.CounterWindow, 0.1f, 3f);

            Calibration.MinSamples = Math.Max(1, Math.Min(Calibration.MinSamples, 50));
            Calibration.Agreement = Clamp(Calibration.Agreement, 0.5f, 1f);
            if (string.IsNullOrEmpty(Calibration.File)) Calibration.File = "calibration.json";

            Logging.MaxLinesPerSecond = Math.Max(1, Math.Min(Logging.MaxLinesPerSecond, 500));
        }

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);

        private static Dictionary<string, T> ToCI<T>(Dictionary<string, T> src)
        {
            return src == null
                ? new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, T>(src, StringComparer.OrdinalIgnoreCase);
        }
    }

    public static class ConfigIO
    {
        /// <summary>
        /// Parses a JSON config (comments allowed). On any problem returns defaults and reports it in
        /// <paramref name="warnings"/>; never throws.
        /// </summary>
        public static ModConfig Parse(string json, List<string> warnings)
        {
            warnings = warnings ?? new List<string>();
            ModConfig cfg = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(json))
                {
                    JObject root = JObject.Parse(json, new JObjectLoadSettings());
                    JObject userProfiles = root["Profiles"] as JObject;
                    root.Remove("Profiles");

                    var settings = new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore,
                        Error = (s, e) =>
                        {
                            warnings.Add("config: ignored bad value at '" + e.ErrorContext.Path + "': " + e.ErrorContext.Error.Message);
                            e.ErrorContext.Handled = true;
                        }
                    };
                    cfg = root.ToObject<ModConfig>(JsonSerializer.Create(settings));
                    if (cfg != null && userProfiles != null) cfg.Profiles = LayerProfiles(userProfiles, warnings);
                }
            }
            catch (Exception ex)
            {
                warnings.Add("config: could not parse JSON, using defaults: " + ex.Message);
                cfg = null;
            }
            if (cfg == null) cfg = new ModConfig();
            cfg.Normalize();
            return cfg;
        }

        /// <summary>
        /// A profile written by the user is layered over the default profile of the same name (or over the one
        /// named by "Extends"), so a profile can be edited by listing only the fields that change.
        /// </summary>
        private static Dictionary<string, SkillProfile> LayerProfiles(JObject user, List<string> warnings)
        {
            var defaults = SkillProfile.CreateDefaults();
            var result = new Dictionary<string, SkillProfile>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in defaults) result[kv.Key] = kv.Value;
            foreach (JProperty prop in user.Properties())
            {
                try
                {
                    JObject overlay = prop.Value as JObject;
                    if (overlay == null) continue;
                    string baseName = overlay.Value<string>("Extends") ?? prop.Name;
                    SkillProfile baseProfile;
                    if (!defaults.TryGetValue(baseName, out baseProfile)) baseProfile = result.ContainsKey(baseName) ? result[baseName] : new SkillProfile();
                    JObject merged = JObject.FromObject(baseProfile);
                    overlay.Remove("Extends");
                    merged.Merge(overlay);
                    result[prop.Name] = merged.ToObject<SkillProfile>();
                }
                catch (Exception ex)
                {
                    warnings.Add("config: profile '" + prop.Name + "' ignored: " + ex.Message);
                }
            }
            return result;
        }

        private sealed class JObjectLoadSettings : JsonLoadSettings
        {
            public JObjectLoadSettings()
            {
                CommentHandling = CommentHandling.Ignore;
                LineInfoHandling = LineInfoHandling.Ignore;
            }
        }

        public static string Serialize(ModConfig cfg)
        {
            return JsonConvert.SerializeObject(cfg, Formatting.Indented);
        }
    }

    /// <summary>Decides which archetype a creature gets.</summary>
    public sealed class ProfileResolver
    {
        private readonly ModConfig _cfg;

        public ProfileResolver(ModConfig cfg)
        {
            _cfg = cfg;
        }

        /// <summary>Returns the profile name after applying precedence and the global level shift.</summary>
        public string ResolveName(string creatureId, string brainId, int factionId, int brainDifficulty)
        {
            AssignmentConfig a = _cfg.Assignment;
            string name = null;
            if (!string.IsNullOrEmpty(creatureId)) a.ByCreatureId.TryGetValue(creatureId, out name);
            if (name == null) a.ByFactionId.TryGetValue(factionId.ToString(), out name);
            if (name == null && !string.IsNullOrEmpty(brainId)) a.ByBrainId.TryGetValue(brainId, out name);
            if (name == null) a.ByDifficulty.TryGetValue(brainDifficulty.ToString(), out name);
            if (name == null || !_cfg.Profiles.ContainsKey(name)) name = a.DefaultProfile;
            return Shift(name, a.GlobalLevelShift);
        }

        public string Shift(string name, int shift)
        {
            if (shift == 0) return name;
            List<string> ladder = _cfg.Assignment.Ladder;
            int i = ladder.FindIndex(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
            if (i < 0) return name;
            int j = Math.Max(0, Math.Min(ladder.Count - 1, i + shift));
            string shifted = ladder[j];
            return _cfg.Profiles.ContainsKey(shifted) ? shifted : name;
        }

        public SkillProfile Get(string name)
        {
            if (name != null && _cfg.Profiles.TryGetValue(name, out SkillProfile p)) return p;
            return _cfg.Profiles[_cfg.Assignment.DefaultProfile];
        }
    }
}
