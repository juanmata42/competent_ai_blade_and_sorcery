using System;
using System.Text;

namespace CompetentAI.Core
{
    /// <summary>
    /// Tuning of "attack where the opponent is open": the opponent's weapon(s) and shield are mapped onto the 3x3 defence grid
    /// (<see cref="Zones"/>), and attacks that end in uncovered cells are favoured. Also the cut-then-thrust change
    /// ("mutation"): a cut that gets parried turns into a thrust.
    /// </summary>
    public sealed class OpeningConfig
    {
        /// <summary>How much a weapon in one cell also covers the 8 neighbouring cells (0-1).</summary>
        public float NeighbourCover = 0.45f;

        /// <summary>Cover a held shield gives to its own cell and to every neighbour (0-1).</summary>
        public float ShieldCover = 0.8f;

        /// <summary>Weight multiplier of a fully open cell is 1 + Gain x profile.OpeningSeeking.</summary>
        public float Gain = 3f;

        /// <summary>When even the most open cell is less open than this, the guard is "tight": no gaps to aim at.</summary>
        public float TightGuardThreshold = 0.45f;

        /// <summary>Extra distance tolerance (metres) when looking for the thrust a parried cut turns into.</summary>
        public float MutationRangeTolerance = 0.6f;

        /// <summary>Do not mutate again within this many seconds of the last mutation (avoids a thrust-thrust-thrust loop).</summary>
        public float MutationCooldown = 2.5f;
    }

    /// <summary>How well the opponent's weapon(s) cover each of the nine zones in front of them (0 = open, 1 = covered).</summary>
    public sealed class GuardMap
    {
        private readonly float[] _cover = new float[9];

        /// <summary>Number of weapons/shields that were mapped. 0 = unknown (no information, do not steer).</summary>
        public int Sources { get; private set; }

        public void Clear()
        {
            Array.Clear(_cover, 0, _cover.Length);
            Sources = 0;
        }

        private void Raise(int index, float value)
        {
            if (value > _cover[index]) _cover[index] = value;
        }

        /// <summary>A weapon whose tip is in <paramref name="zone"/>: covers it fully and its neighbours partially.</summary>
        public void AddWeapon(Zone zone, OpeningConfig cfg) => Add(zone, cfg.NeighbourCover);

        /// <summary>A shield in <paramref name="zone"/>: big, covers its cell and every neighbour.</summary>
        public void AddShield(Zone zone, OpeningConfig cfg) => Add(zone, cfg.ShieldCover);

        private void Add(Zone zone, float neighbour)
        {
            Sources++;
            for (int h = 0; h < 3; h++)
                for (int s = 0; s < 3; s++)
                {
                    int d = Zone.Distance(zone, new Zone((ZoneHeight)h, (ZoneSide)s));
                    Raise(h * 3 + s, d == 0 ? 1f : (d == 1 ? neighbour : 0f));
                }
        }

        public float Coverage(Zone z) => _cover[(int)z.Height * 3 + (int)z.Side];

        public float Openness(Zone z) => 1f - Coverage(z);

        /// <summary>Openness of the most open cell.</summary>
        public float BestOpenness()
        {
            float best = 0f;
            for (int i = 0; i < 9; i++) best = Math.Max(best, 1f - _cover[i]);
            return best;
        }

        public Zone MostOpen()
        {
            int best = 0;
            for (int i = 1; i < 9; i++)
                if (_cover[i] < _cover[best]) best = i;
            return new Zone((ZoneHeight)(best / 3), (ZoneSide)(best % 3));
        }

        /// <summary>Top row first, defender's left to right; '#' covered, '+' partly, '.' open.</summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int h = 2; h >= 0; h--)
            {
                for (int s = 0; s < 3; s++)
                {
                    float c = _cover[h * 3 + s];
                    sb.Append(c >= 0.75f ? '#' : (c >= 0.3f ? '+' : '.'));
                }
                if (h > 0) sb.Append('/');
            }
            return sb.ToString();
        }
    }

    public static class OpeningPlanner
    {
        /// <summary>
        /// Weight multiplier (>= 1) for an attack that ends in <paramref name="end"/>: the more open that cell, the more
        /// the creature wants it. 1 when nothing is known about the opponent's guard.
        /// </summary>
        public static float WeightFactor(Zone end, GuardMap map, SkillProfile p, OpeningConfig cfg)
        {
            if (map == null || map.Sources == 0 || p.OpeningSeeking <= 0f) return 1f;
            return 1f + cfg.Gain * p.OpeningSeeking * map.Openness(end);
        }

        public static float WeightFactor(AttackCandidate c, GuardMap map, SkillProfile p, OpeningConfig cfg)
        {
            if (c.Direction == CutDirection.Unknown) return 1f;
            return WeightFactor(Zones.EndZone(c.Direction), map, p, cfg);
        }

        /// <summary>True when the guard leaves no real gap: the situation for attacking one side to open the other.</summary>
        public static bool IsTight(GuardMap map, OpeningConfig cfg) =>
            map != null && map.Sources > 0 && map.BestOpenness() < cfg.TightGuardThreshold;

        /// <summary>Extra probability of starting a sequence against a tight guard.</summary>
        public static float ComboBonus(GuardMap map, SkillProfile p, OpeningConfig cfg) =>
            IsTight(map, cfg) ? p.TightGuardComboBoost : 0f;
    }
}
