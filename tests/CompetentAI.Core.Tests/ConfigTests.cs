using System.Collections.Generic;
using CompetentAI.Core;
using Xunit;

namespace CompetentAI.Core.Tests
{
    public class ConfigTests
    {
        [Fact]
        public void Empty_or_garbage_config_falls_back_to_defaults_without_throwing()
        {
            var w = new List<string>();
            var c1 = ConfigIO.Parse("", w);
            var c2 = ConfigIO.Parse("{ not json", w);
            Assert.True(c1.Modules.Enabled);
            Assert.True(c2.Profiles.ContainsKey("Veteran"));
            Assert.NotEmpty(w);
        }

        [Fact]
        public void Comments_are_allowed_in_the_file()
        {
            var w = new List<string>();
            var cfg = ConfigIO.Parse("{ // hello\n \"Modules\": { /* x */ \"Feints\": false } }", w);
            Assert.False(cfg.Modules.Feints);
            Assert.True(cfg.Modules.Variety);
            Assert.Empty(w);
        }

        [Fact]
        public void Partial_profile_is_layered_over_the_default_one()
        {
            var cfg = ConfigIO.Parse("{ \"Profiles\": { \"Veteran\": { \"FeintChance\": 0.5 } } }", null);
            Assert.Equal(0.5f, cfg.Profiles["Veteran"].FeintChance);
            Assert.Equal(1.4f, cfg.Profiles["Veteran"].ParryAdjustSpeedMul);
            Assert.True(cfg.Profiles["Veteran"].OneHandIK);
        }

        [Fact]
        public void Custom_profile_can_extend_another()
        {
            var cfg = ConfigIO.Parse("{ \"Profiles\": { \"Boss\": { \"Extends\": \"Duelist\", \"FeintChance\": 0.6 } } }", null);
            Assert.Equal(0.6f, cfg.Profiles["Boss"].FeintChance);
            Assert.Equal(1.7f, cfg.Profiles["Boss"].ParryAdjustSpeedMul);
        }

        [Fact]
        public void Values_are_clamped_to_safe_ranges()
        {
            var cfg = ConfigIO.Parse(
                "{ \"Profiles\": { \"Soldier\": { \"ParryAdjustSpeedMul\": 500, \"FeintChance\": 5, \"AnimSpeedMul\": 0 } }, " +
                "\"Logging\": { \"MaxLinesPerSecond\": 99999 }, \"Variety\": { \"Memory\": 0 } }", null);
            Assert.Equal(4f, cfg.Profiles["Soldier"].ParryAdjustSpeedMul);
            Assert.Equal(0.8f, cfg.Profiles["Soldier"].FeintChance);
            Assert.Equal(0.6f, cfg.Profiles["Soldier"].AnimSpeedMul);
            Assert.Equal(500, cfg.Logging.MaxLinesPerSecond);
            Assert.Equal(1, cfg.Variety.Memory);
        }

        [Fact]
        public void User_dictionary_entries_merge_with_defaults()
        {
            var cfg = ConfigIO.Parse("{ \"Assignment\": { \"ByBrainId\": { \"MyModBrain\": \"Duelist\" } } }", null);
            Assert.Equal("Duelist", cfg.Assignment.ByBrainId["MyModBrain"]);
            Assert.Equal("Recruit", cfg.Assignment.ByBrainId["HumanEasy"]);
        }

        [Fact]
        public void Resolver_follows_precedence()
        {
            var cfg = ConfigIO.Parse("{ \"Assignment\": { \"ByFactionId\": { \"7\": \"Duelist\" }, \"ByCreatureId\": { \"Boss\": \"Recruit\" } } }", null);
            var r = new ProfileResolver(cfg);
            Assert.Equal("Recruit", r.ResolveName("HumanMale", "HumanEasy", 0, 1));
            Assert.Equal("Veteran", r.ResolveName("HumanMale", "HumanHard", 0, 3));
            Assert.Equal("Duelist", r.ResolveName("HumanMale", "HumanEasy", 7, 1));
            Assert.Equal("Recruit", r.ResolveName("Boss", "HumanHard", 7, 3));
            Assert.Equal("Veteran", r.ResolveName("X", "UnknownBrain", 0, 3));
            Assert.Equal("Soldier", r.ResolveName("X", "UnknownBrain", 0, 99));
        }

        [Fact]
        public void Global_level_shift_moves_along_the_ladder_and_clamps()
        {
            var cfg = ConfigIO.Parse("{ \"Assignment\": { \"GlobalLevelShift\": 1 } }", null);
            var r = new ProfileResolver(cfg);
            Assert.Equal("Soldier", r.ResolveName("X", "HumanEasy", 0, 1));
            Assert.Equal("Duelist", r.ResolveName("X", "HumanVIP", 0, 4));
            cfg = ConfigIO.Parse("{ \"Assignment\": { \"GlobalLevelShift\": -5 } }", null);
            Assert.Equal("Recruit", new ProfileResolver(cfg).ResolveName("X", "HumanHard", 0, 3));
        }

        [Fact]
        public void Jitter_is_bounded_and_deterministic()
        {
            var p = SkillProfile.CreateDefaults()["Veteran"];
            var a = p.WithJitter(0.25f);
            var b = p.WithJitter(0.25f);
            Assert.Equal(a.ParryAdjustSpeedMul, b.ParryAdjustSpeedMul);
            Assert.InRange(a.ParryAdjustSpeedMul, p.ParryAdjustSpeedMul * (1 - p.Jitter) - 1e-4, p.ParryAdjustSpeedMul * (1 + p.Jitter) + 1e-4);
        }

        [Fact]
        public void Round_trip_serialization_keeps_values()
        {
            var cfg = new ModConfig();
            cfg.Normalize();
            string json = ConfigIO.Serialize(cfg);
            var back = ConfigIO.Parse(json, null);
            Assert.Equal(cfg.Profiles["Duelist"].FeintChance, back.Profiles["Duelist"].FeintChance);
            Assert.Equal(cfg.Rules.ClassBias["Rapier"].Thrust, back.Rules.ClassBias["Rapier"].Thrust);
        }
    }

    public class CalibrationTests
    {
        [Fact]
        public void Votes_need_min_samples_and_agreement()
        {
            var cal = new DirectionCalibration();
            cal.Add("A", CutDirection.Left);
            cal.Add("A", CutDirection.Left);
            Assert.False(cal.TryGet("A", 3, 0.7f, out _, out _));
            cal.Add("A", CutDirection.Left);
            Assert.True(cal.TryGet("A", 3, 0.7f, out CutDirection d, out int n));
            Assert.Equal(CutDirection.Left, d);
            Assert.Equal(3, n);
            cal.Add("A", CutDirection.Right);
            cal.Add("A", CutDirection.Right);
            cal.Add("A", CutDirection.Right);
            Assert.False(cal.TryGet("A", 3, 0.7f, out _, out _)); // 50/50: no agreement
        }

        [Fact]
        public void Calibration_round_trips_through_json()
        {
            var cal = new DirectionCalibration();
            for (int i = 0; i < 4; i++) cal.Add("X", CutDirection.DownRight);
            var back = DirectionCalibration.FromJson(cal.ToJson());
            Assert.True(back.TryGet("X", 3, 0.7f, out CutDirection d, out _));
            Assert.Equal(CutDirection.DownRight, d);
            Assert.Equal(0, DirectionCalibration.FromJson("garbage").Keys);
        }

        [Fact]
        public void Resolver_prefers_override_then_measurement_then_name()
        {
            var rules = new RulesConfig();
            var cal = new CalibrationConfig();
            var data = new DirectionCalibration();
            var res = new DirectionResolver(rules, cal, data);
            Assert.Equal(CutDirection.Left, res.Resolve("SwingRightToLeft", "Bas.Animation.Attack.Sword1h.Stance1.RtLSwing", AttackKind.Swing, out string s1));
            Assert.Equal("name", s1);
            Assert.Equal(CutDirection.Unknown, res.Resolve("RiposteHighLeft", "x", AttackKind.Swing, out string s2));
            Assert.Equal("unknown", s2);
            for (int i = 0; i < 3; i++) data.Add("RiposteHighLeft", CutDirection.DownRight);
            Assert.Equal(CutDirection.DownRight, res.Resolve("RiposteHighLeft", "x", AttackKind.Swing, out string s3));
            Assert.Equal("measured", s3);
            rules.DirectionOverrides["RiposteHighLeft"] = "Up";
            Assert.Equal(CutDirection.Up, res.Resolve("RiposteHighLeft", "x", AttackKind.Swing, out string s4));
            Assert.Equal("config", s4);
        }

        [Fact]
        public void Name_hints_cover_the_stock_vanilla_motions_unambiguously()
        {
            Assert.Equal(CutDirection.Thrust, DirectionResolver.FromName("LungeThrust", "", AttackKind.Thrust));
            Assert.Equal(CutDirection.Left, DirectionResolver.FromName("SwingRightToLeftStep", "Bas.Animation.Attack.Sword1h.Stance1.SlashRtLStep", AttackKind.Swing));
            Assert.Equal(CutDirection.Right, DirectionResolver.FromName("SwingLeftToRight", "", AttackKind.Swing));
            Assert.Equal(CutDirection.DownLeft, DirectionResolver.FromName("SwingTopToLeft", "", AttackKind.Swing));
            Assert.Equal(CutDirection.DownRight, DirectionResolver.FromName("LungeSwingTopToRight", "", AttackKind.Swing));
            Assert.Equal(CutDirection.Unknown, DirectionResolver.FromName("CounterSwingHighRight", "", AttackKind.Swing));
        }
    }
}
