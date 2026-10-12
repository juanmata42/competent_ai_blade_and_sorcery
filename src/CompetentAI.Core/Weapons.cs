using System;
using System.Collections.Generic;

namespace CompetentAI.Core
{
    /// <summary>What the weapon can actually do to a target, derived from its damagers (not from item-data flags).</summary>
    [Flags]
    public enum DamageTraits
    {
        None = 0,
        Edge = 1,   // has a slash damager (a real cutting edge)
        Point = 2,  // has a pierce damager (a usable point)
        Blunt = 4   // has a blunt head (not just the handle)
    }

    /// <summary>The game's own coarse attack-type flags from <c>ItemModuleAI.weaponAttackTypes</c>.</summary>
    [Flags]
    public enum AttackKinds
    {
        None = 0,
        Swing = 1,
        Thrust = 2
    }

    public enum WeaponClass
    {
        Unknown,
        Sword,
        Dagger,
        Rapier,
        Axe,
        Mace,
        Spear,
        Polearm,
        Staff,
        Shield,
        Tool
    }

    /// <summary>Engine-independent description of a held weapon.</summary>
    public sealed class WeaponProfile
    {
        public string ItemId = "";
        public DamageTraits Traits = DamageTraits.None;
        public AttackKinds DataKinds = AttackKinds.Swing | AttackKinds.Thrust;
        public bool TwoHanded;
        public WeaponClass Class = WeaponClass.Unknown;

        /// <summary>
        /// False when the damager information could not be read (e.g. a modded item). Rules then stay permissive.
        /// </summary>
        public bool InfoKnown;

        public bool IsShield => Class == WeaponClass.Shield;
        public bool HasEdge => (Traits & DamageTraits.Edge) != 0;
        public bool HasPoint => (Traits & DamageTraits.Point) != 0;
        public bool HasBlunt => (Traits & DamageTraits.Blunt) != 0;

        public override string ToString() =>
            $"{ItemId}[{Class},{Traits},data={DataKinds},{(TwoHanded ? "2H" : "1H")},{(InfoKnown ? "known" : "UNKNOWN")}]";
    }

    public static class WeaponClassifier
    {
        /// <summary>
        /// Maps a damager data id (e.g. "SwordSlash2H", "EdgelessDaggerPierce", "Mace1H", "HandleHeavy")
        /// to the trait it provides. Handles, fists and ragdoll damagers provide nothing.
        /// </summary>
        public static DamageTraits TraitFromDamagerId(string damagerId)
        {
            if (string.IsNullOrEmpty(damagerId)) return DamageTraits.None;
            string d = damagerId.ToLowerInvariant();
            if (d.StartsWith("handle") || d.Contains("punch") || d.Contains("kick") || d.Contains("karate") ||
                d.Contains("ragdoll") || d.Contains("fireball") || d.Contains("lightning") || d.Contains("arrow"))
                return DamageTraits.None;
            if (d.Contains("slash")) return DamageTraits.Edge;
            if (d.Contains("pierce") || d.Contains("spike")) return DamageTraits.Point;
            if (d.Contains("mace") || d.Contains("blunt") || d.Contains("shield")) return DamageTraits.Blunt;
            return DamageTraits.None;
        }

        public static DamageTraits TraitsFromDamagerIds(IEnumerable<string> ids)
        {
            DamageTraits t = DamageTraits.None;
            if (ids == null) return t;
            foreach (string id in ids) t |= TraitFromDamagerId(id);
            return t;
        }

        /// <summary>Best-effort class from item id keywords, falling back to traits. Never throws.</summary>
        public static WeaponClass Classify(string itemId, DamageTraits traits, bool twoHanded, bool isShield)
        {
            if (isShield) return WeaponClass.Shield;
            string id = (itemId ?? "").ToLowerInvariant();

            if (id.Contains("shield") || id.Contains("buckler")) return WeaponClass.Shield;
            if (id.Contains("rapier") || id.Contains("duelingsword")) return WeaponClass.Rapier;
            if (id.Contains("dagger") || id.Contains("stiletto") || id.Contains("knife") || id.EndsWith("sai") ||
                id.Contains("wristblade")) return WeaponClass.Dagger;
            if (id.Contains("staff")) return WeaponClass.Staff;
            if (id.Contains("halberd") || id.Contains("bardiche") || id.Contains("glaive") || id.Contains("poleaxe") ||
                id.Contains("guandao")) return WeaponClass.Polearm;
            if (id.Contains("spear") || id.Contains("javelin") || id.Contains("harpoon") || id.Contains("pike") ||
                id.Contains("fork")) return WeaponClass.Spear;
            if (id.Contains("mace") || id.Contains("hammer") || id.Contains("maul") || id.Contains("club") ||
                id.Contains("mallet") || id.Contains("kanabo")) return WeaponClass.Mace;
            if (id.Contains("axe") || id.Contains("hatchet") || id.Contains("cleaver") || id.Contains("kama") ||
                id.Contains("warpick")) return WeaponClass.Axe;
            if (id.Contains("sword") || id.Contains("saber") || id.Contains("scimitar") || id.Contains("katana") ||
                id.Contains("khopesh") || id.Contains("falchion") || id.Contains("cutlass") || id.Contains("seax") ||
                id.Contains("zhanmadao")) return WeaponClass.Sword;
            if (id.StartsWith("tool") || id.Contains("pickaxe") || id.Contains("shovel") || id.Contains("hoe") ||
                id.Contains("rake") || id.Contains("sickle") || id.Contains("oar")) return WeaponClass.Tool;

            // Fall back to what the damagers say.
            bool edge = (traits & DamageTraits.Edge) != 0;
            bool point = (traits & DamageTraits.Point) != 0;
            bool blunt = (traits & DamageTraits.Blunt) != 0;
            if (edge && point) return WeaponClass.Sword;
            if (point && !edge && !blunt) return twoHanded ? WeaponClass.Spear : WeaponClass.Dagger;
            if (blunt && !edge && !point) return WeaponClass.Mace;
            if (edge && !point) return WeaponClass.Axe;
            return WeaponClass.Unknown;
        }
    }
}
