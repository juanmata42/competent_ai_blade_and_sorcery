using CompetentAI.Core;
using ThunderRoad;

namespace CompetentAI.Mod
{
    /// <summary>
    /// In-game options menu (Mods tab). These can only turn things OFF relative to CompetentAI.config.json
    /// (effective = JSON AND menu), so a module disabled in the JSON stays disabled.
    /// The game applies saved values at load and when the player changes them.
    /// </summary>
    public static class ModOptions
    {
        private const string Cat = "Competent AI";

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(1)]
        [ModOption("Enabled", "Master switch. Off = the mod leaves NPC combat exactly as the game ships it.")]
        public static bool Enabled = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(2)]
        [ModOptionIntValues(-3, 3, 1)]
        [ModOption("Difficulty shift", "Moves every NPC along Recruit > Soldier > Veteran > Duelist. 0 = as configured.")]
        public static int DifficultyShift = 0;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(3)]
        [ModOption("Profile tuning", "Archetype tuning of reaction speed, riposte chance, attack tempo.")]
        public static bool ProfileTuning = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(4)]
        [ModOption("Weapon rules", "Filter attacks by what the weapon can really do (no cuts with edgeless weapons).")]
        public static bool WeaponRules = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(5)]
        [ModOption("Attack variety", "Attack memory and stance steering so NPCs stop repeating the same two attacks.")]
        public static bool Variety = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(6)]
        [ModOption("Feints", "NPCs abort a wind-up and redirect the attack.")]
        public static bool Feints = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(7)]
        [ModOption("Combos", "NPCs chain a second attack after the first.")]
        public static bool Combos = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(8)]
        [ModOption("Defence reading", "NPCs can fall for (or read) your feints, and get overwhelmed by sustained parrying.")]
        public static bool DefenseReading = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(9)]
        [ModOption("Counter after dodge", "NPCs counter-attack right after a successful dodge.")]
        public static bool CounterAfterDodge = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(10)]
        [ModOption("Active weapon parry (IK)", "Veteran/Duelist place their weapon in your strike's path. Experimental: turn off if arms look odd.")]
        public static bool OneHandIK = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(11)]
        [ModOption("Sidestep dodge", "Duelists may sidestep. Experimental: needs the game's side-dodge animations.")]
        public static bool SideDodge = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(12)]
        [ModOption("Measure attack directions", "Learns the real direction of every attack animation (written to calibration.json).")]
        public static bool Calibration = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(13)]
        [ModOptionIntValues(0, 5, 1)]
        [ModOption("Log level", "0 Off, 1 Error, 2 Warn, 3 Info, 4 Debug, 5 Trace. Use 4 when reporting problems.")]
        public static int LogLevelOption = 3;
    }
}
