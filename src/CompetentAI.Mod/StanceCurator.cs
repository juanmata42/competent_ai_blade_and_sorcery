using System;
using System.Collections.Generic;
using System.Text;
using CompetentAI.Core;
using ThunderRoad;

namespace CompetentAI.Mod
{
    /// <summary>Everything the mod knows about one attack motion of one creature's stance clone.</summary>
    internal sealed class MotionInfo
    {
        public AttackMotion Motion;
        public AttackCandidate Candidate;
        public float BaseWeight;
        public string DirectionSource = "";
        public bool Synthesized;
        public bool Readmitted;
    }

    internal sealed class CurationReport
    {
        public int Removed;
        public int Readmitted;
        public int Synthesized;
        public readonly List<string> Notes = new List<string>();

        public override string ToString() =>
            $"removed={Removed} readmitted={Readmitted} synthesized={Synthesized}" + (Notes.Count > 0 ? " [" + string.Join("; ", Notes) + "]" : "");
    }

    /// <summary>
    /// Edits a creature's own (already weapon-filtered) clone of a <see cref="MeleeStanceData"/>:
    /// removes attacks the weapon cannot legitimately perform, gives back attacks the stock filter wrongly dropped,
    /// and adds a thrust riposte for thrust-only weapons. Never touches the catalog's source data.
    /// </summary>
    internal static class StanceCurator
    {
        public static AttackKind MapKind(ItemModuleAI.AttackType t)
        {
            switch (t)
            {
                case ItemModuleAI.AttackType.Swing: return AttackKind.Swing;
                case ItemModuleAI.AttackType.Thrust: return AttackKind.Thrust;
                default: return AttackKind.Unknown;
            }
        }

        public static Hand MapHand(Interactable.HandSide s)
        {
            switch (s)
            {
                case Interactable.HandSide.Left: return Hand.Left;
                case Interactable.HandSide.Both: return Hand.Both;
                default: return Hand.Right;
            }
        }

        public static MotionInfo MakeInfo(AttackMotion m, AttackRules rules, DirectionResolver dirs)
        {
            AttackKind declared = MapKind(m.attackMotion);
            AttackKind kind = rules.ResolveKind(m.id, m.address, declared);
            var c = new AttackCandidate
            {
                Id = m.id,
                Address = m.address,
                DeclaredKind = declared,
                Kind = kind,
                Side = MapHand(m.attackSide),
                MinRange = m.minRange,
                MaxRange = m.maxRange,
                Weight = m.weight,
                SourceIdle = m.sourceIdle,
                Tag = m
            };
            c.IsLunge = kind != AttackKind.Bash && rules.LooksLikeLunge(m.id, m.minRange);
            c.Direction = dirs.Resolve(m.id, m.address, kind, out string src);
            return new MotionInfo { Motion = m, Candidate = c, BaseWeight = m.weight, DirectionSource = src };
        }

        /// <summary>Re-resolves the direction (e.g. after new measurements arrived).</summary>
        public static void RefreshDirection(MotionInfo mi, DirectionResolver dirs)
        {
            mi.Candidate.Direction = dirs.Resolve(mi.Candidate.Id, mi.Candidate.Address, mi.Candidate.Kind, out string src);
            mi.DirectionSource = src;
        }

        public static Dictionary<AttackMotion, MotionInfo> Curate(MeleeStanceData data, WeaponProfile right, WeaponProfile left,
            ModConfig cfg, AttackRules rules, DirectionResolver dirs, int difficulty, bool applyRules, out CurationReport report)
        {
            report = new CurationReport();
            var infos = new Dictionary<AttackMotion, MotionInfo>();
            var lists = new[] { data.openingAttacks, data.chainAttacks, data.ripostes };
            string[] names = { "openers", "chains", "ripostes" };

            for (int li = 0; li < lists.Length; li++)
            {
                List<AttackMotion> list = lists[li];
                var keep = new List<AttackMotion>(list.Count);
                var all = new List<MotionInfo>(list.Count);
                var removedNotes = new List<string>();
                foreach (AttackMotion m in list)
                {
                    MotionInfo mi = MakeInfo(m, rules, dirs);
                    all.Add(mi);
                    RuleVerdict v = applyRules ? rules.Evaluate(right, left, mi.Candidate) : new RuleVerdict();
                    if (v.Allowed)
                    {
                        mi.BaseWeight = m.weight * v.Bias;
                        keep.Add(m);
                    }
                    else
                    {
                        removedNotes.Add(names[li] + ":-" + m.id + " (" + v.Reason + ")");
                    }
                }
                if (keep.Count == 0 && list.Count > 0)
                {
                    // Never leave a creature without attacks: keep the stock list untouched.
                    report.Notes.Add(names[li] + ": every attack was filtered, keeping the stock list");
                    foreach (MotionInfo mi in all)
                    {
                        mi.BaseWeight = mi.Motion.weight;
                        keep.Add(mi.Motion);
                    }
                }
                else
                {
                    report.Removed += removedNotes.Count;
                    report.Notes.AddRange(removedNotes);
                }
                var keepSet = new HashSet<AttackMotion>(keep);
                foreach (MotionInfo mi in all)
                {
                    if (!keepSet.Contains(mi.Motion)) continue;
                    infos[mi.Motion] = mi;
                    mi.Motion.weight = mi.BaseWeight;
                }
                list.Clear();
                list.AddRange(keep);
            }

            if (applyRules && cfg.Rules.ReadmitReclassified) Readmit(data, right, left, rules, dirs, difficulty, infos, report);
            if (applyRules) SynthesizeThrustRiposte(data, rules, dirs, infos, report);

            data.UpdateAllNodes(false);
            data.PopulateAllNodeLists();
            return infos;
        }

        private static bool ItemAllowsKind(WeaponProfile w, AttackKind k)
        {
            if (w == null) return true;
            if (k == AttackKind.Swing) return (w.DataKinds & AttackKinds.Swing) != 0;
            if (k == AttackKind.Thrust) return (w.DataKinds & AttackKinds.Thrust) != 0;
            return true;
        }

        /// <summary>
        /// The stock filter compares the DECLARED attack type with the weapon's flags. Some attacks are declared
        /// wrongly (e.g. CounterThrust is declared as a swing), which leaves thrust-only weapons with 2 attacks.
        /// Give those back when the corrected kind is legitimate for the weapon.
        /// </summary>
        private static void Readmit(MeleeStanceData data, WeaponProfile right, WeaponProfile left, AttackRules rules,
            DirectionResolver dirs, int difficulty, Dictionary<AttackMotion, MotionInfo> infos, CurationReport report)
        {
            MeleeStanceData source = Catalog.GetData<StanceData>(data.id) as MeleeStanceData;
            if (source == null) return;

            var pairs = new[]
            {
                new KeyValuePair<List<AttackMotion>, List<AttackMotion>>(source.openingAttacks, data.openingAttacks),
                new KeyValuePair<List<AttackMotion>, List<AttackMotion>>(source.chainAttacks, data.chainAttacks),
                new KeyValuePair<List<AttackMotion>, List<AttackMotion>>(source.ripostes, data.ripostes)
            };
            foreach (var pair in pairs)
            {
                foreach (AttackMotion src in pair.Key)
                {
                    if (src == null || src.attackMotion == ItemModuleAI.AttackType.None) continue;
                    if (difficulty > 0 && src.difficulty > difficulty) continue;
                    bool present = false;
                    foreach (AttackMotion have in pair.Value)
                        if (have.id == src.id) { present = true; break; }
                    if (present) continue;

                    MotionInfo mi = MakeInfo(src, rules, dirs);
                    if (mi.Candidate.Kind == mi.Candidate.DeclaredKind || mi.Candidate.Kind == AttackKind.Unknown) continue;
                    WeaponProfile w = mi.Candidate.Side == Hand.Left ? left : right;
                    if (!ItemAllowsKind(w, mi.Candidate.Kind) || (mi.Candidate.Side == Hand.Both && !ItemAllowsKind(left, mi.Candidate.Kind))) continue;
                    RuleVerdict v = rules.Evaluate(right, left, mi.Candidate);
                    if (!v.Allowed) continue;

                    AttackMotion copy = src.Copy<AttackMotion>();
                    mi.Motion = copy;
                    mi.Candidate.Tag = copy;
                    mi.BaseWeight = src.weight * v.Bias;
                    mi.Readmitted = true;
                    copy.weight = mi.BaseWeight;
                    pair.Value.Add(copy);
                    infos[copy] = mi;
                    report.Readmitted++;
                    report.Notes.Add("+" + src.id + " (declared " + mi.Candidate.DeclaredKind + ", really " + mi.Candidate.Kind + ")");
                }
            }
        }

        /// <summary>Thrust-only weapons have no ripostes at all in the stock data (all of them are swings): add one.</summary>
        private static void SynthesizeThrustRiposte(MeleeStanceData data, AttackRules rules, DirectionResolver dirs,
            Dictionary<AttackMotion, MotionInfo> infos, CurationReport report)
        {
            if (data.ripostes.Count > 0) return;
            AttackMotion best = null;
            foreach (AttackMotion m in data.openingAttacks)
            {
                if (!infos.TryGetValue(m, out MotionInfo mi)) continue;
                if (mi.Candidate.Kind != AttackKind.Thrust || mi.Candidate.IsLunge || m.animationClip == null) continue;
                if (best == null || m.minRange < best.minRange) best = m;
            }
            if (best == null) return;

            AttackMotion r = best.Copy<AttackMotion>();
            r.id = "CompetentRiposteThrust";
            r.sourceIdle = "Any";
            r.endIdle = "Previous";
            r.comboChainAttack = "None";
            r.minRange = 0f;
            r.maxRange = 0f;
            r.weight = 1f;
            r.difficulty = 0;
            data.ripostes.Add(r);
            foreach (GuardPose g in data.guardPoseIDs.Values)
                if (g.riposteOptions != null && !g.riposteOptions.Contains(r.id)) g.riposteOptions.Add(r.id);

            MotionInfo info = MakeInfo(r, rules, dirs);
            info.Synthesized = true;
            info.BaseWeight = 1f;
            infos[r] = info;
            report.Synthesized++;
            report.Notes.Add("+riposte " + r.id + " (from " + best.id + ")");
        }

        public static string Describe(MeleeStanceData data, Dictionary<AttackMotion, MotionInfo> infos)
        {
            var sb = new StringBuilder();
            sb.Append("stance ").Append(data.id).Append(": ");
            Append(sb, "openers", data.openingAttacks, infos);
            Append(sb, "chains", data.chainAttacks, infos);
            Append(sb, "ripostes", data.ripostes, infos);
            return sb.ToString();
        }

        private static void Append(StringBuilder sb, string label, List<AttackMotion> list, Dictionary<AttackMotion, MotionInfo> infos)
        {
            sb.Append(label).Append('(').Append(list.Count).Append(")=[");
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                AttackMotion m = list[i];
                sb.Append(m.id);
                if (infos != null && infos.TryGetValue(m, out MotionInfo mi))
                    sb.Append(':').Append(mi.Candidate.Kind).Append('/').Append(mi.Candidate.Direction).Append('/').Append(mi.DirectionSource);
            }
            sb.Append("] ");
        }
    }
}
