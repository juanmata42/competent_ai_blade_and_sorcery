using System;
using System.Collections.Generic;
using CompetentAI.Core;
using Xunit;

namespace CompetentAI.Core.Tests
{
    public class FixedRandom : IRandom
    {
        private readonly float[] _v;
        private int _i;

        public FixedRandom(params float[] v)
        {
            _v = v;
        }

        public float Value()
        {
            float r = _v[_i % _v.Length];
            _i++;
            return r;
        }
    }

    public class FeintDefenseTests
    {
        private static FeintContext Ctx(AttackKind k = AttackKind.Swing, float window = 0.6f)
        {
            return new FeintContext { Kind = k, Direction = CutDirection.Left, WindowSeconds = window, TargetDistance = 1.5f };
        }

        [Fact]
        public void Recruit_never_feints_and_veteran_does_with_the_configured_probability()
        {
            var profiles = SkillProfile.CreateDefaults();
            var cfg = new FeintConfig();
            var rng = new SystemRandomSource(7);
            int recruit = 0, vet = 0;
            for (int i = 0; i < 2000; i++)
            {
                if (FeintPlanner.Plan(profiles["Recruit"], cfg, Ctx(), rng).Feint) recruit++;
                if (FeintPlanner.Plan(profiles["Veteran"], cfg, Ctx(), rng).Feint) vet++;
            }
            Assert.Equal(0, recruit);
            Assert.InRange(vet / 2000.0, 0.13, 0.23);
        }

        [Fact]
        public void Reactive_boost_increases_feint_rate()
        {
            var p = SkillProfile.CreateDefaults()["Veteran"];
            var cfg = new FeintConfig();
            var rng = new SystemRandomSource(3);
            int plain = 0, reactive = 0;
            for (int i = 0; i < 4000; i++)
            {
                var c = Ctx();
                if (FeintPlanner.Plan(p, cfg, c, rng).Feint) plain++;
                c = Ctx();
                c.TargetGuardsEndZone = true;
                if (FeintPlanner.Plan(p, cfg, c, rng).Feint) reactive++;
            }
            Assert.True(reactive > plain * 1.4);
        }

        [Fact]
        public void Feint_is_refused_when_window_too_short_for_bash_or_lunges_or_far_targets()
        {
            var p = SkillProfile.CreateDefaults()["Duelist"];
            var cfg = new FeintConfig();
            var yes = new FixedRandom(0.0f);
            Assert.True(FeintPlanner.Plan(p, cfg, Ctx(), yes).Feint);
            Assert.False(FeintPlanner.Plan(p, cfg, Ctx(window: 0.1f), yes).Feint);
            Assert.False(FeintPlanner.Plan(p, cfg, Ctx(AttackKind.Bash), yes).Feint);
            var lunge = Ctx();
            lunge.IsLunge = true;
            Assert.False(FeintPlanner.Plan(p, cfg, lunge, yes).Feint);
            var far = Ctx();
            far.TargetDistance = 9f;
            Assert.False(FeintPlanner.Plan(p, cfg, far, yes).Feint);
        }

        [Fact]
        public void Feints_in_a_row_decay()
        {
            var p = SkillProfile.CreateDefaults()["Duelist"];
            var cfg = new FeintConfig();
            var rng = new SystemRandomSource(11);
            int first = 0, third = 0;
            for (int i = 0; i < 4000; i++)
            {
                var c = Ctx();
                c.FeintsInRow = 0;
                if (FeintPlanner.Plan(p, cfg, c, rng).Feint) first++;
                c = Ctx();
                c.FeintsInRow = 3;
                if (FeintPlanner.Plan(p, cfg, c, rng).Feint) third++;
            }
            Assert.True(third < first * 0.3);
        }

        [Fact]
        public void Redirect_prefers_a_contrasting_direction_and_never_the_same_attack()
        {
            var pool = new List<AttackCandidate>
            {
                new AttackCandidate { Id = "A", Direction = CutDirection.Left },
                new AttackCandidate { Id = "B", Direction = CutDirection.DownLeft },
                new AttackCandidate { Id = "C", Direction = CutDirection.Right },
            };
            var rng = new SystemRandomSource(21);
            int b = 0, c = 0;
            for (int i = 0; i < 2000; i++)
            {
                int idx = FeintPlanner.ChooseRedirect(CutDirection.Left, "A", pool, null, rng);
                Assert.NotEqual(0, idx);
                if (idx == 1) b++;
                else c++;
            }
            Assert.True(c > b * 3, $"b={b} c={c}");
        }

        // The strike point starts at x = 0 and the defender stands at +z 1.5 m: "approaching" means moving along +z.
        private static readonly Vec3 ToDefender = new Vec3(0f, 0f, 1f);

        [Fact]
        public void StrikeTracker_detects_a_feint_an_approaching_strike_that_pulls_back()
        {
            var t = new StrikeTracker();
            var cfg = new DefenseConfig();
            float now = 0f;
            int hits = 0;
            for (int i = 0; i < 10; i++, now += 0.02f)
                if (t.Update(now, new Vec3(0f, 0f, 3f), ToDefender, cfg, out _)) hits++;   // coming at the defender
            Assert.Equal(0, hits);
            for (int i = 0; i < 10; i++, now += 0.02f)
                if (t.Update(now, new Vec3(0f, 0.2f, -3f), ToDefender, cfg, out float a))  // pulls back
                {
                    hits++;
                    Assert.True(a > 150f);
                }
            Assert.Equal(1, hits);
        }

        [Fact]
        public void StrikeTracker_does_not_treat_a_normal_backswing_then_strike_as_a_feint()
        {
            var t = new StrikeTracker();
            var cfg = new DefenseConfig();
            float now = 0f;
            int hits = 0;
            for (int i = 0; i < 8; i++, now += 0.02f)
                if (t.Update(now, new Vec3(0f, 0f, -3f), ToDefender, cfg, out _)) hits++;  // wind-up: moving AWAY
            for (int i = 0; i < 12; i++, now += 0.02f)
                if (t.Update(now, new Vec3(0f, 0f, 4f), ToDefender, cfg, out _)) hits++;   // the real strike: towards
            Assert.Equal(0, hits);
        }

        [Fact]
        public void StrikeTracker_detects_a_sideways_redirection_of_an_approaching_strike()
        {
            var t = new StrikeTracker();
            var cfg = new DefenseConfig();
            float now = 0f;
            int hits = 0;
            for (int i = 0; i < 8; i++, now += 0.02f)
                if (t.Update(now, new Vec3(0f, 0f, 3f), ToDefender, cfg, out _)) hits++;
            for (int i = 0; i < 8; i++, now += 0.02f)
                if (t.Update(now, new Vec3(3f, 0f, 0f), ToDefender, cfg, out _)) hits++;   // veers sideways (90 degrees)
            Assert.Equal(1, hits);
        }

        [Fact]
        public void StrikeTracker_ignores_slow_jitter_and_gentle_curves()
        {
            var t = new StrikeTracker();
            var cfg = new DefenseConfig();
            float now = 0f;
            int hits = 0;
            for (int i = 0; i < 60; i++, now += 0.02f)
            {
                double ang = 1.2 + i * 0.02; // slow arc around the approach direction: far below the reversal angle
                if (t.Update(now, new Vec3((float)Math.Cos(ang) * 3f, 0f, (float)Math.Sin(ang) * 3f), ToDefender, cfg, out _)) hits++;
            }
            Assert.Equal(0, hits);
            var slow = new StrikeTracker();
            for (int i = 0; i < 20; i++, now += 0.02f)
                Assert.False(slow.Update(now, new Vec3(i % 2 == 0 ? 0.05f : -0.05f, 0f, 0f), ToDefender, cfg, out _));
        }

        [Fact]
        public void FeintReader_skill_controls_the_fooled_rate()
        {
            var cfg = new DefenseConfig();
            var profiles = SkillProfile.CreateDefaults();
            int fooledRecruit = 0, fooledDuelist = 0;
            var rng = new SystemRandomSource(5);
            for (int i = 0; i < 2000; i++)
            {
                if (new FeintReader(profiles["Recruit"], cfg, rng).OnReversal(0f) == FeintOutcome.Fooled) fooledRecruit++;
                if (new FeintReader(profiles["Duelist"], cfg, rng).OnReversal(0f) == FeintOutcome.Fooled) fooledDuelist++;
            }
            Assert.True(fooledRecruit > fooledDuelist * 3);
        }

        [Fact]
        public void FeintReader_effect_expires()
        {
            var cfg = new DefenseConfig();
            var p = SkillProfile.CreateDefaults()["Soldier"];
            var reader = new FeintReader(p, cfg, new FixedRandom(0.99f)); // 0.99 >= read skill: fooled
            Assert.Equal(FeintOutcome.Fooled, reader.OnReversal(10f));
            Assert.Equal(cfg.FooledSpeedFactor, reader.SpeedFactor(10.1f));
            Assert.Equal(1f, reader.SpeedFactor(11f));
        }

        [Fact]
        public void Fatigue_overwhelms_after_n_parries_in_window_then_recovers()
        {
            var p = SkillProfile.CreateDefaults()["Soldier"]; // 4 parries / 3 s, overwhelmed 0.9 s
            var f = new ParryFatigue(p);
            Assert.False(f.RegisterParry(0.0f));
            Assert.False(f.RegisterParry(0.5f));
            Assert.False(f.RegisterParry(1.0f));
            Assert.True(f.RegisterParry(1.5f));
            Assert.True(f.IsOverwhelmed(1.6f));
            Assert.False(f.IsOverwhelmed(2.5f));
        }

        [Fact]
        public void Fatigue_does_not_trigger_when_parries_are_spread_out()
        {
            var p = SkillProfile.CreateDefaults()["Soldier"];
            var f = new ParryFatigue(p);
            for (int i = 0; i < 20; i++) Assert.False(f.RegisterParry(i * 2.0f));
        }
    }
}
