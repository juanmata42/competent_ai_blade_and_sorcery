using CompetentAI.Core;
using Xunit;

namespace CompetentAI.Core.Tests
{
    public class PressureTests
    {
        [Fact]
        public void PressureGrowsWithSkillLevel()
        {
            var d = SkillProfile.CreateDefaults();
            string[] order = { "Recruit", "Soldier", "Veteran", "Duelist" };
            for (int i = 1; i < order.Length; i++)
            {
                var lo = d[order[i - 1]];
                var hi = d[order[i]];
                Assert.True(hi.ParryRecoilResist >= lo.ParryRecoilResist, order[i] + " recoil");
                Assert.True(hi.AttackDespiteDanger >= lo.AttackDespiteDanger, order[i] + " despite danger");
                Assert.True(hi.RangeDeltaMul >= lo.RangeDeltaMul, order[i] + " range");
                Assert.True(hi.LegDodgeChance >= lo.LegDodgeChance, order[i] + " leg dodge");
                Assert.True(hi.ThrustDodgeChance >= lo.ThrustDodgeChance, order[i] + " thrust dodge");
                Assert.True(hi.DodgeCooldownMul <= lo.DodgeCooldownMul, order[i] + " dodge cooldown");
                Assert.True(hi.ParryAdjustSpeedFloor >= lo.ParryAdjustSpeedFloor, order[i] + " parry floor");
            }
        }

        [Fact]
        public void AttackIntervalCapsAreOrderedAndOptional()
        {
            var d = SkillProfile.CreateDefaults();
            Assert.Equal(0f, d["Recruit"].AttackIntervalCapMax); // no cap: an easy brain keeps its own tempo
            foreach (var p in d.Values)
                if (p.AttackIntervalCapMax > 0f) Assert.True(p.AttackIntervalCapMax >= p.AttackIntervalCapMin);
            Assert.True(d["Duelist"].AttackIntervalCapMax < d["Veteran"].AttackIntervalCapMax);
        }

        [Fact]
        public void NewFieldsAreClampedAndOverridable()
        {
            var cfg = ConfigIO.Parse("{ \"Profiles\": { \"Soldier\": { \"ParryRecoilResist\": 7, \"RangeDeltaMul\": 50, \"DodgeCooldownMul\": 0, \"AttackIntervalCapMax\": 0.5 } } }", null);
            var s = cfg.Profiles["Soldier"];
            Assert.Equal(1f, s.ParryRecoilResist);
            Assert.Equal(3f, s.RangeDeltaMul);
            Assert.Equal(0.1f, s.DodgeCooldownMul);
            Assert.Equal(0.5f, s.AttackIntervalCapMax);
        }

        [Fact]
        public void KatasKeepChainingAfterBodyHitsByDefault()
        {
            Assert.True(new KataConfig().ChainAfterBodyHit);
        }
    }
}
