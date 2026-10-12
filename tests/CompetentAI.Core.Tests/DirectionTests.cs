using CompetentAI.Core;
using Xunit;

namespace CompetentAI.Core.Tests
{
    public class DirectionTests
    {
        [Theory]
        [InlineData(0f, -1f, 0f, CutDirection.Down)]
        [InlineData(-1f, -1f, 0f, CutDirection.DownLeft)]
        [InlineData(-1f, 0f, 0f, CutDirection.Left)]
        [InlineData(-1f, 1f, 0f, CutDirection.UpLeft)]
        [InlineData(0f, 1f, 0f, CutDirection.Up)]
        [InlineData(1f, 1f, 0f, CutDirection.UpRight)]
        [InlineData(1f, 0f, 0f, CutDirection.Right)]
        [InlineData(1f, -1f, 0f, CutDirection.DownRight)]
        [InlineData(0f, 0f, 1f, CutDirection.Thrust)]
        [InlineData(0.2f, 0.1f, 1f, CutDirection.Thrust)]
        [InlineData(0.6f, -0.6f, 0.8f, CutDirection.DownRight)]
        public void Classify_maps_motion_vectors_to_the_nine_directions(float x, float y, float z, CutDirection expected)
        {
            Assert.Equal(expected, Directions.Classify(new Vec3(x, y, z)));
        }

        [Fact]
        public void Classify_zero_vector_is_unknown()
        {
            Assert.Equal(CutDirection.Unknown, Directions.Classify(Vec3.Zero));
        }

        [Fact]
        public void Motion_well_off_the_forward_axis_is_a_cut_and_close_to_it_is_a_thrust()
        {
            Assert.Equal(CutDirection.Right, Directions.Classify(new Vec3(1f, 0f, 1f)));     // 45 degrees off forward: a cut
            Assert.Equal(CutDirection.Thrust, Directions.Classify(new Vec3(0.5f, 0f, 2f))); // ~14 degrees off forward: a thrust
        }

        [Fact]
        public void Defender_frame_is_mirrored_into_attacker_frame()
        {
            // Strike moving along the defender's +x (defender's right) while approaching (-z):
            // for the attacker facing the defender that is travelling to the attacker's left.
            Vec3 attackerLocal = Directions.DefenderLocalToAttackerLocal(new Vec3(1f, 0f, -0.1f));
            Assert.Equal(CutDirection.Left, Directions.Classify(attackerLocal));
        }

        [Theory]
        [InlineData(0f, 0f, 0.8f, 0f, 0f, 1f, CutDirection.Thrust)]        // tip moves along the blade axis
        [InlineData(0.05f, 0.1f, 0.7f, 0.1f, 0f, 1f, CutDirection.Thrust)]
        [InlineData(1.09f, -0.15f, 0.90f, 0f, 0f, 1f, CutDirection.Right)] // measured in game: RightToLeft, arm extends forward too
        [InlineData(-0.61f, -0.16f, 0.16f, 0f, 0f, 1f, CutDirection.Left)]
        [InlineData(0.4f, -0.5f, 0.6f, 0f, 0f, 1f, CutDirection.DownRight)]
        [InlineData(0f, 0.6f, 0.5f, 0f, 0f, 1f, CutDirection.Up)]
        public void Measured_tip_paths_use_the_blade_axis_for_thrusts_and_the_lateral_part_for_cuts(
            float mx, float my, float mz, float ax, float ay, float az, CutDirection expected)
        {
            Assert.Equal(expected, Directions.ClassifyMeasured(new Vec3(mx, my, mz), new Vec3(ax, ay, az)));
        }

        [Fact]
        public void Measured_paths_too_short_or_without_lateral_travel_are_not_classified()
        {
            Assert.Equal(CutDirection.Unknown, Directions.ClassifyMeasured(new Vec3(0.05f, 0.02f, 0.05f), new Vec3(0f, 0f, 1f)));
            // forward-only motion while the blade points sideways: neither a thrust nor a readable cut
            Assert.Equal(CutDirection.Unknown, Directions.ClassifyMeasured(new Vec3(0.05f, 0.05f, 0.9f), new Vec3(1f, 0f, 0f)));
        }

        [Fact]
        public void Opposite_and_mirror_are_involutions()
        {
            foreach (CutDirection d in Directions.All)
            {
                Assert.Equal(d, Directions.Opposite(Directions.Opposite(d)));
                Assert.Equal(d, Directions.Mirror(Directions.Mirror(d)));
            }
        }

        [Fact]
        public void Steps_are_symmetric_and_thrust_is_distinct_from_cuts()
        {
            foreach (CutDirection a in Directions.All)
                foreach (CutDirection b in Directions.All)
                    Assert.Equal(Directions.Steps(a, b), Directions.Steps(b, a));
            Assert.Equal(4, Directions.Steps(CutDirection.Left, CutDirection.Right));
            Assert.Equal(1, Directions.Steps(CutDirection.Left, CutDirection.DownLeft));
            Assert.Equal(3, Directions.Steps(CutDirection.Thrust, CutDirection.Down));
            Assert.Equal(0, Directions.Steps(CutDirection.Thrust, CutDirection.Thrust));
        }

        [Fact]
        public void Zone_mapping_is_mirrored_between_attacker_and_defender()
        {
            Assert.Equal(new Zone(ZoneHeight.Mid, ZoneSide.Right), Zones.EndZone(CutDirection.Left));
            Assert.Equal(new Zone(ZoneHeight.Mid, ZoneSide.Left), Zones.StartZone(CutDirection.Left));
            Assert.Equal("GuardUpperLeft", Zones.GuardPoseId(new Zone(ZoneHeight.High, ZoneSide.Left)));
            Assert.Equal("GuardNeutral", Zones.GuardPoseId(new Zone(ZoneHeight.High, ZoneSide.Center)));
        }

        [Fact]
        public void FromNormalized_uses_a_central_dead_band()
        {
            Assert.Equal(new Zone(ZoneHeight.Mid, ZoneSide.Center), Zones.FromNormalized(0f, 0f));
            Assert.Equal(new Zone(ZoneHeight.High, ZoneSide.Right), Zones.FromNormalized(0.8f, 0.9f));
            Assert.Equal(new Zone(ZoneHeight.Low, ZoneSide.Left), Zones.FromNormalized(-0.8f, -0.9f));
        }
    }
}
