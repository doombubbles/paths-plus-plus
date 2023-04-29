using System;
using System.Collections.Generic;
using System.Reflection;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;

namespace PathsPlusPlus.Patches;

[HarmonyPatch]
internal static class TowerSelectionMenu_Show
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.Show));
        yield return AccessTools.Method(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.UpdateTower));
    }

    [HarmonyPostfix]
    private static void Postfix(TowerSelectionMenu __instance)
    {
        var controller = __instance.GetComponentInChildren<PathsPlusPlusController>();
        if (controller == null)
        {
            controller = PathsPlusPlusController.Create(__instance);
        }
    }
}

[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.SelectTower))]
internal static class TowerSelectionMenu_SelectTower
{
    [HarmonyPostfix]
    private static void Postfix(TowerSelectionMenu __instance, TowerToSimulation tower)
    {
        var controller = __instance.GetComponentInChildren<PathsPlusPlusController>();
        if (controller != null)
        {
            controller.SetMode(PathsPlusPlusMod.PathsByTower.ContainsKey(tower.Def.baseId));
        }
    }
}

[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.InitUpgradeButtons))]
internal static class TowerSelectionMenu_InitUpgradeButtons
{
    [HarmonyPostfix]
    private static void Postfix(TowerSelectionMenu __instance)
    {
        var controller = __instance.GetComponentInChildren<PathsPlusPlusController>();

        if (controller != null)
        {
            controller.InitUpgradeButtons();
        }
    }
}

/// <summary>
/// Set up getting the correct UpgradeModel within this method
/// </summary>
[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.LoadUpgrades))]
internal static class UpgradeObject_LoadUpgrades
{
    [HarmonyPrefix]
    private static void Prefix(UpgradeObject __instance)
    {
        if (__instance.gameObject.HasComponent(out UpgradeObjectPlusPlus upgradeObjectPlusPlus))
        {
            upgradeObjectPlusPlus.getLowerUpgrade = __instance.tier > 0;
        }
    }
}

/// <summary>
/// Control getting the correct UpgradeModel within UpgradeObject.LoadUpgrades
/// </summary>
[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.GetUpgrade))]
internal static class UpgradeObject_GetUpgrade
{
    [HarmonyPrefix]
    private static bool Prefix(UpgradeObject __instance, ref UpgradeModel? __result)
    {
        try
        {
            if (__instance.gameObject.HasComponent(out UpgradeObjectPlusPlus upgradeObjectPlusPlus))
            {
                var path = PathsPlusPlusMod.PathsById[upgradeObjectPlusPlus.pathId];
                if (upgradeObjectPlusPlus.getLowerUpgrade)
                {
                    __result = InGame.Bridge.Model.GetUpgrade(path.Upgrades[Math.Max(0, __instance.tier - 1)].Id);
                    upgradeObjectPlusPlus.getLowerUpgrade = false;
                }
                else if (__instance.tier < path.UpgradeCount)
                {
                    __result = InGame.Bridge.Model.GetUpgrade(path.Upgrades[__instance.tier].Id);
                }
                else
                {
                    __result = null;
                }

                return false;
            }
        }
        catch (Exception e)
        {
            ModHelper.Warning<PathsPlusPlusMod>(e);
        }

        return true;
    }
}

/// <summary>
/// Make each UpgradePlusPlus update alongside the others
/// </summary>
[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.UpdateUpgradeVisuals))]
internal static class TowerSelectionMenu_UpdateUpgradeVisuals
{
    [HarmonyPostfix]
    private static void Postfix(TowerSelectionMenu __instance)
    {
        var pathsPlusPlusController = __instance.GetComponentInChildren<PathsPlusPlusController>();

        if (pathsPlusPlusController != null)
        {
            pathsPlusPlusController.UpdateVisuals();
        }
    }
}

[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.CheckUpgradeCosts))]
internal static class TowerSelectionMenu_CheckUpgradeCosts
{
    [HarmonyPostfix]
    private static void Postfix(TowerSelectionMenu __instance)
    {
        var pathsPlusPlusController = __instance.GetComponentInChildren<PathsPlusPlusController>();

        if (pathsPlusPlusController != null)
        {
            pathsPlusPlusController.UpdateCosts();
        }
    }
}

[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.OnUpgradePricesChanged))]
internal static class TowerSelectionMenu_OnUpgradePricesChanged
{
    [HarmonyPostfix]
    private static void Postfix(TowerSelectionMenu __instance)
    {
        var pathsPlusPlusController = __instance.GetComponentInChildren<PathsPlusPlusController>();

        if (pathsPlusPlusController != null)
        {
            pathsPlusPlusController.UpdateCosts();
            pathsPlusPlusController.UpdateVisuals();
        }
    }
}

[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.CashChanged))]
internal static class TowerSelectionMenu_CashChanged
{
    [HarmonyPostfix]
    private static void Postfix(TowerSelectionMenu __instance)
    {
        var pathsPlusPlusController = __instance.GetComponentInChildren<PathsPlusPlusController>();

        if (pathsPlusPlusController != null)
        {
            pathsPlusPlusController.CheckCash();
        }
    }
}

/// <summary>
/// For getting next available upgrade
/// </summary>
[HarmonyPatch(typeof(Tower), nameof(Tower.GetUpgrade))]
internal static class Tower_GetUpgrade
{
    [HarmonyPrefix]
    private static bool Prefix(Tower __instance, int path, ref UpgradeModel? __result)
    {
        if (path >= 3 && PathPlusPlus.TryGetPath(__instance.towerModel.baseId, path, out var thisPath))
        {
            var tier = __instance.GetTier(thisPath.Id);

            __result = tier < thisPath.UpgradeCount ? __instance.Sim.model.GetUpgrade(thisPath.Upgrades[0].Id) : null;
            return false;
        }

        return true;
    }
}

/// <summary>
/// Show correct amount of upgrade pips
/// </summary>
[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.CheckBlockedPath))]
internal class UpgradeObject_CheckBlockedPath
{
    [HarmonyPrefix]
    private static bool Prefix(UpgradeObject __instance) => __instance.isActiveAndEnabled;

    [HarmonyPostfix]
    internal static void Postfix(UpgradeObject __instance, ref int __result)
    {
        if (!__instance.isActiveAndEnabled) return;

        var tower = __instance.towerSelectionMenu.selectedTower.tower;
        var path = __instance.path;
        var pathPlusPlus = PathPlusPlus.GetPath(tower.towerModel.baseId, path);
        var max = pathPlusPlus?.UpgradeCount ?? 5;
        
        if (!PathsPlusPlusMod.BalancedMode)
        {
            if (pathPlusPlus != null) __result = max;
            return;
        }
        
        var tiers = tower.GetAllTiers();
        var thisTier = tiers[path];

        __result = thisTier;
        for (var i = thisTier; i < max; i++)
        {
            tiers[path] = i + 1;

            if (!PathsPlusPlusMod.ValidTiers(tiers))
            {
                __result = i;
                return;
            }
        }
    }
}

[HarmonyPatch(typeof(TowerSelectionMenu.__c__DisplayClass62_0),
    nameof(TowerSelectionMenu.__c__DisplayClass62_0._UpgradeTower_b__0))]
internal static class TowerSelectionMenu_DisplayClass62_UpgradeTower
{
    [HarmonyPrefix]
    private static bool Prefix(TowerSelectionMenu.__c__DisplayClass62_0 __instance, bool isUpgraded)
    {
        if (__instance.path < 3) return true;

        var menu = __instance.__4__this;

        var upgradeObject = menu.upgradeButtons[__instance.path];

        if (!upgradeObject.isActiveAndEnabled) return false;
        
        if (!isUpgraded)
        {
            upgradeObject.tier = menu.selectedTower.tower.GetTier(__instance.path);
            upgradeObject.LoadUpgrades();
            __instance.__4__this.UpdateUpgradeVisuals();
        }

        upgradeObject.PostSimUpgrade();

        return false;
    }
}

[HarmonyPatch(typeof(TowerManager), nameof(TowerManager.GetTowerUpgradeCost))]
internal static class TowerManager_GetTowerUpgradeCost
{
    [HarmonyPostfix]
    private static void Postfix(Tower tower, int path, int tier, float overrideBaseCost, ref float __result)
    {
        // TODO wtf??
        if (path >= 3 && __result > 999999)
        {
            __result = overrideBaseCost;
        }
    }
}