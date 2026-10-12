using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CompetentAI.Core
{
    /// <summary>
    /// Learns what direction each attack animation really travels, from measurements taken at runtime
    /// (weapon tip path in the attacker's local frame). Persisted as a small JSON file.
    /// </summary>
    public sealed class DirectionCalibration
    {
        [JsonProperty]
        private Dictionary<string, Dictionary<string, int>> _votes =
            new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty]
        private Dictionary<string, int[]> _advance = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);

        [JsonIgnore]
        public int Keys => _votes.Count;

        /// <summary>Records whether an attack animation moved the body forward (root motion) when it was observed.</summary>
        public void AddAdvance(string key, bool advanced)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (!_advance.TryGetValue(key, out int[] c)) _advance[key] = c = new int[2];
            c[advanced ? 1 : 0]++;
        }

        public bool TryGetAdvance(string key, int minSamples, float agreement, out bool advances)
        {
            advances = false;
            if (string.IsNullOrEmpty(key) || !_advance.TryGetValue(key, out int[] c)) return false;
            int total = c[0] + c[1];
            if (total < minSamples) return false;
            int best = Math.Max(c[0], c[1]);
            if ((float)best / total < agreement) return false;
            advances = c[1] > c[0];
            return true;
        }

        public void Add(string key, CutDirection d)
        {
            if (string.IsNullOrEmpty(key) || d == CutDirection.Unknown) return;
            if (!_votes.TryGetValue(key, out var bucket))
            {
                bucket = new Dictionary<string, int>();
                _votes[key] = bucket;
            }
            string name = d.ToString();
            bucket.TryGetValue(name, out int n);
            bucket[name] = n + 1;
        }

        public bool TryGet(string key, int minSamples, float agreement, out CutDirection direction, out int samples)
        {
            direction = CutDirection.Unknown;
            samples = 0;
            if (string.IsNullOrEmpty(key) || !_votes.TryGetValue(key, out var bucket)) return false;
            int total = 0, best = 0;
            string bestName = null;
            foreach (var kv in bucket)
            {
                total += kv.Value;
                if (kv.Value > best)
                {
                    best = kv.Value;
                    bestName = kv.Key;
                }
            }
            samples = total;
            if (total < minSamples || bestName == null) return false;
            if ((float)best / total < agreement) return false;
            return Enum.TryParse(bestName, out direction);
        }

        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static DirectionCalibration FromJson(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return new DirectionCalibration();
                return JsonConvert.DeserializeObject<DirectionCalibration>(json) ?? new DirectionCalibration();
            }
            catch
            {
                return new DirectionCalibration();
            }
        }
    }

    /// <summary>
    /// Resolves the direction of an attack by (in order): explicit config override, runtime measurement,
    /// unambiguous name hints, otherwise Unknown. Name hints are INFERRED from the stock ids and were not
    /// verified against the animation clips; measurements supersede them.
    /// </summary>
    public sealed class DirectionResolver
    {
        private readonly RulesConfig _rules;
        private readonly CalibrationConfig _cal;
        private readonly DirectionCalibration _calibration;

        public DirectionResolver(RulesConfig rules, CalibrationConfig cal, DirectionCalibration calibration)
        {
            _rules = rules;
            _cal = cal;
            _calibration = calibration;
        }

        public CutDirection Resolve(string id, string address, AttackKind kind, out string source)
        {
            if (TryOverride(id, out CutDirection d) || TryOverride(address, out d))
            {
                source = "config";
                return d;
            }
            if (_calibration != null &&
                (_calibration.TryGet(address, _cal.MinSamples, _cal.Agreement, out d, out _) ||
                 _calibration.TryGet(id, _cal.MinSamples, _cal.Agreement, out d, out _)))
            {
                source = "measured";
                return d;
            }
            d = FromName(id, address, kind);
            source = d == CutDirection.Unknown ? "unknown" : "name";
            return d;
        }

        private bool TryOverride(string key, out CutDirection d)
        {
            d = CutDirection.Unknown;
            if (string.IsNullOrEmpty(key) || _rules.DirectionOverrides == null) return false;
            return _rules.DirectionOverrides.TryGetValue(key, out string name) && Enum.TryParse(name, true, out d) &&
                   d != CutDirection.Unknown;
        }

        /// <summary>Only unambiguous hints. "RtL" = travels from the right towards the left, and so on.</summary>
        public static CutDirection FromName(string id, string address, AttackKind kind)
        {
            string t = (id ?? "") + "|" + (address ?? "");
            string l = t.ToLowerInvariant();
            if (kind == AttackKind.Thrust || l.Contains("thrust") || l.Contains("stab")) return CutDirection.Thrust;
            if (kind == AttackKind.Bash) return CutDirection.Unknown;
            if (t.Contains("RtL") || l.Contains("righttoleft")) return CutDirection.Left;
            if (t.Contains("LtR") || l.Contains("lefttoright")) return CutDirection.Right;
            if (t.Contains("TtL") || l.Contains("toptoleft")) return CutDirection.DownLeft;
            if (t.Contains("TtR") || l.Contains("toptoright")) return CutDirection.DownRight;
            return CutDirection.Unknown;
        }
    }
}
