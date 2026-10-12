using CompetentAI.Core;
using Xunit;

namespace CompetentAI.Core.Tests
{
    public class RulesTests
    {
        private static WeaponProfile W(string id, DamageTraits t, bool twoHanded = false, bool known = true)
        {
            return new WeaponProfile
            {
                ItemId = id,
                Traits = t,
                TwoHanded = twoHanded,
                InfoKnown = known,
                Class = WeaponClassifier.Classify(id, t, twoHanded, false)
            };
        }

        private static AttackCandidate A(string id, AttackKind kind, Hand side = Hand.Right, float minRange = 1f)
        {
            return new AttackCandidate { Id = id, Kind = kind, Side = side, MinRange = minRange };
        }

        [Theory]
        [InlineData("SwordSlash2H", DamageTraits.Edge)]
        [InlineData("SaberSlash1H", DamageTraits.Edge)]
        [InlineData("ChopperSlash2H", DamageTraits.Edge)]
        [InlineData("DaggerSlash", DamageTraits.Edge)]
        [InlineData("SwordPierce", DamageTraits.Point)]
        [InlineData("EdgelessDaggerPierce", DamageTraits.Point)]
        [InlineData("ChopperPierce", DamageTraits.Point)]
        [InlineData("RapierPierce", DamageTraits.Point)]
        [InlineData("WarpickPierce", DamageTraits.Point)]
        [InlineData("Mace1H", DamageTraits.Blunt)]
        [InlineData("Mace2H", DamageTraits.Blunt)]
        [InlineData("PropBluntHeavy", DamageTraits.Blunt)]
        [InlineData("ShieldStrong", DamageTraits.Blunt)]
        [InlineData("HandleHeavy", DamageTraits.None)]
        [InlineData("HandleLight", DamageTraits.None)]
        [InlineData("Punch", DamageTraits.None)]
        public void Damager_ids_map_to_traits(string id, DamageTraits expected)
        {
            Assert.Equal(expected, WeaponClassifier.TraitFromDamagerId(id));
        }

        [Theory]
        [InlineData("SwordLongCommon", DamageTraits.Edge | DamageTraits.Point, false, WeaponClass.Sword)]
        [InlineData("DaggerSentaraStiletto", DamageTraits.Point, false, WeaponClass.Dagger)]
        [InlineData("DaggerRaktaSai", DamageTraits.Point, false, WeaponClass.Dagger)]
        [InlineData("SwordSentaraDuelingSword", DamageTraits.Edge | DamageTraits.Point, false, WeaponClass.Rapier)]
        [InlineData("Rapier", DamageTraits.Edge | DamageTraits.Point, false, WeaponClass.Rapier)]
        [InlineData("SpearBladed", DamageTraits.Edge | DamageTraits.Point, true, WeaponClass.Spear)]
        [InlineData("AxeLongHalberd", DamageTraits.Edge | DamageTraits.Point, true, WeaponClass.Polearm)]
        [InlineData("AxeShortCommon", DamageTraits.Edge, false, WeaponClass.Axe)]
        [InlineData("MaceLongMaul", DamageTraits.Blunt, true, WeaponClass.Mace)]
        [InlineData("DualBladeStaff", DamageTraits.Edge | DamageTraits.Point, true, WeaponClass.Staff)]
        [InlineData("SomeModdedThing", DamageTraits.Blunt, false, WeaponClass.Mace)]
        [InlineData("SomeModdedThing", DamageTraits.None, false, WeaponClass.Unknown)]
        public void Weapon_class_is_derived_from_id_then_traits(string id, DamageTraits traits, bool twoHanded, WeaponClass expected)
        {
            Assert.Equal(expected, WeaponClassifier.Classify(id, traits, twoHanded, false));
        }

        [Fact]
        public void Edgeless_stiletto_cannot_swing_but_can_thrust()
        {
            var rules = new AttackRules(new RulesConfig());
            var stiletto = W("DaggerSentaraStiletto", DamageTraits.Point);
            Assert.False(rules.Evaluate(stiletto, null, A("SwingRightToLeft", AttackKind.Swing)).Allowed);
            Assert.True(rules.Evaluate(stiletto, null, A("Thrust", AttackKind.Thrust)).Allowed);
        }

        [Fact]
        public void Pure_blunt_weapon_cannot_thrust_but_can_swing()
        {
            var rules = new AttackRules(new RulesConfig());
            var viennoise = W("ViennoiseSword", DamageTraits.Blunt);
            Assert.False(rules.Evaluate(viennoise, null, A("Thrust", AttackKind.Thrust)).Allowed);
            Assert.True(rules.Evaluate(viennoise, null, A("SwingLeftToRight", AttackKind.Swing)).Allowed);
        }

        [Fact]
        public void Unknown_weapon_keeps_stock_behaviour()
        {
            var rules = new AttackRules(new RulesConfig());
            var modded = W("ModdedBlade", DamageTraits.None, known: false);
            Assert.True(rules.Evaluate(modded, null, A("Thrust", AttackKind.Thrust)).Allowed);
            Assert.True(rules.Evaluate(modded, null, A("SwingRightToLeft", AttackKind.Swing)).Allowed);
        }

        [Fact]
        public void Unknown_weapons_can_be_restricted_by_config()
        {
            var rules = new AttackRules(new RulesConfig { UnknownWeaponsAreUnrestricted = false });
            var modded = W("ModdedStick", DamageTraits.None, known: false);
            Assert.False(rules.Evaluate(modded, null, A("Thrust", AttackKind.Thrust)).Allowed);
        }

        [Fact]
        public void Dual_wield_attack_needs_both_weapons_to_allow_it()
        {
            var rules = new AttackRules(new RulesConfig());
            var sword = W("SwordShortCommon", DamageTraits.Edge | DamageTraits.Point);
            var stiletto = W("DaggerSentaraStiletto", DamageTraits.Point);
            var cross = A("CrossSlash", AttackKind.Swing, Hand.Both);
            Assert.False(rules.Evaluate(sword, stiletto, cross).Allowed);
            Assert.True(rules.Evaluate(sword, sword, cross).Allowed);
        }

        [Fact]
        public void Left_hand_attack_is_judged_against_the_left_weapon()
        {
            var rules = new AttackRules(new RulesConfig());
            var sword = W("SwordShortCommon", DamageTraits.Edge | DamageTraits.Point);
            var stiletto = W("DaggerSentaraStiletto", DamageTraits.Point);
            Assert.False(rules.Evaluate(sword, stiletto, A("MidLeftSwing", AttackKind.Swing, Hand.Left)).Allowed);
            Assert.True(rules.Evaluate(sword, stiletto, A("MidRightSwing", AttackKind.Swing, Hand.Right)).Allowed);
        }

        [Fact]
        public void Shield_bash_requires_a_shield()
        {
            var rules = new AttackRules(new RulesConfig());
            var shield = new WeaponProfile { ItemId = "ShieldRound", Traits = DamageTraits.Blunt, InfoKnown = true, Class = WeaponClass.Shield };
            var sword = W("SwordShortCommon", DamageTraits.Edge | DamageTraits.Point);
            var push = A("PushCloseLight", AttackKind.Bash, Hand.Left);
            Assert.True(rules.Evaluate(sword, shield, push).Allowed);
            Assert.False(rules.Evaluate(sword, sword, push).Allowed);
        }

        [Theory]
        [InlineData("CounterThrust", "Bas.Animation.Attack.Sword1h.Counter.Stab", AttackKind.Swing, AttackKind.Thrust)]
        [InlineData("Thrust", "Bas.Animation.Attack.Sword1h.Stance2.Stab", AttackKind.Thrust, AttackKind.Thrust)]
        [InlineData("LungeSwingTopToLeft", "x", AttackKind.Swing, AttackKind.Swing)]
        [InlineData("SwingCombo", "Bas.Animation.Attack.Sword1h.Stance1.Combo", AttackKind.Swing, AttackKind.Swing)]
        [InlineData("RiposteHighLeft", "Bas.Animation.Attack.Sword1h.Riposte.HighLeft", AttackKind.Swing, AttackKind.Swing)]
        [InlineData("PushFarHeavy", "Bas.Animation.Attack.Shield.PushCharge", AttackKind.Swing, AttackKind.Bash)]
        [InlineData("ChainThrustLeft", "Bas.Animation.Attack.DualWield.Counter.StabLeft", AttackKind.Thrust, AttackKind.Thrust)]
        public void Kind_is_reclassified_from_id_and_address(string id, string address, AttackKind declared, AttackKind expected)
        {
            var rules = new AttackRules(new RulesConfig());
            Assert.Equal(expected, rules.ResolveKind(id, address, declared));
        }

        [Fact]
        public void Class_bias_scales_weights_and_lunges_are_detected()
        {
            var rules = new AttackRules(new RulesConfig());
            var rapier = W("Rapier", DamageTraits.Edge | DamageTraits.Point);
            RuleVerdict thrust = rules.Evaluate(rapier, null, A("Thrust", AttackKind.Thrust));
            RuleVerdict swing = rules.Evaluate(rapier, null, A("SwingRightToLeft", AttackKind.Swing));
            Assert.True(thrust.Bias > swing.Bias * 4f);
            Assert.True(rules.LooksLikeLunge("LungeThrust", 2.0f));
            Assert.True(rules.LooksLikeLunge("X", 2.5f));
            Assert.False(rules.LooksLikeLunge("SwingRightToLeft", 1.05f));
        }

        [Fact]
        public void Disabled_rules_allow_everything()
        {
            var rules = new AttackRules(new RulesConfig { Enabled = false });
            var stiletto = W("DaggerSentaraStiletto", DamageTraits.Point);
            Assert.True(rules.Evaluate(stiletto, null, A("SwingRightToLeft", AttackKind.Swing)).Allowed);
        }
    }
}
