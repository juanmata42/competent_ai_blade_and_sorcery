using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompetentAI.Core;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CompetentAI.Core.Tests
{
    /// <summary>
    /// Replays the REAL stock stance data (extracted locally from bas.jsondb, never committed) through the rules.
    /// Skipped silently when the extracted data is not present on the machine.
    /// </summary>
    public class StockStanceReplayTests
    {
        private static string FindStance(string name)
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                string c = Path.Combine(dir, ".local", "basjson", "Stances", "Stance_" + name + ".json");
                if (File.Exists(c)) return c;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        private sealed class Motion
        {
            public string Id, Address, Declared;
            public float MinRange;
        }

        private static List<Motion> Load(string stance, string list)
        {
            string path = FindStance(stance);
            if (path == null) return null;
            JObject o = JObject.Parse(File.ReadAllText(path));
            return ((JArray)o[list]).Select(m => new Motion
            {
                Id = (string)m["id"],
                Address = (string)m["address"],
                Declared = (string)m["attackMotion"],
                MinRange = (float)m["minRange"]
            }).ToList();
        }

        private static WeaponProfile W(string id, DamageTraits t, AttackKinds data, bool two = false)
        {
            return new WeaponProfile { ItemId = id, Traits = t, DataKinds = data, TwoHanded = two, InfoKnown = true, Class = WeaponClassifier.Classify(id, t, two, false) };
        }

        // Stock filter (MeleeStanceData.MakeFilteredClone): declared kind must match the item's data flags.
        private static bool StockKeeps(Motion m, WeaponProfile w)
        {
            if (w.DataKinds == (AttackKinds.Swing | AttackKinds.Thrust)) return true;
            return (m.Declared == "Swing" && (w.DataKinds & AttackKinds.Swing) != 0) ||
                   (m.Declared == "Thrust" && (w.DataKinds & AttackKinds.Thrust) != 0);
        }

        private static List<string> OursKeeps(List<Motion> all, WeaponProfile w)
        {
            var rules = new AttackRules(new RulesConfig());
            var result = new List<string>();
            foreach (Motion m in all)
            {
                AttackKind declared = m.Declared == "Swing" ? AttackKind.Swing : (m.Declared == "Thrust" ? AttackKind.Thrust : AttackKind.Unknown);
                AttackKind kind = rules.ResolveKind(m.Id, m.Address, declared);
                var c = new AttackCandidate { Id = m.Id, Address = m.Address, DeclaredKind = declared, Kind = kind, MinRange = m.MinRange };
                bool dataOk = kind == AttackKind.Bash || (kind == AttackKind.Swing && (w.DataKinds & AttackKinds.Swing) != 0) ||
                              (kind == AttackKind.Thrust && (w.DataKinds & AttackKinds.Thrust) != 0);
                if (dataOk && rules.Evaluate(w, null, c).Allowed) result.Add(m.Id);
            }
            return result;
        }

        [Fact]
        public void Spear_recovers_the_chain_thrust_the_stock_filter_drops()
        {
            var chains = Load("HumanMelee1hStance", "chainAttacks");
            if (chains == null) return; // data not present on this machine
            WeaponProfile spear = W("SpearBladed", DamageTraits.Edge | DamageTraits.Point, AttackKinds.Thrust, true);

            List<string> stock = chains.Where(m => StockKeeps(m, spear)).Select(m => m.Id).ToList();
            List<string> ours = OursKeeps(chains, spear);

            Assert.DoesNotContain("CounterThrust", stock);
            Assert.Contains("CounterThrust", ours);
            Assert.DoesNotContain("CounterSwingRight", ours);
        }

        [Fact]
        public void Spear_keeps_both_stock_thrust_openers()
        {
            var openers = Load("HumanMelee1hStance", "openingAttacks");
            if (openers == null) return;
            WeaponProfile spear = W("SpearBladed", DamageTraits.Edge | DamageTraits.Point, AttackKinds.Thrust, true);
            List<string> ours = OursKeeps(openers, spear);
            Assert.Contains("Thrust", ours);
            Assert.Contains("LungeThrust", ours);
            Assert.DoesNotContain("SwingRightToLeft", ours);
        }

        [Fact]
        public void Stiletto_loses_every_swing_and_keeps_the_thrusts()
        {
            var openers = Load("HumanMelee1hStance", "openingAttacks");
            if (openers == null) return;
            WeaponProfile stiletto = W("DaggerSentaraStiletto", DamageTraits.Point, AttackKinds.Swing | AttackKinds.Thrust);
            List<string> stock = openers.Where(m => StockKeeps(m, stiletto)).Select(m => m.Id).ToList();
            List<string> ours = OursKeeps(openers, stiletto);
            Assert.Contains("SwingRightToLeft", stock);       // the game lets it cut
            Assert.DoesNotContain("SwingRightToLeft", ours);  // we do not
            Assert.Equal(new[] { "LungeThrust", "Thrust" }, ours.OrderBy(x => x).ToArray());
        }

        [Fact]
        public void Viennoise_sword_keeps_swings_and_loses_thrusts()
        {
            var openers = Load("HumanMelee1hStance", "openingAttacks");
            if (openers == null) return;
            WeaponProfile w = W("ViennoiseSword", DamageTraits.Blunt, AttackKinds.Swing | AttackKinds.Thrust);
            List<string> ours = OursKeeps(openers, w);
            Assert.DoesNotContain("Thrust", ours);
            Assert.DoesNotContain("LungeThrust", ours);
            Assert.Contains("SwingRightToLeft", ours);
        }

        [Fact]
        public void Common_sword_keeps_everything_the_game_gives_it()
        {
            foreach (string list in new[] { "openingAttacks", "chainAttacks", "ripostes" })
            {
                var all = Load("HumanMelee1hStance", list);
                if (all == null) return;
                WeaponProfile sword = W("SwordLongCommon", DamageTraits.Edge | DamageTraits.Point, AttackKinds.Swing | AttackKinds.Thrust);
                Assert.Equal(all.Count, OursKeeps(all, sword).Count);
            }
        }

        [Fact]
        public void Shield_stance_pushes_survive_with_a_shield_in_the_left_hand()
        {
            var openers = Load("HumanMeleeShieldStance", "openingAttacks");
            if (openers == null) return;
            var rules = new AttackRules(new RulesConfig());
            WeaponProfile sword = W("SwordLongCommon", DamageTraits.Edge | DamageTraits.Point, AttackKinds.Swing | AttackKinds.Thrust);
            var shield = new WeaponProfile { ItemId = "ShieldRound", Traits = DamageTraits.Blunt, DataKinds = AttackKinds.Swing, InfoKnown = true, Class = WeaponClass.Shield };
            foreach (Motion m in openers.Where(m => m.Id.StartsWith("Push")))
            {
                AttackKind kind = rules.ResolveKind(m.Id, m.Address, AttackKind.Swing);
                Assert.Equal(AttackKind.Bash, kind);
                var c = new AttackCandidate { Id = m.Id, Kind = kind, Side = Hand.Left };
                Assert.True(rules.Evaluate(sword, shield, c).Allowed);
            }
        }

        [Fact]
        public void Stock_attacks_get_sensible_name_based_directions()
        {
            var openers = Load("HumanMelee1hStance", "openingAttacks");
            if (openers == null) return;
            var rules = new AttackRules(new RulesConfig());
            var res = new DirectionResolver(new RulesConfig(), new CalibrationConfig(), new DirectionCalibration());
            var labelled = openers.ToDictionary(m => m.Id, m =>
                res.Resolve(m.Id, m.Address, rules.ResolveKind(m.Id, m.Address, m.Declared == "Thrust" ? AttackKind.Thrust : AttackKind.Swing), out _));
            Assert.Equal(CutDirection.Left, labelled["SwingRightToLeft"]);
            Assert.Equal(CutDirection.Right, labelled["SwingLeftToRight"]);
            Assert.Equal(CutDirection.DownLeft, labelled["SwingTopToLeft"]);
            Assert.Equal(CutDirection.DownRight, labelled["SwingTopToRight"]);
            Assert.Equal(CutDirection.Thrust, labelled["Thrust"]);
            Assert.Equal(CutDirection.Thrust, labelled["LungeThrust"]);
        }
    }
}
