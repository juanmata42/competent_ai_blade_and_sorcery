using System;
using System.Collections.Generic;

namespace CompetentAI.Core
{
    public enum AttackKind
    {
        Unknown = 0,
        Swing,
        Thrust,
        Bash
    }

    public enum Hand
    {
        Right = 0,
        Left = 1,
        Both = 2
    }

    /// <summary>A candidate attack, decoupled from the engine's <c>AttackMotion</c>.</summary>
    public sealed class AttackCandidate
    {
        public string Id = "";
        public string Address = "";
        public AttackKind DeclaredKind = AttackKind.Unknown;
        public AttackKind Kind = AttackKind.Unknown;
        public CutDirection Direction = CutDirection.Unknown;
        public Hand Side = Hand.Right;
        public float MinRange;
        public float MaxRange;
        public float Weight = 1f;
        public bool IsLunge;
        /// <summary>The attack moves the body forward (step / lunge variants).</summary>
        public bool Advances;
        /// <summary>The attack comes from the stance's chain (follow-up) list.</summary>
        public bool IsChain;
        public string SourceIdle = "Any";

        /// <summary>Opaque handle to the engine object (set by the integration layer).</summary>
        public object Tag;

        public override string ToString() => $"{Id}[{Kind},{Direction},{Side},w={Weight:0.##}]";
    }

    public sealed class RuleVerdict
    {
        public bool Allowed = true;
        public float Bias = 1f;
        public string Reason = "ok";
    }

    /// <summary>Per weapon-class multipliers applied on top of the attack's own weight.</summary>
    public sealed class ClassBias
    {
        public float Swing = 1f;
        public float Thrust = 1f;
        public float Lunge = 1f;
        public float Bash = 1f;

        public ClassBias()
        {
        }

        public ClassBias(float swing, float thrust, float lunge, float bash = 1f)
        {
            Swing = swing;
            Thrust = thrust;
            Lunge = lunge;
            Bash = bash;
        }
    }

    /// <summary>
    /// Decides which attacks are legitimate for the weapon(s) in hand, using the weapon's REAL damage traits
    /// (damagers) rather than only the item's coarse Swing/Thrust flags. Restrictive only when the weapon is
    /// known; permissive (stock behaviour) when it is not.
    /// </summary>
    public sealed class AttackRules
    {
        private readonly RulesConfig _cfg;

        public AttackRules(RulesConfig cfg)
        {
            _cfg = cfg ?? new RulesConfig();
        }

        /// <summary>
        /// Resolves the effective attack kind. The game declares some thrusts as Swing (e.g. "CounterThrust"),
        /// which starves spears of attacks; the id/address keywords are more reliable.
        /// </summary>
        public AttackKind ResolveKind(string id, string address, AttackKind declared)
        {
            if (!_cfg.ReclassifyKindsById) return declared;
            string t = ((id ?? "") + "|" + (address ?? "")).ToLowerInvariant();
            if (t.Contains("push") || t.Contains("bash") || t.Contains("shieldcharge")) return AttackKind.Bash;
            bool swingWord = t.Contains("swing") || t.Contains("slash") || t.Contains("chop") || t.Contains("cut");
            bool thrustWord = t.Contains("thrust") || t.Contains("stab") || t.Contains("pierce");
            if (swingWord && !thrustWord) return AttackKind.Swing;
            if (thrustWord && !swingWord) return AttackKind.Thrust;
            return declared;
        }

        /// <summary>Name hints for attacks that advance. Measurements (see DirectionCalibration) supersede them.</summary>
        public bool LooksLikeAdvance(string id, string address, float minRange)
        {
            string t = ((id ?? "") + "|" + (address ?? "")).ToLowerInvariant();
            return t.Contains("step") || t.Contains("lunge") || minRange >= _cfg.LungeMinRange;
        }

        public bool LooksLikeLunge(string id, float minRange)
        {
            return (id != null && id.IndexOf("lunge", StringComparison.OrdinalIgnoreCase) >= 0) ||
                   minRange >= _cfg.LungeMinRange;
        }

        public RuleVerdict Evaluate(WeaponProfile right, WeaponProfile left, AttackCandidate c)
        {
            var v = new RuleVerdict();
            if (!_cfg.Enabled) return v;

            WeaponProfile w1, w2 = null;
            switch (c.Side)
            {
                case Hand.Left: w1 = left; break;
                case Hand.Both: w1 = right; w2 = left; break;
                default: w1 = right; break;
            }

            if (!Permits(w1, c.Kind, out string reason1)) return Deny(v, reason1);
            if (w2 != null && !Permits(w2, c.Kind, out string reason2)) return Deny(v, reason2);

            WeaponProfile primary = w1 ?? w2;
            v.Bias = BiasFor(primary, c);
            return v;
        }

        private bool Permits(WeaponProfile w, AttackKind kind, out string reason)
        {
            reason = "ok";
            if (w == null) return true; // nothing to judge against: stay permissive
            if (!w.InfoKnown && _cfg.UnknownWeaponsAreUnrestricted) return true;

            switch (kind)
            {
                case AttackKind.Swing:
                    if (!w.HasEdge && !w.HasBlunt)
                    {
                        reason = "swing needs an edge or a blunt head (" + w.ItemId + " has neither)";
                        return false;
                    }
                    return true;
                case AttackKind.Thrust:
                    if (!w.HasPoint)
                    {
                        reason = "thrust needs a point (" + w.ItemId + " has none)";
                        return false;
                    }
                    return true;
                case AttackKind.Bash:
                    if (!w.IsShield && w.InfoKnown)
                    {
                        reason = "bash needs a shield (" + w.ItemId + ")";
                        return false;
                    }
                    return true;
                default:
                    return true;
            }
        }

        private float BiasFor(WeaponProfile w, AttackCandidate c)
        {
            if (w == null) return 1f;
            ClassBias b = null;
            if (_cfg.ClassBias != null) _cfg.ClassBias.TryGetValue(w.Class.ToString(), out b);
            if (b == null) return 1f;
            float bias;
            switch (c.Kind)
            {
                case AttackKind.Swing: bias = b.Swing; break;
                case AttackKind.Thrust: bias = b.Thrust; break;
                case AttackKind.Bash: bias = b.Bash; break;
                default: bias = 1f; break;
            }
            if (c.IsLunge) bias *= b.Lunge;
            return bias < 0f ? 0f : bias;
        }

        private static RuleVerdict Deny(RuleVerdict v, string reason)
        {
            v.Allowed = false;
            v.Bias = 0f;
            v.Reason = reason;
            return v;
        }
    }
}
