using System;
using System.Linq;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace PathsPlusPlus.Patches;

/// <summary>
/// Hijack our RateSupportMutators into mutators that apply the upgrades
/// </summary>
[HarmonyPatch(typeof(RateSupportModel.RateSupportMutator), nameof(RateSupportModel.RateSupportMutator.Mutate))]
internal static class RateMutator_Mutate
{
    [HarmonyPrefix]
    private static bool Prefix(RateSupportModel.RateSupportMutator __instance, Model model, ref bool __result)
    {
        if (!PathsPlusPlusMod.PathsById.TryGetValue(__instance.id, out var path)) return true;

        var tower = model.Cast<TowerModel>();
        var tier = Convert.ToInt32(__instance.multiplier);

        path.Apply(tower, tier);

        __result = true;
        return false;
    }
}

/// <summary>
/// Handle performing our custom upgrade action instead of standard behavior
/// </summary>
[HarmonyPatch(typeof(UnityToSimulation), nameof(UnityToSimulation.UpgradeTower_Impl))]
internal static class UnityToSimulation_UpgradeTower_Impl
{
    [HarmonyPrefix]
    private static bool Prefix(UnityToSimulation __instance, ObjectId id, int pathIndex, int callbackId, int inputId,
        double nonUpgradeCashInvestment)
    {
        var towerManager = __instance.simulation.towerManager;
        var tower = towerManager.GetTowerById(id);
        var tiers = tower.GetAllTiers();

        var tier = tiers[pathIndex] + 1;

        if (pathIndex < 3 && tier <= 5) return true;

        var action = __instance.UnregisterCallback(callbackId, inputId);
        var cost = 99999999f;

        if (towerManager.CanUpgradeTower(tower, pathIndex, tier, inputId, ref cost))
        {
            towerManager.UpgradeTower(inputId, tower, tower.rootModel.Cast<TowerModel>(), pathIndex, cost,
                nonUpgradeCashInvestment: nonUpgradeCashInvestment);
        }

        if (action != null)
        {
            action.Invoke(true);
        }

        UpgradeButton.upgradeCashOffset = 0;
        return false;
    }
}

/// <summary>
/// Get correct upgrade cost for paths++ upgrades
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

/// <summary>
/// Apply real path++ upgrade if applicable
/// </summary>
[HarmonyPatch(typeof(TowerManager), nameof(TowerManager.UpgradeTower))]
internal static class TowerManager_UpgradeTower
{
    [HarmonyPostfix]
    internal static void Postfix(Tower tower, int pathIndex)
    {
        var tiers = tower.GetAllTiers();
        if (pathIndex < 0 || pathIndex >= tiers.Count) return;
        var tier = tiers[pathIndex] + 1;
        if (pathIndex < 3 && tier <= 5) return;

        tower.SetTier(pathIndex, tier, true);
    }
}

/// <summary>
/// Check valid tiers
/// </summary>
[HarmonyPatch(typeof(TowerManager), nameof(TowerManager.IsTowerPathTierLocked))]
internal static class TowerManager_IsTowerPathTierLocked
{
    [HarmonyPrefix]
    private static bool Prefix(Tower tower, int path, int tier, ref bool __result)
    {
        if (path < 3 && tier <= 5) return true;

        var tiers = tower.GetAllTiers().ToArray();
        tiers[path] = tier;

        __result = PathsPlusPlusMod.PathsByTower.TryGetValue(tower.towerModel.baseId, out var paths) &&
                   paths.FirstOrDefault(plus => plus.Path == path) is { } pathPlusPlus &&
                   pathPlusPlus.Upgrades.TryGetValue(tier - 1, out var upgrade)
                   && upgrade.currentlyAppliedOn.Count >= upgrade.MaxAtOnce &&
                   pathPlusPlus.ValidTiers(tiers);

        return false;
    }
}