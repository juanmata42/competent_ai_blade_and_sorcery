using ThunderRoad;

namespace CompetentAI.Mod
{
    /// <summary>
    /// In-game options menu (Options > Mods > "Competent AI"). Changes act LIVE on NPCs already in the scene
    /// (except "Weapon rules", which applies the next time an NPC draws a weapon or spawns).
    /// These can only turn things OFF relative to CompetentAI.config.json (effective = JSON AND menu).
    /// </summary>
    public static class ModOptions
    {
        private const string Cat = "Competent AI";

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(1)]
        [ModOption("Competent AI", "ON: NPCs use the mod's combat AI. OFF: the game's stock AI, exactly as shipped (live).")]
        public static bool Enabled = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(2)]
        [ModOptionValues(nameof(SkillLevelValues))]
        [ModOption("Skill level", "Auto = each enemy type keeps its own level (easy brains Recruit ... hard brains Veteran). Or force one level for every NPC. Applies live.")]
        public static int SkillLevel = 0;

        public static ModOptionParameter[] SkillLevelValues()
        {
            return new ModOptionParameter[]
            {
                new ModOptionInt("Auto (by enemy type)", 0),
                new ModOptionInt("1 - Recruit", 1),
                new ModOptionInt("2 - Soldier", 2),
                new ModOptionInt("3 - Veteran", 3),
                new ModOptionInt("4 - Duelist", 4)
            };
        }

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(3)]
        [ModOptionIntValues(-3, 3, 1)]
        [ModOption("Skill shift (Auto only)", "With Skill level = Auto: move every NPC this many steps along Recruit > Soldier > Veteran > Duelist. Applies live.")]
        public static int DifficultyShift = 0;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(4)]
        [ModOption("Chained strikes (katas)", "NPCs chain several strikes, assuming the first will be parried, and break off if you become dangerous.")]
        public static bool Combos = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(5)]
        [ModOption("Feints", "NPCs abort a wind-up and redirect the attack.")]
        public static bool Feints = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(6)]
        [ModOption("Falling for feints / fatigue", "NPCs can fall for (or read) your feints, and get overwhelmed by sustained parrying.")]
        public static bool DefenseReading = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(7)]
        [ModOption("Counter after dodge", "NPCs counter-attack right after a successful dodge.")]
        public static bool CounterAfterDodge = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(8)]
        [ModOption("Attack variety", "Attack memory and stance steering so NPCs stop repeating the same two attacks.")]
        public static bool Variety = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(9)]
        [ModOption("Weapon rules", "No cuts with edgeless weapons, no thrusts with blunt ones. Applies when an NPC next draws a weapon or spawns.")]
        public static bool WeaponRules = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(10)]
        [ModOption("Skill tuning", "Reaction speed, riposte chance and attack tempo per skill level.")]
        public static bool ProfileTuning = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(11)]
        [ModOption("Active weapon parry (experimental)", "Veteran/Duelist place their weapon in your strike's path (and also deflect spells). Turn off if arms look odd.")]
        public static bool OneHandIK = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(12)]
        [ModOption("Sidestep dodge (experimental)", "Duelists may sidestep. Needs the game's side-dodge animations.")]
        public static bool SideDodge = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(13)]
        [ModOption("Measure attack directions", "Learns the real direction of every attack animation (written to calibration.json).")]
        public static bool Calibration = true;

        [ModOptionCategory(Cat, 1)]
        [ModOptionOrder(14)]
        [ModOptionIntValues(0, 5, 1)]
        [ModOption("Log level", "0 Off, 1 Error, 2 Warn, 3 Info, 4 Debug, 5 Trace. Use 4 when reporting problems.")]
        public static int LogLevelOption = 3;
    }
}
