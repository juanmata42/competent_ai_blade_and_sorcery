using System;

namespace CompetentAI.Core
{
    public enum ZoneHeight { Low = 0, Mid = 1, High = 2 }

    /// <summary>Horizontal band as seen from the DEFENDER's own left/right.</summary>
    public enum ZoneSide { Left = 0, Center = 1, Right = 2 }

    /// <summary>One cell of the 3x3 grid in front of a defender (the nine defensive zones).</summary>
    public readonly struct Zone : IEquatable<Zone>
    {
        public readonly ZoneHeight Height;
        public readonly ZoneSide Side;

        public Zone(ZoneHeight height, ZoneSide side)
        {
            Height = height;
            Side = side;
        }

        public bool Equals(Zone other) => Height == other.Height && Side == other.Side;
        public override bool Equals(object obj) => obj is Zone z && Equals(z);
        public override int GetHashCode() => ((int)Height * 3) + (int)Side;
        public override string ToString() => Height + "-" + Side;

        /// <summary>Chebyshev distance between zones (0 = same cell, 2 = opposite corners).</summary>
        public static int Distance(Zone a, Zone b) =>
            Math.Max(Math.Abs((int)a.Height - (int)b.Height), Math.Abs((int)a.Side - (int)b.Side));
    }

    public static class Zones
    {
        /// <summary>
        /// Maps a normalised point (x: defender's right = +1, y: up = +1, both in [-1,1]) to a zone.
        /// <paramref name="dead"/> is the half-size of the central band.
        /// </summary>
        public static Zone FromNormalized(float x, float y, float dead = 0.33f)
        {
            ZoneSide side = x < -dead ? ZoneSide.Left : (x > dead ? ZoneSide.Right : ZoneSide.Center);
            ZoneHeight height = y < -dead ? ZoneHeight.Low : (y > dead ? ZoneHeight.High : ZoneHeight.Mid);
            return new Zone(height, side);
        }

        /// <summary>
        /// Where a strike of the given direction ENDS, in the defender's frame. The attacker faces the defender,
        /// so a strike travelling to the attacker's Left ends on the defender's Right.
        /// </summary>
        public static Zone EndZone(CutDirection d)
        {
            switch (d)
            {
                case CutDirection.Down: return new Zone(ZoneHeight.Low, ZoneSide.Center);
                case CutDirection.DownLeft: return new Zone(ZoneHeight.Low, ZoneSide.Right);
                case CutDirection.DownRight: return new Zone(ZoneHeight.Low, ZoneSide.Left);
                case CutDirection.Left: return new Zone(ZoneHeight.Mid, ZoneSide.Right);
                case CutDirection.Right: return new Zone(ZoneHeight.Mid, ZoneSide.Left);
                case CutDirection.UpLeft: return new Zone(ZoneHeight.High, ZoneSide.Right);
                case CutDirection.UpRight: return new Zone(ZoneHeight.High, ZoneSide.Left);
                case CutDirection.Up: return new Zone(ZoneHeight.High, ZoneSide.Center);
                default: return new Zone(ZoneHeight.Mid, ZoneSide.Center); // thrust: centre mass
            }
        }

        /// <summary>Where a strike of the given direction STARTS, in the defender's frame.</summary>
        public static Zone StartZone(CutDirection d)
        {
            switch (d)
            {
                case CutDirection.Down: return new Zone(ZoneHeight.High, ZoneSide.Center);
                case CutDirection.DownLeft: return new Zone(ZoneHeight.High, ZoneSide.Left);
                case CutDirection.DownRight: return new Zone(ZoneHeight.High, ZoneSide.Right);
                case CutDirection.Left: return new Zone(ZoneHeight.Mid, ZoneSide.Left);
                case CutDirection.Right: return new Zone(ZoneHeight.Mid, ZoneSide.Right);
                case CutDirection.UpLeft: return new Zone(ZoneHeight.Low, ZoneSide.Left);
                case CutDirection.UpRight: return new Zone(ZoneHeight.Low, ZoneSide.Right);
                case CutDirection.Up: return new Zone(ZoneHeight.Low, ZoneSide.Center);
                default: return new Zone(ZoneHeight.Mid, ZoneSide.Center);
            }
        }

        /// <summary>
        /// The stock guard pose that best covers a zone. The game has seven poses; the two centre-top/centre-bottom
        /// cells have no dedicated pose and map to the neutral one.
        /// </summary>
        public static string GuardPoseId(Zone z)
        {
            switch (z.Height)
            {
                case ZoneHeight.High:
                    return z.Side == ZoneSide.Left ? "GuardUpperLeft" : (z.Side == ZoneSide.Right ? "GuardUpperRight" : "GuardNeutral");
                case ZoneHeight.Low:
                    return z.Side == ZoneSide.Left ? "GuardLowerLeft" : (z.Side == ZoneSide.Right ? "GuardLowerRight" : "GuardNeutral");
                default:
                    return z.Side == ZoneSide.Left ? "GuardMidLeft" : (z.Side == ZoneSide.Right ? "GuardMidRight" : "GuardNeutral");
            }
        }
    }
}
