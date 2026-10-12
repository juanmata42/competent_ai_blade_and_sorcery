using System;
using System.Collections.Generic;

namespace CompetentAI.Core
{
    public sealed class FeintContext
    {
        public AttackKind Kind;
        public CutDirection Direction;
        public bool IsLunge;
        /// <summary>Seconds left before the attack's damage window opens.</summary>
        public float WindowSeconds;
        /// <summary>True when the target's weapon/guard already sits in the zone this attack ends in.</summary>
        public bool TargetGuardsEndZone;
        /// <summary>Feints already performed in a row against this target.</summary>
        public int FeintsInRow;
        public float TargetDistance;
    }

    public sealed class FeintPlan
    {
        public bool Feint;
        /// <summary>0..1: how far through the pre-damage window the attack is aborted.</summary>
        public float CancelFraction;
        /// <summary>True when the redirected attack is itself a feint (a double feint).</summary>
        public bool Double;
        public string Why = "";

        public static readonly FeintPlan None = new FeintPlan { Feint = false, Why = "none" };
    }

    public static class FeintPlanner
    {
        public static FeintPlan Plan(SkillProfile p, FeintConfig cfg, FeintContext c, IRandom rng)
        {
            if (p == null || cfg == null || c == null) return FeintPlan.None;
            if (p.FeintChance <= 0f) return Why("no-feint-skill");
            if (c.Kind == AttackKind.Bash || c.Kind == AttackKind.Unknown) return Why("kind");
            if (c.IsLunge && !cfg.AllowOnLunges) return Why("lunge");
            if (c.WindowSeconds < cfg.MinWindowSeconds) return Why("window-too-short");
            if (c.TargetDistance > cfg.MaxTargetDistance) return Why("target-too-far");

            double chance = p.FeintChance;
            if (c.TargetGuardsEndZone) chance *= p.ReactiveFeintBoost;
            chance *= Math.Pow(cfg.ChainDecay, Math.Max(0, c.FeintsInRow));
            if (chance > 0.85) chance = 0.85;
            if (rng.Value() >= chance) return Why("roll");

            float frac = cfg.CancelFractionMin + (cfg.CancelFractionMax - cfg.CancelFractionMin) * rng.Value();
            return new FeintPlan
            {
                Feint = true,
                CancelFraction = frac,
                Double = rng.Value() < p.DoubleFeintChance,
                Why = c.TargetGuardsEndZone ? "reactive" : "random"
            };
        }

        private static FeintPlan Why(string why) => new FeintPlan { Feint = false, Why = why };

        /// <summary>
        /// Chooses the attack to redirect into: prefers a different direction (the further, the better),
        /// never the same attack. Returns an index into <paramref name="candidates"/> or -1.
        /// </summary>
        public static int ChooseRedirect(CutDirection first, string firstId, IList<AttackCandidate> candidates,
            IList<float> baseWeights, IRandom rng)
        {
            var w = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                AttackCandidate c = candidates[i];
                if (c.Id == firstId) continue;
                float bw = baseWeights != null && i < baseWeights.Count ? baseWeights[i] : c.Weight;
                float contrast = 1f + Directions.Steps(first, c.Direction);
                w[i] = bw * contrast * contrast;
            }
            return WeightedPicker.Pick(w, w.Length, rng);
        }
    }
}
