using CompetentAI.Core;
using Xunit;

namespace CompetentAI.Core.Tests
{
    public class OpeningTests
    {
        private static readonly OpeningConfig Cfg = new OpeningConfig();

        private static AttackCandidate C(string id, CutDirection d, float w = 1f)
        {
            return new AttackCandidate { Id = id, Direction = d, Weight = w, Kind = d == CutDirection.Thrust ? AttackKind.Thrust : AttackKind.Swing };
        }

        [Fact]
        public void Weapon_covers_its_zone_and_partly_the_neighbours()
        {
            var map = new GuardMap();
            map.AddWeapon(new Zone(ZoneHeight.High, ZoneSide.Left), Cfg);
            Assert.Equal(1f, map.Coverage(new Zone(ZoneHeight.High, ZoneSide.Left)));
            Assert.Equal(Cfg.NeighbourCover, map.Coverage(new Zone(ZoneHeight.Mid, ZoneSide.Center)));
            Assert.Equal(0f, map.Coverage(new Zone(ZoneHeight.Low, ZoneSide.Right)));
            Assert.Equal(1, map.Sources);
        }

        [Fact]
        public void Shield_covers_its_whole_neighbourhood()
        {
            var map = new GuardMap();
            map.AddShield(new Zone(ZoneHeight.Mid, ZoneSide.Left), Cfg);
            Assert.Equal(Cfg.ShieldCover, map.Coverage(new Zone(ZoneHeight.High, ZoneSide.Center)));
            Assert.Equal(0f, map.Coverage(new Zone(ZoneHeight.Mid, ZoneSide.Right)));
        }

        [Fact]
        public void Attacks_are_steered_to_the_open_side()
        {
            // The opponent holds the weapon high on its right: a cut that ends at its high-left is much more attractive.
            var map = new GuardMap();
            map.AddWeapon(new Zone(ZoneHeight.High, ZoneSide.Right), Cfg);
            var p = SkillProfile.CreateDefaults()["Duelist"];
            // UpRight ends at the defender's high LEFT; UpLeft ends at its high RIGHT (where the weapon is)
            float open = OpeningPlanner.WeightFactor(C("open", CutDirection.UpRight), map, p, Cfg);
            float covered = OpeningPlanner.WeightFactor(C("covered", CutDirection.UpLeft), map, p, Cfg);
            Assert.True(open > covered * 2f, $"{open} vs {covered}");
            Assert.Equal(1f, covered, 3);
        }

        [Fact]
        public void No_information_or_no_skill_means_no_steering()
        {
            var empty = new GuardMap();
            var p = SkillProfile.CreateDefaults()["Duelist"];
            Assert.Equal(1f, OpeningPlanner.WeightFactor(C("a", CutDirection.Down), empty, p, Cfg));
            var map = new GuardMap();
            map.AddWeapon(new Zone(ZoneHeight.Mid, ZoneSide.Center), Cfg);
            var none = p.Clone();
            none.OpeningSeeking = 0f;
            Assert.Equal(1f, OpeningPlanner.WeightFactor(C("a", CutDirection.Down), map, none, Cfg));
            Assert.Equal(1f, OpeningPlanner.WeightFactor(C("u", CutDirection.Unknown), map, p, Cfg));
        }

        [Fact]
        public void Better_fighters_steer_harder()
        {
            var map = new GuardMap();
            map.AddWeapon(new Zone(ZoneHeight.High, ZoneSide.Right), Cfg);
            var d = SkillProfile.CreateDefaults();
            var c = C("x", CutDirection.DownRight);
            Assert.True(OpeningPlanner.WeightFactor(c, map, d["Duelist"], Cfg) > OpeningPlanner.WeightFactor(c, map, d["Recruit"], Cfg));
        }

        [Fact]
        public void A_shield_plus_weapon_guard_can_be_tight_and_boosts_sequences()
        {
            var map = new GuardMap();
            map.AddShield(new Zone(ZoneHeight.Mid, ZoneSide.Center), Cfg);
            Assert.True(OpeningPlanner.IsTight(map, Cfg) == (map.BestOpenness() < Cfg.TightGuardThreshold));
            map.AddShield(new Zone(ZoneHeight.Mid, ZoneSide.Center), Cfg);
            var tight = new GuardMap();
            tight.AddShield(new Zone(ZoneHeight.Mid, ZoneSide.Left), Cfg);
            tight.AddShield(new Zone(ZoneHeight.Mid, ZoneSide.Right), Cfg);
            Assert.True(OpeningPlanner.IsTight(tight, Cfg));
            var p = SkillProfile.CreateDefaults()["Duelist"];
            Assert.Equal(p.TightGuardComboBoost, OpeningPlanner.ComboBonus(tight, p, Cfg));
            Assert.Equal(0f, OpeningPlanner.ComboBonus(new GuardMap(), p, Cfg));
        }

        [Fact]
        public void Tight_guard_raises_the_chance_to_start_a_sequence()
        {
            var p = SkillProfile.CreateDefaults()["Veteran"];
            var rng1 = new SystemRandomSource(11);
            var rng2 = new SystemRandomSource(11);
            int plain = 0, boosted = 0;
            for (int i = 0; i < 4000; i++)
            {
                if (KataPlanner.ShouldStart(p, rng1)) plain++;
                if (KataPlanner.ShouldStart(p, rng2, p.TightGuardComboBoost)) boosted++;
            }
            Assert.True(boosted > plain);
        }

        [Fact]
        public void After_a_cut_the_next_strike_is_often_a_thrust_but_not_after_a_thrust()
        {
            var cfg = new KataConfig();
            var variety = new VarietyConfig();
            var prof = SkillProfile.CreateDefaults()["Veteran"];
            var memory = new AttackMemory(4);
            var cut = C("cut", CutDirection.DownLeft);
            var cands = new System.Collections.Generic.List<AttackCandidate>
            {
                C("a", CutDirection.Right), C("b", CutDirection.Left), C("t", CutDirection.Thrust)
            };
            var thrustPrev = C("prevThrust", CutDirection.Thrust);
            var rng = new SystemRandomSource(5);
            int afterCut = 0, afterThrust = 0, n = 4000;
            for (int i = 0; i < n; i++)
            {
                int k = KataPlanner.ChooseNext(cut, cands, memory, variety, cfg, prof, true, rng);
                if (k >= 0 && cands[k].Id == "t") afterCut++;
                int j = KataPlanner.ChooseNext(thrustPrev, cands.GetRange(0, 3), memory, variety, cfg, prof, true, rng);
                if (j >= 0 && cands[j].Id == "t") afterThrust++;
            }
            Assert.True(afterCut > afterThrust, $"{afterCut} vs {afterThrust}");
            Assert.True(afterCut / (double)n > 0.35);
        }

        [Fact]
        public void Opening_profile_fields_are_ordered_and_clamped()
        {
            var d = SkillProfile.CreateDefaults();
            string[] order = { "Recruit", "Soldier", "Veteran", "Duelist" };
            for (int i = 1; i < order.Length; i++)
            {
                Assert.True(d[order[i]].OpeningSeeking >= d[order[i - 1]].OpeningSeeking);
                Assert.True(d[order[i]].ParryMutationChance >= d[order[i - 1]].ParryMutationChance);
                Assert.True(d[order[i]].TightGuardComboBoost >= d[order[i - 1]].TightGuardComboBoost);
            }
            var cfg = ConfigIO.Parse("{ \"Profiles\": { \"Soldier\": { \"OpeningSeeking\": 9, \"ParryMutationChance\": -3 } }, \"Openings\": { \"Gain\": 99 } }", null);
            Assert.Equal(1f, cfg.Profiles["Soldier"].OpeningSeeking);
            Assert.Equal(0f, cfg.Profiles["Soldier"].ParryMutationChance);
            Assert.Equal(10f, cfg.Openings.Gain);
            Assert.True(cfg.Modules.Openings);
        }
    }
}
