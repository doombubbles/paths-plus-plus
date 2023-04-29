using HarmonyLib;
using Il2CppAssets.Scripts.Simulation.Input;
using Il2CppAssets.Scripts.Simulation.Towers;
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
            if (path < 3) return true;

            __result = false;
            return false;
        }

        var tower = __instance.selectedTower.tower;
        var tiers = tower.GetAllTiers();
        tiers[path]++;

        __result = !PathsPlusPlusMod.ValidTiers(tiers);
        return false;
    }
}

[HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.IsUpgradeBlocked))]
internal static class UpgradeButton_IsUpgradeBlocked
{
    [HarmonyPrefix]
    private static bool Prefix(UpgradeButton __instance, ref string? __result)
    {
        if (__instance.upgrade == null || __instance.upgrade.path < 3) return true;

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
        if (path < 3) return true;

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

[HarmonyPatch(typeof(TowerManager), nameof(TowerManager.IsTowerPathTierLocked))]
internal static class TowerManager_IsTowerPathTierLocked
{
    [HarmonyPrefix]
    private static bool Prefix(int path, ref bool __result)
    {
        if (path < 3) return true;

        __result = false;
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