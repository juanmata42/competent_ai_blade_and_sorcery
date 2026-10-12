using System;

namespace CompetentAI.Core
{
    /// <summary>
    /// Watches the incoming strike's velocity and tells when it makes a sharp change of direction
    /// (a feint or a redirection). Feed it every fixed step while a strike is incoming.
    /// </summary>
    public sealed class StrikeTracker
    {
        private struct Sample
        {
            public float Time;
            public Vec3 Dir;
            /// <summary>Cosine between the motion and the direction towards the defender (positive = approaching).</summary>
            public float Approach;
            /// <summary>The game judged the strike to be on course to hit the defender.</summary>
            public bool OnCourse;
        }

        private const int Capacity = 32;
        private readonly Sample[] _samples = new Sample[Capacity];
        private int _head;
        private int _count;
        private float _latchedUntil;

        public void Reset()
        {
            _head = 0;
            _count = 0;
            _latchedUntil = 0f;
        }

        /// <summary>
        /// Returns true exactly once per reversal. <paramref name="towardDefender"/> is the direction from the strike
        /// point to the defender: only a motion that was APPROACHING the defender and then changes direction counts
        /// as a feint. A back-swing followed by the real strike (away, then towards) is a normal attack, not a feint.
        /// </summary>
        public bool Update(float now, Vec3 velocity, Vec3 towardDefender, bool onHitCourse, DefenseConfig cfg, out float angle)
        {
            angle = 0f;
            if (velocity.Magnitude < cfg.MinStrikeSpeed) return false;

            Vec3 dir = velocity.Normalized;
            // drop samples older than the window
            while (_count > 0 && now - Get(_count - 1).Time > cfg.ReversalWindow) _count--;

            bool reversed = false;
            if (_count > 0 && now >= _latchedUntil)
            {
                Sample oldest = Get(_count - 1);
                float a = Vec3.Angle(oldest.Dir, dir);
                if (a >= cfg.ReversalAngle && oldest.Approach > 0.2f && (oldest.OnCourse || !cfg.RequireHitCourse))
                {
                    reversed = true;
                    angle = a;
                    _latchedUntil = now + cfg.ReversalWindow;
                    _count = 0; // restart observation from the new direction
                }
            }

            _head = (_head + 1) % Capacity;
            Vec3 toward = towardDefender.Normalized;
            _samples[_head] = new Sample { Time = now, Dir = dir, OnCourse = onHitCourse, Approach = toward.SqrMagnitude < 0.5f ? 1f : Vec3.Dot(dir, toward) };
            if (_count < Capacity) _count++;
            return reversed;
        }

        // age 0 = newest
        private Sample Get(int index)
        {
            int idx = (_head - index + Capacity * 2) % Capacity;
            return _samples[idx];
        }
    }

    public enum FeintOutcome
    {
        None,
        Fooled,
        Read
    }

    /// <summary>Decides whether an NPC falls for a feint and for how long its guard reacts accordingly.</summary>
    public sealed class FeintReader
    {
        private readonly SkillProfile _p;
        private readonly DefenseConfig _cfg;
        private readonly IRandom _rng;
        private float _until;
        private FeintOutcome _state;

        public FeintReader(SkillProfile p, DefenseConfig cfg, IRandom rng)
        {
            _p = p;
            _cfg = cfg;
            _rng = rng;
        }

        public FeintOutcome Current(float now) => now < _until ? _state : FeintOutcome.None;

        public FeintOutcome OnReversal(float now)
        {
            bool read = _rng.Value() < _p.FeintReadSkill;
            _state = read ? FeintOutcome.Read : FeintOutcome.Fooled;
            _until = now + (read ? 0.3f : _p.FooledDuration);
            return _state;
        }

        /// <summary>Multiplier for the guard-follow speed.</summary>
        public float SpeedFactor(float now)
        {
            switch (Current(now))
            {
                case FeintOutcome.Fooled: return _cfg.FooledSpeedFactor;
                case FeintOutcome.Read: return _cfg.ReadSpeedFactor;
                default: return 1f;
            }
        }

        public bool RollFooledDodge() => _rng.Value() < _cfg.FooledDodgeChance;
    }

    /// <summary>Stops an NPC from being unbeatable: sustained parrying overwhelms it for a moment.</summary>
    public sealed class ParryFatigue
    {
        private readonly SkillProfile _p;
        private readonly float[] _times;
        private int _n;
        private float _overwhelmedUntil;

        public ParryFatigue(SkillProfile p)
        {
            _p = p;
            _times = new float[Math.Max(2, p.MaxParriesBeforeFatigue + 1)];
        }

        public bool IsOverwhelmed(float now) => now < _overwhelmedUntil;

        /// <summary>Registers a successful parry. Returns true when this parry tipped the NPC into overwhelmed.</summary>
        public bool RegisterParry(float now)
        {
            // forget old parries
            int keep = 0;
            for (int i = 0; i < _n; i++)
                if (now - _times[i] <= _p.FatigueWindow) _times[keep++] = _times[i];
            _n = keep;
            if (_n < _times.Length) _times[_n++] = now;
            else
            {
                Array.Copy(_times, 1, _times, 0, _times.Length - 1);
                _times[_times.Length - 1] = now;
            }
            if (_n >= _p.MaxParriesBeforeFatigue && now >= _overwhelmedUntil)
            {
                _overwhelmedUntil = now + _p.FatigueDuration;
                _n = 0;
                return true;
            }
            return false;
        }
    }
}
