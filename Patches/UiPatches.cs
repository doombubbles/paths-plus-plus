using System;
using System.Collections.Generic;
using System.Reflection;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine.EventSystems;

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
        var controller = __instance.GetComponentInChildren<PathsPlusPlusController>(true);
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
        var controller = __instance.GetComponentInChildren<PathsPlusPlusController>(false);
        if (controller != null)
        {
            controller.SetMode(PathsPlusPlusMod.PathsByTower.ContainsKey(tower.Def.baseId));
        }
    }
}

[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.InitUpgradeButtons))]
internal static class TowerSelectionMenu_InitUpgradeButtons
{
    [HarmonyPrefix]
    private static void Prefix(TowerSelectionMenu __instance)
    {
        foreach (var upgradeButton in __instance.upgradeButtons)
        {
            if (upgradeButton.gameObject.HasComponent(out UpgradeObjectPlusPlus button))
            {
                button.pathId = null;
            }
        }
    }

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
        if (__instance.gameObject.HasComponent(out UpgradeObjectPlusPlus upgradeObj) && upgradeObj.IsExtra)
        {
            upgradeObj.getLowerUpgrade = __instance.tier > 0;
        }
    }

    [HarmonyPostfix]
    private static void Postfix(UpgradeObject __instance)
    {
        if (__instance.gameObject.HasComponent(out UpgradeObjectPlusPlus upgradeObj) &&
            upgradeObj.IsExtra &&
            __instance.tier >= 5)
        {
            var upgradeButton = __instance.upgradeButton;
            var upgradeModel = __instance.GetUpgrade(null);

            upgradeButton.SetUpgradeModel(upgradeModel);
            upgradeButton.UpdateVisuals(__instance.path, false);
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
            if (__instance.gameObject.HasComponent(out UpgradeObjectPlusPlus upgradeObj) &&
                upgradeObj.IsExtra &&
                upgradeObj.GetPath() is PathPlusPlus path)
            {
                if (upgradeObj.getLowerUpgrade)
                {
                    upgradeObj.getLowerUpgrade = false;
                    if (__instance is { path: < 3, tier: 5 }) return true;

                    __result = InGame.Bridge.Model.GetUpgrade(path.Upgrades[Math.Max(0, __instance.tier - 1)]!.Id);
                }
                else if (__instance.tier < path.UpgradeCount)
                {
                    __result = InGame.Bridge.Model.GetUpgrade(path.Upgrades[__instance.tier]!.Id);
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
        if (PathPlusPlus.TryGetPath(__instance.towerModel.baseId, path, out var thisPath))
        {
            var tier = __instance.GetTier(thisPath.Id);

            if (path < 3 && tier < 5) return true;

            __result = tier < thisPath.UpgradeCount
                ? __instance.Sim.model.GetUpgrade(thisPath.Upgrades[tier]!.Id)
                : null;

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

        if (!PathsPlusPlusMod.BalancedMode || path < 3 && ModHelper.HasMod("UltimateCrosspathing"))
        {
            if (pathPlusPlus != null) __result = max;
            return;
        }

        var tiers = tower.GetAllTiers();
        var thisTier = tiers[path];

        for (var i = thisTier; i <= max; i++)
        {
            __result = i;
            tiers[path] = i + 1;
            if (pathPlusPlus != null)
            {
                if (!pathPlusPlus.ValidTiers(tiers.ToArray()))
                {
                    return;
                }
            }
            else
            {
                if (!PathPlusPlus.DefaultValidTiers(tiers.ToArray()))
                {
                    return;
                }
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

/// <summary>
/// Fix discounts and cost rounding for the upgrades
/// </summary>
[HarmonyPatch(typeof(TowerManager), nameof(TowerManager.GetTowerUpgradeCost))]
internal static class TowerManager_GetTowerUpgradeCost
{
    private static void Prefix(Tower tower, int path, int tier, ref Il2CppReferenceArray<UpgradePathModel>? __state)
    {
        var towerModel = tower.towerModel;
        if ((tier > 5 || path >= 3) &&
            path >= 0 &&
            PathPlusPlus.TryGetPath(towerModel.baseId, path, out var pathPlusPlus))
        {
            __state = towerModel.upgrades;
            towerModel.upgrades = new[]
            {
                new UpgradePathModel(pathPlusPlus.Upgrades[tier - 1]!.Id, towerModel.name)
            };
        }
    }

    [HarmonyPostfix]
    private static void Postfix(Tower tower, ref Il2CppReferenceArray<UpgradePathModel>? __state)
    {
        if (__state != default)
        {
            tower.towerModel.upgrades = __state;
        }
    }
}

// Adding extra upgrade pips
[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.SetTier), typeof(int), typeof(int), typeof(int))]
internal static class UpgradeObject_SetTier
{
    [HarmonyPrefix]
    private static void Prefix(UpgradeObject __instance, ref int tier, ref int maxTier, ref int maxTierRestricted)
    {
        var tiers = __instance.tiers.ToList();
        var baseTier = tiers[0]!;

        var show = 5;

        if (__instance.gameObject.HasComponent(out UpgradeObjectPlusPlus upgradePlusPlus))
        {
            show = upgradePlusPlus.GetPath()?.UpgradeCount ?? 5;

            if (maxTier == 5) maxTier = Math.Max(5, show);
            if (maxTierRestricted == 5) maxTierRestricted = Math.Max(5, show);
        }

        while (show > tiers.Count)
        {
            var newTier = baseTier.Duplicate(baseTier.transform.parent);
            tiers.Add(newTier);
            newTier.name = "Tier " + tiers.Count;
        }

        if (tiers.Count > __instance.tiers.Length)
        {
            __instance.tiers = tiers.ToArray();
        }

        for (var i = 0; i < show; i++)
        {
            tiers[i].gameObject.SetActive(true);
        }

        for (var i = show; i < tiers.Count; i++)
        {
            tiers[i].gameObject.SetActive(false);
        }
    }
}

/// <summary>
/// Fix v38.1 inlining of TowerSelectionMenu.IsUpgradePathClosed method
/// </summary>
[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.UpdateVisuals))]
internal static class UpgradeObject_UpdateVisuals
{
    [HarmonyPrefix]
    private static bool Prefix(UpgradeObject __instance, int path, bool upgradeClicked)
    {
        if (ModHelper.HasMod("UltimateCrosspathing")) return false;

        if (__instance.towerSelectionMenu.IsUpgradePathClosed(path))
        {
            __instance.upgradeButton.SetUpgradeModel(null);
        }

        __instance.CheckLocked();
        var maxTier = __instance.CheckBlockedPath();
        var maxTierRestricted = __instance.CheckRestrictedPath();
        __instance.SetTier(__instance.tier, maxTier, maxTierRestricted);
        __instance.currentUpgrade.UpdateVisuals();
        __instance.upgradeButton.UpdateVisuals(path, upgradeClicked);

        return false;
    }
}

[HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.OnPointerDown))]
internal static class UpgradeButton_OnPointerDown
{
    [HarmonyPostfix]
    private static void Postfix(UpgradeButton __instance, PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right ||
            !TowerSelectionMenu.instance.selectedTower.CanUpgradeToParagon(true)) return;

        var button = __instance.GetComponentInParent<UpgradeObjectPlusPlus>();
        var upgradeObject = __instance.GetComponentInParent<UpgradeObject>();

        if (button == null || upgradeObject.tier < 5) return;

        var isExtra = button.IsExtra;
        button.overrideParagon = !button.overrideParagon;
        if (isExtra || button.IsExtra)
        {
            __instance.GetComponentInParent<UpgradeObject>().LoadUpgrades();
            MenuManager.instance.buttonClickSound.Play();
        }
    }
}