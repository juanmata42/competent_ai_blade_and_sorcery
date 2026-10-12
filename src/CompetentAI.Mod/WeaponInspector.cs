using System;
using System.Collections.Generic;
using CompetentAI.Core;
using ThunderRoad;

namespace CompetentAI.Mod
{
    /// <summary>Builds an engine-independent <see cref="WeaponProfile"/> from a held <see cref="Item"/>.</summary>
    internal static class WeaponInspector
    {
        private static readonly Dictionary<string, WeaponProfile> Cache = new Dictionary<string, WeaponProfile>(StringComparer.OrdinalIgnoreCase);

        public static void ClearCache() => Cache.Clear();

        public static WeaponProfile Build(Item item, RulesConfig rules)
        {
            if (item == null) return null;
            string id = item.data != null ? item.data.id : item.name;
            if (id != null && Cache.TryGetValue(id, out WeaponProfile cached)) return cached;

            WeaponProfile p = new WeaponProfile { ItemId = id ?? "?" };
            try
            {
                ItemModuleAI ai = item.data != null ? item.data.moduleAI : null;
                if (ai != null)
                {
                    p.DataKinds = (AttackKinds)(int)ai.weaponAttackTypes;
                    p.TwoHanded = ai.weaponHandling == ItemModuleAI.WeaponHandling.TwoHanded;
                }

                DamageTraits traits = DamageTraits.None;
                if (item.collisionHandlers != null)
                {
                    foreach (CollisionHandler handler in item.collisionHandlers)
                    {
                        if (handler == null || handler.damagers == null) continue;
                        foreach (Damager damager in handler.damagers)
                        {
                            if (damager == null) continue;
                            string did = damager.data != null ? damager.data.id : damager.damagerID;
                            if (WeaponClassifier.TraitFromDamagerId(did) == DamageTraits.None &&
                                (did == null || did.StartsWith("Handle", StringComparison.OrdinalIgnoreCase)))
                                continue; // handles are blunt but incidental
                            switch (damager.type)
                            {
                                case Damager.Type.Slash: traits |= DamageTraits.Edge; break;
                                case Damager.Type.Pierce: traits |= DamageTraits.Point; break;
                                case Damager.Type.Blunt:
                                    if (did == null || !did.StartsWith("Handle", StringComparison.OrdinalIgnoreCase))
                                        traits |= DamageTraits.Blunt;
                                    break;
                            }
                        }
                    }
                }

                string over;
                if (rules != null && rules.TraitOverrides != null && id != null && rules.TraitOverrides.TryGetValue(id, out over))
                    traits = ParseTraits(over);

                p.Traits = traits;
                p.InfoKnown = traits != DamageTraits.None;
                bool isShield = ai != null && (ai.primaryClass == ItemModuleAI.WeaponClass.Shield ||
                                               ai.secondaryClass == ItemModuleAI.WeaponClass.Shield);
                p.Class = WeaponClassifier.Classify(id, traits, p.TwoHanded, isShield);

                if (rules != null && rules.ClassOverrides != null && id != null &&
                    rules.ClassOverrides.TryGetValue(id, out string cls) && Enum.TryParse(cls, true, out WeaponClass wc))
                    p.Class = wc;
            }
            catch (Exception ex)
            {
                // Never let an odd item break the AI: fall back to "unknown" = stock behaviour.
                Log.Warn("could not inspect weapon " + p.ItemId + ": " + ex.Message);
                p.InfoKnown = false;
            }

            if (id != null) Cache[id] = p;
            Log.Debug_("weapon " + p);
            return p;
        }

        private static DamageTraits ParseTraits(string text)
        {
            DamageTraits t = DamageTraits.None;
            if (string.IsNullOrEmpty(text)) return t;
            foreach (string part in text.Split(','))
                if (Enum.TryParse(part.Trim(), true, out DamageTraits one)) t |= one;
            return t;
        }
    }
}
