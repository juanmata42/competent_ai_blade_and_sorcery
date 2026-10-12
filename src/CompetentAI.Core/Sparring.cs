using System.Collections.Generic;
using Newtonsoft.Json;

namespace CompetentAI.Core
{
    public sealed class SparringLoadout
    {
        /// <summary>Label used in the log.</summary>
        public string Name = "";

        /// <summary>Container (catalog) id with the apparel and weapons; shipped in the mod's Containers folder.</summary>
        public string ContainerId = "";

        public SparringLoadout()
        {
        }

        public SparringLoadout(string name, string containerId)
        {
            Name = name;
            ContainerId = containerId;
        }
    }

    /// <summary>
    /// Practice NPCs summoned by the two "Sparring" spells (spell wheel): one NPC per weapon type that attacks you,
    /// and one NPC that only parries and never attacks.
    /// </summary>
    public sealed class SparringConfig
    {
        /// <summary>Master switch of the spells (false = casting does nothing).</summary>
        public bool Enabled = true;

        /// <summary>Creature data cloned for every practice NPC.</summary>
        public string CreatureId = "HumanMale";

        /// <summary>Brain of the attackers (the skill level of the mod still follows the in-game menu).</summary>
        public string AttackerBrainId = "HumanMedium";

        /// <summary>Brain of the parry-only dummy.</summary>
        public string DummyBrainId = "HumanHard";

        /// <summary>Faction of the practice NPCs. 3 = MixedEnemies, hostile to the player.</summary>
        public int FactionId = 3;

        /// <summary>Health of each attacker, so they survive a few of your hits.</summary>
        public int AttackerHealth = 1000;

        /// <summary>Health of the dummy (it is meant to be hit for a long time).</summary>
        public int DummyHealth = 100000;

        /// <summary>The attackers stand on a ring of this radius (metres) around the aimed point.</summary>
        public float RingRadius = 4f;

        /// <summary>The dummy appears this far (metres) from the aimed point towards you.</summary>
        public float DummyDistance = 0.5f;

        /// <summary>
        /// "Demo": each weapon NPC stands still facing you and performs EVERY attack of its stance one after another,
        /// so you can watch each animation and practise parrying it. "Fight": they fight you like normal enemies.
        /// </summary>
        public string AttackerMode = "Demo";

        /// <summary>Demo: seconds between one demonstrated attack and the next.</summary>
        public float DemoPauseSeconds = 2.5f;

        /// <summary>Demo: only demonstrators closer than this (metres) to you perform; walk up to the one you want to see.</summary>
        public float DemoActiveRange = 5f;

        /// <summary>Demo: show the name, kind and direction of each attack on screen (the log always lists them).</summary>
        public bool DemoShowLabels = true;

        /// <summary>Demo: also show the chain (follow-up) attacks.</summary>
        public bool DemoIncludeChains = true;

        /// <summary>Demo: also show the riposte attacks.</summary>
        public bool DemoIncludeRipostes = true;

        /// <summary>Demo: how long to wait for the NPC to draw its weapon before warning in the log.</summary>
        public float DemoReadyTimeoutSeconds = 25f;

        /// <summary>Fight mode only: only one attacker attacks at a time.</summary>
        public bool OneAttackerAtATime = true;

        /// <summary>Container of the parry-only dummy.</summary>
        public string DummyContainerId = "CompetentSpar_Dummy";

        /// <summary>One attacker per entry. Replace the list in the JSON to choose your own.</summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<SparringLoadout> Loadouts = DefaultLoadouts();

        public static List<SparringLoadout> DefaultLoadouts()
        {
            return new List<SparringLoadout>
            {
                new SparringLoadout("Sword", "CompetentSpar_Sword"),
                new SparringLoadout("Dagger", "CompetentSpar_Dagger"),
                new SparringLoadout("Stiletto (no edge)", "CompetentSpar_Stiletto"),
                new SparringLoadout("Rapier", "CompetentSpar_Rapier"),
                new SparringLoadout("Spear", "CompetentSpar_Spear"),
                new SparringLoadout("Halberd", "CompetentSpar_Halberd"),
                new SparringLoadout("Hand axe", "CompetentSpar_Axe"),
                new SparringLoadout("Mace", "CompetentSpar_Mace"),
                new SparringLoadout("Maul (2H)", "CompetentSpar_Maul"),
                new SparringLoadout("Greatsword (2H)", "CompetentSpar_Greatsword"),
                new SparringLoadout("Staff blade", "CompetentSpar_Staff"),
                new SparringLoadout("Sword and shield", "CompetentSpar_SwordShield"),
                new SparringLoadout("Dual wield", "CompetentSpar_DualWield")
            };
        }

        public void Normalize()
        {
            if (string.IsNullOrEmpty(CreatureId)) CreatureId = "HumanMale";
            if (string.IsNullOrEmpty(AttackerBrainId)) AttackerBrainId = "HumanMedium";
            if (string.IsNullOrEmpty(DummyBrainId)) DummyBrainId = "HumanHard";
            if (string.IsNullOrEmpty(DummyContainerId)) DummyContainerId = "CompetentSpar_Dummy";
            AttackerMode = string.Equals(AttackerMode, "Fight", System.StringComparison.OrdinalIgnoreCase) ? "Fight" : "Demo";
            DemoPauseSeconds = System.Math.Max(0.5f, System.Math.Min(DemoPauseSeconds, 20f));
            DemoActiveRange = System.Math.Max(1.5f, System.Math.Min(DemoActiveRange, 30f));
            DemoReadyTimeoutSeconds = System.Math.Max(5f, System.Math.Min(DemoReadyTimeoutSeconds, 120f));
            if (Loadouts == null) Loadouts = DefaultLoadouts();
            Loadouts.RemoveAll(l => l == null || string.IsNullOrEmpty(l.ContainerId));
            if (Loadouts.Count > 24) Loadouts.RemoveRange(24, Loadouts.Count - 24);
            AttackerHealth = System.Math.Max(10, System.Math.Min(AttackerHealth, 1000000));
            DummyHealth = System.Math.Max(10, System.Math.Min(DummyHealth, 10000000));
            RingRadius = System.Math.Max(1.5f, System.Math.Min(RingRadius, 15f));
            DummyDistance = System.Math.Max(0f, System.Math.Min(DummyDistance, 6f));
        }
    }
}
