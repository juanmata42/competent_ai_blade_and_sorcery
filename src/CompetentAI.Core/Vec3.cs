using System;

namespace CompetentAI.Core
{
    /// <summary>Minimal engine-independent vector so the decision logic can be unit-tested outside Unity.</summary>
    public readonly struct Vec3
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);

        public float SqrMagnitude => X * X + Y * Y + Z * Z;
        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);

        public Vec3 Normalized
        {
            get
            {
                float m = Magnitude;
                return m > 1e-6f ? new Vec3(X / m, Y / m, Z / m) : Zero;
            }
        }

        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        /// <summary>Angle in degrees between two direction vectors (0 when either is ~zero).</summary>
        public static float Angle(Vec3 a, Vec3 b)
        {
            float ma = a.Magnitude, mb = b.Magnitude;
            if (ma < 1e-6f || mb < 1e-6f) return 0f;
            float c = Dot(a, b) / (ma * mb);
            if (c > 1f) c = 1f;
            if (c < -1f) c = -1f;
            return (float)(Math.Acos(c) * 180.0 / Math.PI);
        }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);

        public override string ToString() => $"({X:0.00},{Y:0.00},{Z:0.00})";
    }
}
