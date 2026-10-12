using System;
using System.Collections.Generic;

namespace CompetentAI.Core
{
    /// <summary>
    /// Tuning of chained strike sequences ("katas"): several attacks thrown back to back on the assumption that
    /// the first one will be parried, each one interruptible.
    /// </summary>
    public sealed class KataConfig
    {
        /// <summary>No sequence is ever longer than this, whatever the profile says.</summary>
        public int HardMaxLength = 6;

        /// <summary>
        /// Extra distance tolerance (metres) when choosing the next step of a sequence: the opponent moved while the
        /// previous strike was in the air, so the next one may be a little off the stock range window.
        /// </summary>
        public float StepRangeTolerance = 0.45f;

        /// <summary>Weight multiplier for the game's own follow-up attacks (chain attacks) as next step.</summary>
        public float PrefersChains = 2.5f;

        /// <summary>Extra weight for the follow-up the game's authors paired with the previous attack (AttackMotion.GetChainAttack).</summary>
        public float AuthoredFollowUpBonus = 3f;

        /// <summary>Weight multiplier for attacks that advance (step/lunge) when pressing after a parry or inside a sequence.</summary>
        public float AdvanceBias = 1.25f;

        /// <summary>Seconds after a successful parry during which advancing attacks are favoured (pressure).</summary>
        public float PressureWindow = 1.5f;

        /// <summary>Probability to add one more step to a sequence, applied repeatedly up to the profile's maximum.</summary>
        public float ExtendChance = 0.65f;
    }

    public static class KataPlanner
    {
        public static bool ShouldStart(SkillProfile p, IRandom rng) => p.ComboChance > 0f && rng.Value() < p.ComboChance;

        /// <summary>Total number of strikes of a new sequence (at least 2).</summary>
        public static int PlanLength(SkillProfile p, KataConfig cfg, IRandom rng)
        {
            int max = Math.Max(2, Math.Min(p.ComboMaxLength, cfg.HardMaxLength));
            int len = 2;
            while (len < max && rng.Value() < cfg.ExtendChance) len++;
            return len;
        }

        /// <summary>
        /// Picks the next strike. Prefers a direction that flows from the previous one (the opposite cut is the most
        /// natural return), the game's own chain attacks, and advancing attacks when pressing. Never repeats the previous
        /// attack. Returns an index into <paramref name="candidates"/> or -1.
        /// </summary>
        public static int ChooseNext(AttackCandidate previous, IList<AttackCandidate> candidates, AttackMemory memory,
            VarietyConfig variety, KataConfig cfg, SkillProfile p, bool pressing, IRandom rng)
        {
            if (candidates.Count == 0) return -1;
            var w = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                AttackCandidate c = candidates[i];
                if (previous != null && c.Id == previous.Id) continue;
                float weight = c.Weight;
                if (previous != null && previous.Direction != CutDirection.Unknown && c.Direction != CutDirection.Unknown)
                    weight *= 0.6f + 0.4f * (Directions.Steps(previous.Direction, c.Direction) / 4f);
                if (c.IsChain) weight *= cfg.PrefersChains;
                if (c.IsAuthoredFollowUp) weight *= cfg.AuthoredFollowUpBonus;
                if (c.Advances && pressing) weight *= cfg.AdvanceBias * p.PressureBias;
                weight *= VarietyScorer.Freshness(c, memory, variety, p.VarietyStrength);
                w[i] = weight;
            }
            return WeightedPicker.Pick(w, w.Length, rng);
        }
    }

    /// <summary>
    /// Decides, once per danger episode, whether a creature in the middle of a sequence keeps going or breaks off to defend.
    /// A less committed (more skilled) fighter breaks off more readily when the opponent's strike becomes dangerous.
    /// </summary>
    public sealed class InterruptGate
    {
        private bool _inEpisode;
        private bool _interrupt;
        private float _calmSince = -1f;

        public void Reset()
        {
            _inEpisode = false;
            _interrupt = false;
            _calmSince = -1f;
        }

        /// <summary>True while the creature should break off the sequence.</summary>
        public bool Evaluate(float now, float danger, SkillProfile p, IRandom rng)
        {
            if (danger >= p.InterruptDanger)
            {
                _calmSince = -1f;
                if (!_inEpisode)
                {
                    _inEpisode = true;
                    _interrupt = rng.Value() >= p.ComboCommitment;
                }
                return _interrupt;
            }
            if (_inEpisode)
            {
                if (_calmSince < 0f) _calmSince = now;
                if (now - _calmSince > 0.3f)
                {
                    _inEpisode = false;
                    _interrupt = false;
                }
            }
            return _inEpisode && _interrupt;
        }
    }
}
