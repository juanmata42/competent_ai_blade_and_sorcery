using System.Collections.Generic;
using System.Linq;
using CompetentAI.Core;
using Xunit;

namespace CompetentAI.Core.Tests
{
    public class VarietyTests
    {
        private static List<AttackCandidate> Pool()
        {
            return new List<AttackCandidate>
            {
                new AttackCandidate { Id = "RtL", Direction = CutDirection.Left, Kind = AttackKind.Swing },
                new AttackCandidate { Id = "LtR", Direction = CutDirection.Right, Kind = AttackKind.Swing },
                new AttackCandidate { Id = "TtL", Direction = CutDirection.DownLeft, Kind = AttackKind.Swing },
                new AttackCandidate { Id = "TtR", Direction = CutDirection.DownRight, Kind = AttackKind.Swing },
                new AttackCandidate { Id = "Thrust", Direction = CutDirection.Thrust, Kind = AttackKind.Thrust },
            };
        }

        [Fact]
        public void Memory_returns_latest_first_and_wraps()
        {
            var m = new AttackMemory(3);
            m.Record("a", CutDirection.Left, AttackKind.Swing, 1f);
            m.Record("b", CutDirection.Right, AttackKind.Swing, 2f);
            m.Record("c", CutDirection.Up, AttackKind.Swing, 3f);
            m.Record("d", CutDirection.Down, AttackKind.Swing, 4f);
            Assert.Equal(3, m.Count);
            Assert.Equal("d", m.Get(0).Id);
            Assert.Equal("c", m.Get(1).Id);
            Assert.Equal("b", m.Get(2).Id);
        }

        [Fact]
        public void Just_used_attack_is_much_less_fresh_than_an_unused_one()
        {
            var pool = Pool();
            var mem = new AttackMemory(6);
            var cfg = new VarietyConfig();
            mem.Record("RtL", CutDirection.Left, AttackKind.Swing, 0f);
            float used = VarietyScorer.Freshness(pool[0], mem, cfg, 1f);
            float fresh = VarietyScorer.Freshness(pool[2], mem, cfg, 1f);
            Assert.True(used < fresh * 0.3f, $"used={used} fresh={fresh}");
        }

        [Fact]
        public void Strength_zero_disables_the_effect()
        {
            var pool = Pool();
            var mem = new AttackMemory(6);
            mem.Record("RtL", CutDirection.Left, AttackKind.Swing, 0f);
            Assert.Equal(1f, VarietyScorer.Freshness(pool[0], mem, new VarietyConfig(), 0f));
        }

        [Fact]
        public void Sampling_with_memory_uses_every_candidate_and_rarely_repeats()
        {
            var pool = Pool();
            var mem = new AttackMemory(6);
            var cfg = new VarietyConfig();
            var rng = new SystemRandomSource(1234);
            var weights = new float[pool.Count];
            var picks = new List<string>();
            for (int i = 0; i < 400; i++)
            {
                VarietyScorer.ComputeWeights(pool, null, mem, cfg, 1f, weights);
                int idx = WeightedPicker.Pick(weights, pool.Count, rng);
                Assert.True(idx >= 0);
                picks.Add(pool[idx].Id);
                mem.Record(pool[idx].Id, pool[idx].Direction, pool[idx].Kind, i);
            }
            int repeats = 0;
            for (int i = 1; i < picks.Count; i++) if (picks[i] == picks[i - 1]) repeats++;
            Assert.True(repeats < 30, "immediate repeats: " + repeats);
            foreach (var c in pool) Assert.True(picks.Count(p => p == c.Id) > 20, c.Id + " starved");
        }

        [Fact]
        public void Memory_removes_the_A_B_A_B_pattern()
        {
            var pool = Pool();
            var mem = new AttackMemory(6);
            var cfg = new VarietyConfig();
            var rng = new SystemRandomSource(99);
            var w = new float[pool.Count];
            string prev2 = null, prev1 = null;
            int alternations = 0, samples = 0;
            for (int i = 0; i < 300; i++)
            {
                VarietyScorer.ComputeWeights(pool, null, mem, cfg, 1f, w);
                int idx = WeightedPicker.Pick(w, pool.Count, rng);
                string id = pool[idx].Id;
                if (prev2 != null)
                {
                    samples++;
                    if (id == prev2 && id != prev1) alternations++;
                }
                prev2 = prev1;
                prev1 = id;
                mem.Record(id, pool[idx].Direction, pool[idx].Kind, i);
            }
            Assert.True(alternations < samples * 0.35, $"A-B-A pattern in {alternations}/{samples}");
        }

        [Fact]
        public void Picker_returns_minus_one_when_all_weights_are_zero_and_never_picks_zero_weight()
        {
            var rng = new SystemRandomSource(5);
            Assert.Equal(-1, WeightedPicker.Pick(new float[] { 0, 0, 0 }, 3, rng));
            for (int i = 0; i < 200; i++) Assert.Equal(1, WeightedPicker.Pick(new float[] { 0, 2, 0 }, 3, rng));
        }
    }
}
