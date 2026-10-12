using System.Collections.Generic;
using System.Linq;
using CompetentAI.Core;
using Xunit;

namespace CompetentAI.Core.Tests
{
    public class KataTests
    {
        private static AttackCandidate C(string id, CutDirection d, bool chain = false, bool advances = false, float w = 1f)
        {
            return new AttackCandidate { Id = id, Direction = d, IsChain = chain, Advances = advances, Weight = w, Kind = d == CutDirection.Thrust ? AttackKind.Thrust : AttackKind.Swing };
        }

        [Fact]
        public void Sequence_length_respects_profile_and_hard_maximum()
        {
            var cfg = new KataConfig();
            var profiles = SkillProfile.CreateDefaults();
            var rng = new SystemRandomSource(4);
            int maxRecruit = 0, maxDuelist = 0, minAll = 99;
            for (int i = 0; i < 2000; i++)
            {
                int a = KataPlanner.PlanLength(profiles["Recruit"], cfg, rng);
                int b = KataPlanner.PlanLength(profiles["Duelist"], cfg, rng);
                maxRecruit = System.Math.Max(maxRecruit, a);
                maxDuelist = System.Math.Max(maxDuelist, b);
                minAll = System.Math.Min(minAll, System.Math.Min(a, b));
            }
            Assert.Equal(2, maxRecruit);
            Assert.Equal(5, maxDuelist);
            Assert.Equal(2, minAll);
            var capped = profiles["Duelist"].Clone();
            capped.ComboMaxLength = 6;
            cfg.HardMaxLength = 3;
            for (int i = 0; i < 500; i++) Assert.True(KataPlanner.PlanLength(capped, cfg, rng) <= 3);
        }

        [Fact]
        public void Start_probability_follows_the_profile()
        {
            var profiles = SkillProfile.CreateDefaults();
            var rng = new SystemRandomSource(8);
            int soldier = 0, duelist = 0;
            for (int i = 0; i < 5000; i++)
            {
                if (KataPlanner.ShouldStart(profiles["Soldier"], rng)) soldier++;
                if (KataPlanner.ShouldStart(profiles["Duelist"], rng)) duelist++;
            }
            Assert.InRange(soldier / 5000.0, 0.12, 0.18);
            Assert.InRange(duelist / 5000.0, 0.36, 0.44);
        }

        [Fact]
        public void Next_step_flows_from_the_previous_direction_and_never_repeats_it()
        {
            var prev = C("A", CutDirection.Left);
            var pool = new List<AttackCandidate>
            {
                C("A", CutDirection.Left),         // same attack: excluded
                C("B", CutDirection.DownLeft),     // 1 step away
                C("C", CutDirection.Right),        // opposite: the natural return cut
            };
            var rng = new SystemRandomSource(2);
            var mem = new AttackMemory(6);
            var p = SkillProfile.CreateDefaults()["Veteran"];
            int b = 0, c = 0;
            for (int i = 0; i < 3000; i++)
            {
                int idx = KataPlanner.ChooseNext(prev, pool, mem, new VarietyConfig(), new KataConfig(), p, false, rng);
                Assert.NotEqual(0, idx);
                if (idx == 1) b++; else c++;
            }
            Assert.True(c > b * 1.2, $"opposite {c} vs neighbour {b}");
        }

        [Fact]
        public void Chain_attacks_and_advancing_attacks_when_pressing_are_favoured()
        {
            var prev = C("A", CutDirection.Left);
            var pool = new List<AttackCandidate>
            {
                C("plain", CutDirection.Right),
                C("chain", CutDirection.Right, chain: true),
                C("step", CutDirection.Right, advances: true),
            };
            var p = SkillProfile.CreateDefaults()["Veteran"];
            var rng = new SystemRandomSource(5);
            var mem = new AttackMemory(6);
            int[] n = new int[3];
            for (int i = 0; i < 6000; i++)
                n[KataPlanner.ChooseNext(prev, pool, mem, new VarietyConfig(), new KataConfig(), p, true, rng)]++;
            Assert.True(n[1] > n[0] * 1.3, "chain should beat plain");
            Assert.True(n[2] > n[0] * 1.3, "advance should beat plain while pressing");
            int[] calm = new int[3];
            for (int i = 0; i < 6000; i++)
                calm[KataPlanner.ChooseNext(prev, pool, mem, new VarietyConfig(), new KataConfig(), p, false, rng)]++;
            Assert.InRange(calm[2] / (double)calm[0], 0.85, 1.2); // no pressure: advancing is neutral
        }

        [Fact]
        public void Choose_next_returns_minus_one_when_nothing_is_eligible()
        {
            var prev = C("A", CutDirection.Left);
            var rng = new SystemRandomSource(1);
            var p = SkillProfile.CreateDefaults()["Soldier"];
            Assert.Equal(-1, KataPlanner.ChooseNext(prev, new List<AttackCandidate>(), new AttackMemory(3), new VarietyConfig(), new KataConfig(), p, false, rng));
            Assert.Equal(-1, KataPlanner.ChooseNext(prev, new List<AttackCandidate> { C("A", CutDirection.Left) }, new AttackMemory(3), new VarietyConfig(), new KataConfig(), p, false, rng));
        }

        [Fact]
        public void Interrupt_gate_decides_once_per_danger_episode()
        {
            var p = SkillProfile.CreateDefaults()["Soldier"].Clone();
            p.InterruptDanger = 0.5f;
            p.ComboCommitment = 0.5f;
            var gate = new InterruptGate();
            var rng = new FixedRandom(0.1f, 0.9f); // first roll < commitment: keeps going; second >= commitment: breaks off

            Assert.False(gate.Evaluate(0.0f, 0.2f, p, rng));  // not dangerous
            Assert.False(gate.Evaluate(0.1f, 0.7f, p, rng));  // episode 1: rolls 0.1 -> commits
            Assert.False(gate.Evaluate(0.2f, 0.9f, p, rng));  // still the same episode: no new roll
            Assert.False(gate.Evaluate(0.3f, 0.1f, p, rng));  // calm begins
            Assert.False(gate.Evaluate(0.8f, 0.1f, p, rng));  // calm long enough: episode over
            Assert.True(gate.Evaluate(1.0f, 0.8f, p, rng));   // episode 2: rolls 0.9 -> interrupts
            Assert.True(gate.Evaluate(1.1f, 0.9f, p, rng));   // stays interrupted for the whole episode
        }

        [Fact]
        public void Skilled_fighters_break_off_more_often_than_recruits()
        {
            var profiles = SkillProfile.CreateDefaults();
            var rng = new SystemRandomSource(13);
            int recruit = 0, duelist = 0;
            for (int i = 0; i < 3000; i++)
            {
                if (new InterruptGate().Evaluate(0f, 1f, profiles["Recruit"], rng)) recruit++;
                if (new InterruptGate().Evaluate(0f, 1f, profiles["Duelist"], rng)) duelist++;
            }
            Assert.True(duelist > recruit * 4, $"duelist {duelist} vs recruit {recruit}");
        }

        [Fact]
        public void Advance_votes_need_samples_and_agreement()
        {
            var cal = new DirectionCalibration();
            cal.AddAdvance("Step", true);
            cal.AddAdvance("Step", true);
            Assert.False(cal.TryGetAdvance("Step", 3, 0.7f, out _));
            cal.AddAdvance("Step", true);
            Assert.True(cal.TryGetAdvance("Step", 3, 0.7f, out bool adv));
            Assert.True(adv);
            for (int i = 0; i < 4; i++) cal.AddAdvance("Stay", false);
            Assert.True(cal.TryGetAdvance("Stay", 3, 0.7f, out adv));
            Assert.False(adv);
            var back = DirectionCalibration.FromJson(cal.ToJson());
            Assert.True(back.TryGetAdvance("Step", 3, 0.7f, out adv));
            Assert.True(adv);
        }

        [Fact]
        public void Advance_is_detected_from_stock_names()
        {
            var rules = new AttackRules(new RulesConfig());
            Assert.True(rules.LooksLikeAdvance("SwingRightToLeftStep", "Bas.Animation.Attack.Sword1h.Stance1.SlashRtLStep", 2.0f));
            Assert.True(rules.LooksLikeAdvance("LungeThrust", "x", 2.0f));
            Assert.True(rules.LooksLikeAdvance("LungeSwingTopToLeft", "x", 2.55f));
            Assert.False(rules.LooksLikeAdvance("SwingRightToLeft", "Bas.Animation.Attack.Sword1h.Stance1.RtLSwing", 1.05f));
            Assert.False(rules.LooksLikeAdvance("Thrust", "Bas.Animation.Attack.Sword1h.Stance2.Stab", 1.0f));
        }

        [Fact]
        public void Every_archetype_has_coherent_kata_settings()
        {
            foreach (var kv in SkillProfile.CreateDefaults())
            {
                var p = kv.Value;
                Assert.InRange(p.ComboMaxLength, 2, 6);
                Assert.InRange(p.ComboCommitment, 0f, 1f);
                Assert.InRange(p.InterruptDanger, 0.05f, 1f);
            }
            var d = SkillProfile.CreateDefaults();
            Assert.True(d["Recruit"].ComboCommitment > d["Duelist"].ComboCommitment);
            Assert.True(d["Recruit"].InterruptDanger > d["Duelist"].InterruptDanger);
            Assert.True(d["Recruit"].ComboMaxLength < d["Duelist"].ComboMaxLength);
        }
    }
}
