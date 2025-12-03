using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine.EventSystems;
using Object = Il2CppSystem.Object;

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
    private static void Postfix(TowerSelectionMenu __instance, TowerToSimulation? tower)
    {
        var controller = __instance.GetComponentInChildren<PathsPlusPlusController>(false);
        if (controller != null && tower is { Def: not null })
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

            upgradeButton.tier = 0;
        }

        if (__instance.upgradeButtons.Length > 3)
        {
            foreach (var upgradeObject in __instance.upgradeButtons.Skip(3))
            {
                upgradeObject.gameObject.SetActive(false);
            }
            __instance.upgradeButtons = __instance.upgradeButtons.Take(3).ToArray();
        }

        if (__instance.upgradeInfoPopups.Length > 3)
        {
            __instance.upgradeInfoPopups = __instance.upgradeInfoPopups.Take(3).ToArray();
        }
    }

    [HarmonyPostfix]
    private static void Postfix(TowerSelectionMenu __instance)
    {
        var controller = __instance.GetComponentInChildren<PathsPlusPlusController>();

        if (controller != null && !__instance.powerProDetails.activeSelf)
        {
            controller.InitUpgradeButtons();
        }
    }
}

/// <summary>
/// Set the correct UpgradeModels for the UpgradeButton and CurrentUpgrade
/// </summary>
[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.LoadUpgrades))]
internal static class UpgradeObject_LoadUpgrades
{
    [HarmonyPostfix]
    private static void Postfix(UpgradeObject __instance)
    {
        if (__instance.Is<PowerProUpgradeObject>() ||
            !__instance.gameObject.HasComponent(out UpgradeObjectPlusPlus upgradeObj) ||
            upgradeObj.GetPath() is not { } path ||
            !upgradeObj.IsExtra) return;

        var tower = __instance.towerSelectionMenu.selectedTower.tower;

        if (__instance.tier < path.UpgradeCount &&
            !InGame.Bridge.IsUpgradeLocked(tower.Id, __instance.path, __instance.tier + 1))
        {
            var upgradeButton = __instance.upgradeButton;
            var upgradeModel = InGame.Bridge.Model.GetUpgrade(path.Upgrades[__instance.tier].Id);
            if (__instance.tts.CanUpgradeToParagon(true) &&
                (!upgradeObj.overrideParagon || __instance.tier == path.UpgradeCount))
            {
                upgradeModel = InGame.Bridge.Model.GetUpgrade(__instance.tts.Def.paragonUpgrade.upgrade);
            }

            upgradeButton.SetUpgradeModel(upgradeModel);
            upgradeButton.UpdateVisuals(__instance.path, upgradeModel.tier + 1, false);
        }

        if (__instance.tier > 0 && (__instance.path > 2 || __instance.tier > 5))
        {
            __instance.currentUpgradeModel = InGame.Bridge.Model.GetUpgrade(path.Upgrades[__instance.tier - 1].Id);
            __instance.currentUpgrade.SetUpgradeModel(__instance.currentUpgradeModel, __instance.tts);
        }
    }
}

/// <summary>
/// Make each UpgradePlusPlus update alongside the others
/// </summary>
[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.OnUpdateUpgradeVisuals))]
internal static class TowerSelectionMenu_OnUpdateUpgradeVisuals
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
    private static bool Prefix(UpgradeObject __instance, ref int __result)
    {
        if (!__instance.isActiveAndEnabled) return false;
        if (__instance.Is<PowerProUpgradeObject>()) return true;

        var tower = __instance.towerSelectionMenu.selectedTower.tower;
        var path = __instance.path;
        var pathPlusPlus = PathPlusPlus.GetPath(tower.towerModel.baseId, path);
        var max = pathPlusPlus?.UpgradeCount ?? 5;

        if (!PathsPlusPlusMod.BalancedMode && pathPlusPlus != null)
        {
            __result = max;
            return false;
        }

        if (pathPlusPlus == null &&
            (!PathsPlusPlusMod.BalancedMode || path < 3 && ModHelper.HasMod("UltimateCrosspathing")))
        {
            return true;
        }

        var tiers = tower.GetAllTiers();
        var thisTier = tiers[path];

        var paths = PathsPlusPlusMod.PathsByTower.GetValueOrDefault(tower.towerModel.baseId, []);

        for (var i = thisTier; i <= max; i++)
        {
            __result = i;
            tiers[path] = i + 1;
            if (pathPlusPlus != null)
            {
                if (!pathPlusPlus.ValidTiers(tiers.ToArray())) break;
            }
            else
            {
                if (!PathPlusPlus.DefaultValidTiers(tiers.Take(3).ToArray()) ||
                    paths.Any(p => !p.ValidTiers(tiers.ToArray()))) break;
            }
        }

        return false;
    }
}

[HarmonyPatch]
internal static class TowerSelectionMenu_DisplayClass_UpgradeTower
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return typeof(TowerSelectionMenu)
            .GetNestedTypes()
            .SelectMany(type => type.GetMethods("_UpgradeTower_b__0"))
            .Single(info => info.GetParameters().Any(param => param.Name == "isUpgraded"));
    }

    [HarmonyPrefix]
    private static bool Prefix(Object __instance, bool isUpgraded)
    {
        var path = __instance.GetIl2CppType().GetField("path").GetValue(__instance).Unbox<int>();
        if (path < 3) return true;

        var menu = __instance.GetIl2CppType().GetField("<>4__this").GetValue(__instance).Cast<TowerSelectionMenu>();

        var upgradeObject = menu.upgradeButtons[path];

        if (!upgradeObject.isActiveAndEnabled) return false;

        if (!isUpgraded)
        {
            upgradeObject.tier = menu.selectedTower.tower.GetTier(path);
            upgradeObject.LoadUpgrades();
            menu.OnUpdateUpgradeVisuals();
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
    private static void Prefix(Tower? tower, int path, int tier, ref Il2CppReferenceArray<UpgradePathModel>? __state)
    {
        if (tower?.towerModel?.Is(out var towerModel) != true ||
            (tier <= 5 && path < 3) ||
            path < 0 ||
            !PathPlusPlus.TryGetPath(towerModel.baseId, path, out var pathPlusPlus)) return;

        __state = towerModel.upgrades;
        towerModel.upgrades = new[]
        {
            new UpgradePathModel(pathPlusPlus.Upgrades[tier - 1]!.Id, towerModel.name)
        };
    }

    [HarmonyPostfix]
    private static void Postfix(Tower tower, ref Il2CppReferenceArray<UpgradePathModel>? __state)
    {
        if (__state != null)
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
        if (__instance.Is<PowerProUpgradeObject>()) return;

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

[HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.OnPointerDown))]
internal static class UpgradeButton_OnPointerDown
{
    [HarmonyPostfix]
    private static void Postfix(UpgradeButton __instance, PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right &&
            TowerSelectionMenu.instance.selectedTower.CanUpgradeToParagon(true))
        {
            __instance.GetComponentInParent<UpgradeObjectPlusPlus>().Exists()?.CycleParagon();
        }
    }
}