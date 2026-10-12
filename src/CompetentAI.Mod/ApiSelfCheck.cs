using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ThunderRoad;

namespace CompetentAI.Mod
{
    /// <summary>
    /// Logs, at startup, whether every game member the mod relies on still exists with the expected shape.
    /// The mod is compiled against 1.3.1.260730005; after a game update this tells at a glance what moved.
    /// </summary>
    internal static class ApiSelfCheck
    {
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static readonly KeyValuePair<Type, string[]>[] Required =
        {
            new KeyValuePair<Type, string[]>(typeof(BrainModuleDefense), new[]
            {
                "parryAdjustSpeed", "aboutToBeHitTiming", "dodgeChanceCurve", "sideDodgeEnabled", "oneHandUsesIK",
                "oneHandMaxIKWeightPos", "oneHandMaxIKWeightRot", "parryEnabled", "nextAllowedDodgeTime", "oneHandTransform",
                "oneHandAnchor", "TryToDodge", "EnableIK", "DisableIK", "canDodge"
            }),
            new KeyValuePair<Type, string[]>(typeof(BrainModuleMelee), new[]
            {
                "minMaxTimeBetweenAttack", "animationSpeedMultiplier", "recoilNothingRiposteChance", "minMaxRiposteDelay",
                "maxRangeDelta", "attack", "lastAttack", "attackCount", "attackWindow", "followingThrough", "StopAttack", "TriggerAttack",
                "AttackTarget", "GetWeaponReach", "GetMeleeRanges", "GetBrainSpeedMultiplier"
            }),
            new KeyValuePair<Type, string[]>(typeof(BrainModuleStance), new[] { "OnStanceChange", "currentStanceData", "currentPose", "ChangeIdlePose", "switching" }),
            new KeyValuePair<Type, string[]>(typeof(BrainModuleDetection), new[] { "attackDangerLevel", "attackInfoLeft", "attackInfoRight" }),
            new KeyValuePair<Type, string[]>(typeof(BrainModuleEquipment), new[] { "chosenRightHandItem", "chosenLeftHandItem" }),
            new KeyValuePair<Type, string[]>(typeof(MeleeStanceData), new[] { "openingAttacks", "chainAttacks", "ripostes", "guardPoseIDs", "alternateAttackSides" }),
            new KeyValuePair<Type, string[]>(typeof(AttackMotion), new[] { "sourceIdle", "endIdle", "comboChainAttack", "minRange", "maxRange", "sweep", "attackSide", "attackMotion", "GetChainAttack", "GetRealStartTime" }),
            new KeyValuePair<Type, string[]>(typeof(GuardPose), new[] { "riposteOptions" }),
            new KeyValuePair<Type, string[]>(typeof(EventManager), new[] { "onCreatureAttacking", "onCreatureAttackParry", "onLevelUnload" }),
            new KeyValuePair<Type, string[]>(typeof(Item), new[] { "GetFurthestDamagerCollider", "collisionHandlers", "parryPoint" }),
            new KeyValuePair<Type, string[]>(typeof(ItemModuleAI), new[] { "weaponAttackTypes", "weaponHandling", "primaryClass", "secondaryClass" }),
            new KeyValuePair<Type, string[]>(typeof(Damager), new[] { "type", "damagerID" })
        };

        public static bool Run()
        {
            var missing = new StringBuilder();
            int checkedCount = 0;
            foreach (var entry in Required)
            {
                foreach (string name in entry.Value)
                {
                    checkedCount++;
                    if (entry.Key.GetMember(name, All).Length == 0)
                    {
                        if (missing.Length > 0) missing.Append(", ");
                        missing.Append(entry.Key.Name).Append('.').Append(name);
                    }
                }
            }
            if (missing.Length == 0)
            {
                Log.Info($"API self-check OK ({checkedCount} members)");
                return true;
            }
            Log.Error("API self-check FAILED, the game changed: missing " + missing + ". The mod may misbehave; report this line.");
            return false;
        }
    }
}
