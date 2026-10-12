using System;
using System.Collections.Generic;

namespace CompetentAI.Core
{
    /// <summary>Random source abstraction so decisions are reproducible in tests.</summary>
    public interface IRandom
    {
        /// <summary>Uniform value in [0,1).</summary>
        float Value();
    }

    public sealed class SystemRandomSource : IRandom
    {
        private readonly Random _rng;

        public SystemRandomSource(int seed)
        {
            _rng = new Random(seed);
        }

        public float Value() => (float)_rng.NextDouble();
    }

    public sealed class VarietyConfig
    {
        /// <summary>How many of the latest attacks are remembered.</summary>
        public int Memory = 6;

        /// <summary>Weight factor when the candidate is the very same attack as the latest one (decays with age).</summary>
        public float SameAttackPenalty = 0.12f;

        /// <summary>Weight factor when the candidate travels the same direction as a remembered attack.</summary>
        public float SameDirectionPenalty = 0.45f;

        /// <summary>Weight factor when the candidate is only one 45° step away from a remembered direction.</summary>
        public float NeighbourDirectionPenalty = 0.8f;

        /// <summary>Bonus when the candidate's direction does not appear in memory at all.</summary>
        public float NoveltyBonus = 1.4f;

        /// <summary>
        /// Weight given to every candidate that is NOT the planned one when the director steers the pick.
        /// 1 = no steering, 0 = only the planned attack can be chosen.
        /// </summary>
        public float NonPlannedWeight = 0.04f;
    }

    /// <summary>Ring buffer of the latest attacks of one creature.</summary>
    public sealed class AttackMemory
    {
        public struct Entry
        {
            public string Id;
            public CutDirection Direction;
            public AttackKind Kind;
            public float Time;
        }

        private readonly Entry[] _ring;
        private int _next;
        private int _count;

        public AttackMemory(int capacity)
        {
            _ring = new Entry[Math.Max(1, capacity)];
        }

        public int Count => _count;
        public int Capacity => _ring.Length;

        public void Record(string id, CutDirection direction, AttackKind kind, float time)
        {
            _ring[_next] = new Entry { Id = id, Direction = direction, Kind = kind, Time = time };
            _next = (_next + 1) % _ring.Length;
            if (_count < _ring.Length) _count++;
        }

        /// <summary>Age 0 = most recent.</summary>
        public Entry Get(int age)
        {
            if (age < 0 || age >= _count) return default;
            int idx = (_next - 1 - age + _ring.Length * 2) % _ring.Length;
            return _ring[idx];
        }

        public void Clear()
        {
            _count = 0;
            _next = 0;
        }

        public int CountDirection(CutDirection d)
        {
            int n = 0;
            for (int i = 0; i < _count; i++)
                if (Get(i).Direction == d) n++;
            return n;
        }
    }

    public static class VarietyScorer
    {
        /// <summary>
        /// Multiplier in roughly [0.05, 1.5] expressing how "fresh" a candidate is, given the memory.
        /// <paramref name="strength"/> 0 disables the effect (returns 1), 1 applies it fully.
        /// </summary>
        public static float Freshness(AttackCandidate c, AttackMemory memory, VarietyConfig cfg, float strength)
        {
            if (memory == null || memory.Count == 0 || strength <= 0f) return 1f;
            double factor = 1.0;
            bool dirSeen = false;
            for (int age = 0; age < memory.Count; age++)
            {
                AttackMemory.Entry e = memory.Get(age);
                double decay = 1.0 / (1.0 + age); // recent attacks matter most
                if (e.Id == c.Id)
                    factor *= Math.Pow(cfg.SameAttackPenalty, decay);
                if (c.Direction != CutDirection.Unknown && e.Direction != CutDirection.Unknown)
                {
                    if (e.Direction == c.Direction)
                    {
                        dirSeen = true;
                        factor *= Math.Pow(cfg.SameDirectionPenalty, decay);
                    }
                    else if (Directions.Steps(e.Direction, c.Direction) == 1)
                    {
                        factor *= Math.Pow(cfg.NeighbourDirectionPenalty, decay);
                    }
                }
            }
            if (!dirSeen && c.Direction != CutDirection.Unknown) factor *= cfg.NoveltyBonus;
            double blended = 1.0 + (factor - 1.0) * Math.Min(1.0, strength);
            if (blended < 0.02) blended = 0.02;
            return (float)blended;
        }

        /// <summary>Fills <paramref name="weights"/> with base weight × rule bias × freshness.</summary>
        public static void ComputeWeights(IList<AttackCandidate> candidates, IList<float> biases,
            AttackMemory memory, VarietyConfig cfg, float strength, float[] weights)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                float bias = biases != null && i < biases.Count ? biases[i] : 1f;
                float w = candidates[i].Weight * bias * Freshness(candidates[i], memory, cfg, strength);
                weights[i] = w < 0f ? 0f : w;
            }
        }
    }

    public static class WeightedPicker
    {
        /// <summary>Returns the picked index, or -1 when every weight is zero.</summary>
        public static int Pick(float[] weights, int count, IRandom rng)
        {
            double total = 0;
            for (int i = 0; i < count; i++) total += weights[i] > 0f ? weights[i] : 0f;
            if (total <= 1e-9) return -1;
            double r = rng.Value() * total;
            for (int i = 0; i < count; i++)
            {
                float w = weights[i] > 0f ? weights[i] : 0f;
                if (r < w) return i;
                r -= w;
            }
            return count - 1;
        }
    }
}
