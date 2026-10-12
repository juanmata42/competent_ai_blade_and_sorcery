using System;

namespace CompetentAI.Core
{
    /// <summary>
    /// The nine attack angles: eight cuts plus the thrust. A cut direction names where the weapon TRAVELS,
    /// expressed in the ATTACKER's local frame (x = attacker's right, y = up, z = forward).
    /// Example: a right-to-left horizontal slash travels towards the attacker's left → <see cref="Left"/>.
    /// </summary>
    public enum CutDirection
    {
        Unknown = 0,
        Down,
        DownLeft,
        Left,
        UpLeft,
        Up,
        UpRight,
        Right,
        DownRight,
        Thrust
    }

    public static class Directions
    {
        /// <summary>The nine real directions (without <see cref="CutDirection.Unknown"/>).</summary>
        public static readonly CutDirection[] All =
        {
            CutDirection.Down, CutDirection.DownLeft, CutDirection.Left, CutDirection.UpLeft,
            CutDirection.Up, CutDirection.UpRight, CutDirection.Right, CutDirection.DownRight,
            CutDirection.Thrust
        };

        /// <summary>Fraction of the motion that must point forward for it to count as a thrust.</summary>
        public const float DefaultThrustRatio = 0.8f;

        /// <summary>Minimum travelled distance (metres) for a measured motion to be classified at all.</summary>
        public const float MinMeasurableTravel = 0.15f;

        /// <summary>
        /// Classifies a motion vector given in the attacker's local frame.
        /// Returns Unknown when the vector is (almost) zero.
        /// </summary>
        public static CutDirection Classify(Vec3 localMotion, float thrustRatio = DefaultThrustRatio)
        {
            float mag = localMotion.Magnitude;
            if (mag < 1e-4f) return CutDirection.Unknown;
            if (localMotion.Z / mag >= thrustRatio) return CutDirection.Thrust;

            double deg = Math.Atan2(localMotion.Y, localMotion.X) * 180.0 / Math.PI; // [-180,180]
            if (deg > 157.5 || deg <= -157.5) return CutDirection.Left;
            if (deg > 112.5) return CutDirection.UpLeft;
            if (deg > 67.5) return CutDirection.Up;
            if (deg > 22.5) return CutDirection.UpRight;
            if (deg > -22.5) return CutDirection.Right;
            if (deg > -67.5) return CutDirection.DownRight;
            if (deg > -112.5) return CutDirection.Down;
            return CutDirection.DownLeft;
        }

        /// <summary>
        /// Classifies a MEASURED weapon-tip path (attacker local frame) knowing where the blade points. A strike is a thrust
        /// only when the tip travels along the blade axis (forwards); otherwise the direction is taken from the lateral part
        /// of the motion (x, y) alone, because the arm extending towards the target adds a large forward component to every
        /// swing. Returns Unknown when there is not enough lateral travel to tell.
        /// </summary>
        public static CutDirection ClassifyMeasured(Vec3 tipMotionLocal, Vec3 bladeAxisLocal, float thrustAlongAxis = 0.8f, float minLateral = 0.2f)
        {
            float len = tipMotionLocal.Magnitude;
            if (len < MinMeasurableTravel) return CutDirection.Unknown;
            Vec3 axis = bladeAxisLocal.Normalized;
            if (axis.SqrMagnitude > 0.5f && Vec3.Dot(tipMotionLocal, axis) / len >= thrustAlongAxis) return CutDirection.Thrust;
            float lateral = (float)Math.Sqrt(tipMotionLocal.X * tipMotionLocal.X + tipMotionLocal.Y * tipMotionLocal.Y);
            if (lateral < minLateral) return CutDirection.Unknown;
            return Classify(new Vec3(tipMotionLocal.X, tipMotionLocal.Y, 0f));
        }

        /// <summary>
        /// Converts a world-space strike velocity seen from the DEFENDER (given in the defender's local frame)
        /// into the attacker's local frame, so incoming strikes and own attacks use the same labels.
        /// The attacker faces the defender, so x and z are mirrored.
        /// </summary>
        public static Vec3 DefenderLocalToAttackerLocal(Vec3 defenderLocalVelocity)
        {
            return new Vec3(-defenderLocalVelocity.X, defenderLocalVelocity.Y, -defenderLocalVelocity.Z);
        }

        public static bool IsCut(CutDirection d) => d != CutDirection.Unknown && d != CutDirection.Thrust;

        public static bool IsHorizontal(CutDirection d) => d == CutDirection.Left || d == CutDirection.Right;
        public static bool IsVertical(CutDirection d) => d == CutDirection.Up || d == CutDirection.Down;
        public static bool IsDiagonal(CutDirection d) =>
            d == CutDirection.DownLeft || d == CutDirection.DownRight || d == CutDirection.UpLeft || d == CutDirection.UpRight;

        public static bool IsDescending(CutDirection d) =>
            d == CutDirection.Down || d == CutDirection.DownLeft || d == CutDirection.DownRight;

        public static bool IsRising(CutDirection d) =>
            d == CutDirection.Up || d == CutDirection.UpLeft || d == CutDirection.UpRight;

        /// <summary>Swap left and right (mirror the attacker's handedness). Thrust and vertical are unchanged.</summary>
        public static CutDirection Mirror(CutDirection d)
        {
            switch (d)
            {
                case CutDirection.Left: return CutDirection.Right;
                case CutDirection.Right: return CutDirection.Left;
                case CutDirection.DownLeft: return CutDirection.DownRight;
                case CutDirection.DownRight: return CutDirection.DownLeft;
                case CutDirection.UpLeft: return CutDirection.UpRight;
                case CutDirection.UpRight: return CutDirection.UpLeft;
                default: return d;
            }
        }

        /// <summary>The cut that travels the opposite way (a natural follow-up or return cut). Thrust maps to itself.</summary>
        public static CutDirection Opposite(CutDirection d)
        {
            switch (d)
            {
                case CutDirection.Down: return CutDirection.Up;
                case CutDirection.Up: return CutDirection.Down;
                case CutDirection.Left: return CutDirection.Right;
                case CutDirection.Right: return CutDirection.Left;
                case CutDirection.DownLeft: return CutDirection.UpRight;
                case CutDirection.UpRight: return CutDirection.DownLeft;
                case CutDirection.DownRight: return CutDirection.UpLeft;
                case CutDirection.UpLeft: return CutDirection.DownRight;
                default: return d;
            }
        }

        /// <summary>
        /// Angular distance between two directions in 45° steps (0..4). Thrust is 3 steps from every cut
        /// (it is "different from all of them") and 0 from itself.
        /// </summary>
        public static int Steps(CutDirection a, CutDirection b)
        {
            if (a == CutDirection.Unknown || b == CutDirection.Unknown) return 0;
            if (a == b) return 0;
            if (a == CutDirection.Thrust || b == CutDirection.Thrust) return 3;
            int d = Math.Abs(Ring(a) - Ring(b));
            return d > 4 ? 8 - d : d;
        }

        // Counter-clockwise order of the eight cuts starting at Right (0°).
        private static int Ring(CutDirection d)
        {
            switch (d)
            {
                case CutDirection.Right: return 0;
                case CutDirection.UpRight: return 1;
                case CutDirection.Up: return 2;
                case CutDirection.UpLeft: return 3;
                case CutDirection.Left: return 4;
                case CutDirection.DownLeft: return 5;
                case CutDirection.Down: return 6;
                case CutDirection.DownRight: return 7;
                default: return 0;
            }
        }

        public static string ShortName(CutDirection d) => d.ToString();
    }
}
