using System;
using System.Collections.Generic;

namespace CompetentAI.Core
{
    /// <summary>
    /// A skill archetype. All "Mul" fields multiply the creature's OWN brain value (so Easy stays weaker than Hard
    /// inside the same archetype family and other mods' brain edits are respected).
    /// </summary>
    public sealed class SkillProfile
    {
        // ---- Defence tuning (applied to BrainModuleDefense / BrainModuleMelee) ----
        /// <summary>Speed at which the guard follows the incoming strike (stock 7).</summary>
        public float ParryAdjustSpeedMul = 1f;
        /// <summary>&gt;1 starts blocking/dodging earlier (stock aboutToBeHitTiming 0.5 s).</summary>
        public float AboutToBeHitMul = 1f;
        /// <summary>Scales the dodge-chance curve (clamped to 0..1).</summary>
        public float DodgeChanceMul = 1f;
        /// <summary>Subtracted from recoilNothingRiposteChance (x and y): positive = more ripostes.</summary>
        public float RiposteChanceShift = 0f;
        /// <summary>Scales minMaxRiposteDelay: &lt;1 = counters sooner.</summary>
        public float RiposteDelayMul = 1f;
        /// <summary>Scales minMaxTimeBetweenAttack: &lt;1 = attacks more often.</summary>
        public float AttackIntervalMul = 1f;
        /// <summary>Scales melee animation speed.</summary>
        public float AnimSpeedMul = 1f;
        /// <summary>Actively place a single one-handed weapon in the strike's path (stock: disabled).</summary>
        public bool OneHandIK = false;
        /// <summary>How strongly (0-1) the hand position follows the strike when OneHandIK is on.</summary>
        public float OneHandIKPos = 0.1f;
        /// <summary>How strongly (0-1) the hand rotation follows the strike when OneHandIK is on.</summary>
        public float OneHandIKRot = 0.5f;
        /// <summary>Allow the director to pick sidesteps (needs the game's side-dodge animations).</summary>
        public bool SideDodge = false;

        // ---- Attack behaviour ----
        /// <summary>0..1: how strongly attack memory shapes the choice.</summary>
        public float VarietyStrength = 1f;
        /// <summary>Chance per attack to start it as a feint.</summary>
        public float FeintChance = 0f;
        /// <summary>Chance that a feint is followed by a second feint.</summary>
        public float DoubleFeintChance = 0f;
        /// <summary>Multiplier on FeintChance when the target is guarding the zone the attack heads for.</summary>
        public float ReactiveFeintBoost = 1.5f;
        /// <summary>Chance, per opening attack, to turn it into a chained sequence (kata) of several strikes.</summary>
        public float ComboChance = 0f;
        /// <summary>Longest sequence this archetype throws (total strikes, 2-6).</summary>
        public int ComboMaxLength = 3;
        /// <summary>
        /// 0-1: how likely the creature is to keep chaining when the opponent starts a dangerous strike
        /// (high = blind commitment, low = breaks off to defend as soon as it is threatened).
        /// </summary>
        public float ComboCommitment = 0.6f;
        /// <summary>Opponent strike danger level (0-1) from which a sequence may be interrupted.</summary>
        public float InterruptDanger = 0.6f;
        /// <summary>Weight multiplier for advancing attacks right after a successful parry (pressing the advantage).</summary>
        public float PressureBias = 1.3f;
        /// <summary>Chance to counter-attack right after a successful dodge.</summary>
        public float CounterAfterDodgeChance = 0f;

        // ---- Reading feints / fatigue ----
        /// <summary>Probability of NOT falling for a feint of the target.</summary>
        public float FeintReadSkill = 0.6f;
        /// <summary>Seconds the guard stays frozen when fooled.</summary>
        public float FooledDuration = 0.4f;
        /// <summary>Parries within FatigueWindow after which the creature gets overwhelmed.</summary>
        public int MaxParriesBeforeFatigue = 4;
        /// <summary>Length in seconds of the window in which parries are counted for fatigue.</summary>
        public float FatigueWindow = 3f;
        /// <summary>Seconds the creature cannot parry once overwhelmed.</summary>
        public float FatigueDuration = 0.8f;

        /// <summary>Per-creature random spread (0..0.5) applied to the multipliers so squads are not clones.</summary>
        public float Jitter = 0.08f;

        public SkillProfile Clone() => (SkillProfile)MemberwiseClone();

        public static Dictionary<string, SkillProfile> CreateDefaults()
        {
            return new Dictionary<string, SkillProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["Recruit"] = new SkillProfile
                {
                    ParryAdjustSpeedMul = 0.85f, AboutToBeHitMul = 0.9f, DodgeChanceMul = 0.8f,
                    RiposteChanceShift = -0.05f, RiposteDelayMul = 1.25f, AttackIntervalMul = 1.2f, AnimSpeedMul = 0.95f,
                    VarietyStrength = 0.5f, FeintChance = 0.00f, DoubleFeintChance = 0f, ComboChance = 0.25f, ComboMaxLength = 3, ComboCommitment = 0.9f, InterruptDanger = 0.8f, PressureBias = 1.0f,
                    CounterAfterDodgeChance = 0.10f, FeintReadSkill = 0.25f, FooledDuration = 0.55f,
                    MaxParriesBeforeFatigue = 3, FatigueDuration = 1.1f
                },
                ["Soldier"] = new SkillProfile
                {
                    ParryAdjustSpeedMul = 1.1f, AboutToBeHitMul = 1.0f, DodgeChanceMul = 1.0f,
                    RiposteChanceShift = 0.0f, RiposteDelayMul = 1.0f, AttackIntervalMul = 1.0f, AnimSpeedMul = 1.0f,
                    VarietyStrength = 0.8f, FeintChance = 0.08f, DoubleFeintChance = 0.0f, ComboChance = 0.40f, ComboMaxLength = 4, ComboCommitment = 0.6f, InterruptDanger = 0.6f, PressureBias = 1.3f,
                    CounterAfterDodgeChance = 0.25f, FeintReadSkill = 0.5f, FooledDuration = 0.45f,
                    MaxParriesBeforeFatigue = 4, FatigueDuration = 0.9f
                },
                ["Veteran"] = new SkillProfile
                {
                    ParryAdjustSpeedMul = 1.4f, AboutToBeHitMul = 1.15f, DodgeChanceMul = 1.2f,
                    RiposteChanceShift = 0.05f, RiposteDelayMul = 0.85f, AttackIntervalMul = 0.85f, AnimSpeedMul = 1.05f,
                    OneHandIK = true, OneHandIKPos = 0.15f, OneHandIKRot = 0.6f,
                    VarietyStrength = 1.0f, FeintChance = 0.18f, DoubleFeintChance = 0.04f, ReactiveFeintBoost = 1.7f,
                    ComboChance = 0.55f, ComboMaxLength = 5, ComboCommitment = 0.35f, InterruptDanger = 0.45f, PressureBias = 1.6f, CounterAfterDodgeChance = 0.4f, FeintReadSkill = 0.72f, FooledDuration = 0.38f,
                    MaxParriesBeforeFatigue = 5, FatigueDuration = 0.8f
                },
                ["Duelist"] = new SkillProfile
                {
                    ParryAdjustSpeedMul = 1.7f, AboutToBeHitMul = 1.3f, DodgeChanceMul = 1.4f,
                    RiposteChanceShift = 0.1f, RiposteDelayMul = 0.7f, AttackIntervalMul = 0.7f, AnimSpeedMul = 1.1f,
                    OneHandIK = true, OneHandIKPos = 0.25f, OneHandIKRot = 0.75f, SideDodge = true,
                    VarietyStrength = 1.0f, FeintChance = 0.3f, DoubleFeintChance = 0.1f, ReactiveFeintBoost = 2.0f,
                    ComboChance = 0.70f, ComboMaxLength = 6, ComboCommitment = 0.2f, InterruptDanger = 0.35f, PressureBias = 2.0f, CounterAfterDodgeChance = 0.55f, FeintReadSkill = 0.85f, FooledDuration = 0.3f,
                    MaxParriesBeforeFatigue = 6, FatigueDuration = 0.7f
                }
            };
        }

        /// <summary>Clamps every field to a safe range so a bad config can never produce absurd AI.</summary>
        public void Clamp()
        {
            ParryAdjustSpeedMul = Clamp(ParryAdjustSpeedMul, 0.2f, 4f);
            AboutToBeHitMul = Clamp(AboutToBeHitMul, 0.4f, 2.5f);
            DodgeChanceMul = Clamp(DodgeChanceMul, 0f, 3f);
            RiposteChanceShift = Clamp(RiposteChanceShift, -0.5f, 0.5f);
            RiposteDelayMul = Clamp(RiposteDelayMul, 0.2f, 3f);
            AttackIntervalMul = Clamp(AttackIntervalMul, 0.25f, 3f);
            AnimSpeedMul = Clamp(AnimSpeedMul, 0.6f, 1.6f);
            OneHandIKPos = Clamp(OneHandIKPos, 0f, 0.6f);
            OneHandIKRot = Clamp(OneHandIKRot, 0f, 1f);
            VarietyStrength = Clamp(VarietyStrength, 0f, 1f);
            FeintChance = Clamp(FeintChance, 0f, 0.8f);
            DoubleFeintChance = Clamp(DoubleFeintChance, 0f, 0.6f);
            ReactiveFeintBoost = Clamp(ReactiveFeintBoost, 1f, 4f);
            ComboChance = Clamp(ComboChance, 0f, 0.9f);
            ComboMaxLength = Math.Max(2, Math.Min(ComboMaxLength, 6));
            ComboCommitment = Clamp(ComboCommitment, 0f, 1f);
            InterruptDanger = Clamp(InterruptDanger, 0.05f, 1f);
            PressureBias = Clamp(PressureBias, 1f, 4f);
            CounterAfterDodgeChance = Clamp(CounterAfterDodgeChance, 0f, 0.95f);
            FeintReadSkill = Clamp(FeintReadSkill, 0f, 0.98f);
            FooledDuration = Clamp(FooledDuration, 0.05f, 1.5f);
            MaxParriesBeforeFatigue = Math.Max(1, Math.Min(MaxParriesBeforeFatigue, 20));
            FatigueWindow = Clamp(FatigueWindow, 0.5f, 15f);
            FatigueDuration = Clamp(FatigueDuration, 0.1f, 3f);
            Jitter = Clamp(Jitter, 0f, 0.5f);
        }

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);

        /// <summary>
        /// Returns a copy with the multiplicative fields spread by a deterministic per-creature factor
        /// in [1-Jitter, 1+Jitter], derived from <paramref name="noise"/> in [0,1).
        /// </summary>
        public SkillProfile WithJitter(float noise)
        {
            SkillProfile p = Clone();
            if (Jitter <= 0f) return p;
            float k = 1f + (noise * 2f - 1f) * Jitter;
            p.ParryAdjustSpeedMul *= k;
            p.AboutToBeHitMul *= 1f + (k - 1f) * 0.5f;
            p.AttackIntervalMul /= k;
            p.FeintReadSkill = Clamp(p.FeintReadSkill * (1f + (k - 1f) * 0.5f), 0f, 0.98f);
            p.Clamp();
            return p;
        }
    }
}
