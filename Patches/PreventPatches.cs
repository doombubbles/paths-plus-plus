using System.Linq;
using BTD_Mod_Helper;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Input;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using Il2CppAssets.Scripts.Simulation.Tracking;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;

namespace PathsPlusPlus.Patches;

[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.IsUpgradePathClosed))]
internal static class TowerSelectionMenu_IsUpgradePathClosed
{
    [HarmonyPrefix]
    private static bool Prefix(TowerSelectionMenu __instance, int path, ref bool __result)
    {
        if (!PathsPlusPlusMod.BalancedMode)
        {
            __result = false;
            return false;
        }

        var tower = __instance.selectedTower.tower;
        var towerId = tower.towerModel.baseId;
        var tiers = tower.GetAllTiers();
        tiers[path]++;

        if (PathPlusPlus.TryGetPath(tower.towerModel.baseId, path, out var pathPlusPlus))
        {
            __result = !pathPlusPlus.ValidTiers(tiers.ToArray());
            return false;
        }

        if (PathsPlusPlusMod.PathsByTower.TryGetValue(towerId, out var paths))
        {
            __result = !PathPlusPlus.DefaultValidTiers(tiers.Take(3).ToArray()) ||
                       paths.Any(p => !p.ValidTiers(tiers.ToArray()));
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.IsUpgradeBlocked))]
internal static class UpgradeButton_IsUpgradeBlocked
{
    [HarmonyPrefix]
    private static bool Prefix(UpgradeButton __instance, ref string? __result)
    {
        if (__instance.row < 3) return true;

        __result = null;
        return false;
    }
}

[HarmonyPatch(typeof(TowerInventory), nameof(TowerInventory.IsPathTierLocked))]
internal static class TowerInventory_IsPathTierLocked
{
    [HarmonyPrefix]
    private static bool Prefix(int path, ref bool __result)
    {
        if (path < 3) return true;

        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TowerToSimulation), nameof(TowerToSimulation.GetMaxTierForPath))]
internal static class TowerToSimulation_GetMaxTierForPath
{
    [HarmonyPrefix]
    private static bool Prefix(TowerToSimulation __instance, int path, ref int __result)
    {
        __result = PathPlusPlus.TryGetPath(__instance.Def.baseId, path, out var pathPlusPlus)
            ? pathPlusPlus.UpgradeCount
            : 5;

        return false;
    }
}

[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.CheckRestrictedPath))]
internal static class UpgradeObject_CheckRestrictedPath
{
    [HarmonyPrefix]
    private static bool Prefix(UpgradeObject __instance, ref int __result)
    {
        if (__instance.path < 3) return true;

        __result = 5;
        return false;
    }
}

[HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.CheckDcLocked))]
internal static class UpgradeButton_CheckDcLocked
{
    [HarmonyPrefix]
    private static bool Prefix(int path, ref bool __result)
    {
        if (path < 3) return true;

        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.UpdateBeastHandlerUI))]
internal static class UpgradeButton_UpdateBeastHandlerUI
{
    [HarmonyPrefix]
    private static bool Prefix(int path)
    {
        return path < 3;
    }
}

/// <summary>
/// Prevent PathPlusPlus mutators from ever being added to subtowers
/// </summary>
[HarmonyPatch(typeof(Tower), nameof(Tower.AddMutator))]
internal static class Tower_AddMutator
{
    [HarmonyPrefix]
    private static bool Prefix(Tower __instance, BehaviorMutator mutator) =>
        !(__instance.towerModel.isSubTower && PathsPlusPlusMod.PathsById.ContainsKey(mutator.id));
}

[HarmonyPatch(typeof(UpgradeModel), nameof(UpgradeModel.IsParagon), MethodType.Getter)]
internal static class UpgradeModel_IsParagon
{
    [HarmonyPrefix]
    private static bool Prefix(UpgradeModel __instance, ref bool __result)
    {
        if (PathsPlusPlusMod.UpgradesById.ContainsKey(__instance.name))
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.PostSimUpgrade))]
internal static class UpgradeObject_PostSimUpgrade
{
    [HarmonyPrefix]
    private static bool Prefix(UpgradeObject __instance)
    {
        return __instance.isActiveAndEnabled;
    }
}

[HarmonyPatch(typeof(BeastHandlerUpgradeLock), nameof(BeastHandlerUpgradeLock.IsUpgradeBlocked))]
internal static class BeastHandlerUpgradeLock_IsUpgradeBlocked
{
    [HarmonyPrefix]
    private static bool Prefix(BeastHandlerUpgradeLock __instance, int path, ref bool __result)
    {
        if (path >= 3)
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.UpdateMergeButton))]
internal static class UpgradeButton_UpdateMergeButton
{
    [HarmonyPrefix]
    internal static bool Prefix(UpgradeButton __instance)
    {
        return __instance.row < 3;
    }
}

[HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.IsBeastHandlerContributionUpgradeAvailable),
    MethodType.Getter)]
internal static class UpgradeButton_IsBeastHandlerContributionUpgradeAvailable
{
    [HarmonyPrefix]
    internal static bool Prefix(UpgradeButton __instance)
    {
        return __instance.row < 3;
    }
}

[HarmonyPatch(typeof(CurrentUpgrade), nameof(CurrentUpgrade.UpdateBeastHandlerDisplay))]
internal static class CurrentUpgrade_UpdateBeastHandlerDisplay
{
    [HarmonyPrefix]
    internal static bool Prefix(CurrentUpgrade __instance)
    {
        return __instance.row < 3;
    }
}

[HarmonyPatch(typeof(AnalyticsTrackerSim), nameof(AnalyticsTrackerSim.OnTowerUpgraded))]
internal static class AnalyticsTrackerSim_OnTowerUpgraded
{
    [HarmonyPrefix]
    internal static bool Prefix(Tower tower, int pathUpgraded, bool isParagon)
    {
        if (isParagon) return true;
        if (pathUpgraded >= 3) return false;

        var tiers = tower.GetAllTiers();
        var tier = tiers[pathUpgraded];

        return tier < 6;
    }
}