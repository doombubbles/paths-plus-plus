using System;
using System.Linq;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;

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
    internal static UpgradeModel? current;
    internal static double cash;

    [HarmonyPrefix]
    private static bool Prefix(UnityToSimulation __instance, ObjectId id, int pathIndex, int callbackId, int inputId)
    {
        if (current == null || !PathsPlusPlusMod.UpgradesById.ContainsKey(current.name)) return true;

        var action = __instance.UnregisterCallback(callbackId, inputId);
        var towerManager = __instance.simulation.towerManager;
        var tower = towerManager.GetTowerById(id);

        var cost = towerManager.GetTowerUpgradeCost(tower, pathIndex, current.tier + 1, current.cost);

        // Perform normal upgrade affects
        towerManager.UpgradeTower(inputId, tower, tower.rootModel.Cast<TowerModel>(), 0, cost);
        InGame.instance.SetCash(cash - cost);

        // Apply the upgrade
        tower.SetTier(pathIndex, current.tier + 1);

        if (action != null)
        {
            action.Invoke(true);
        }

        UpgradeButton.upgradeCashOffset = 0;
        current = null;

        return false;
    }
}

[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.UpgradeTower), typeof(UpgradeModel), typeof(int),
    typeof(float), typeof(double))]
internal static class TowerSelectionMenu_UpgradeTower
{
    [HarmonyPrefix]
    private static void Prefix(UpgradeModel upgrade)
    {
        UnityToSimulation_UpgradeTower_Impl.current = upgrade;
        UnityToSimulation_UpgradeTower_Impl.cash = InGame.instance.GetCash();
    }
}