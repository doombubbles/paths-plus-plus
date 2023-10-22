using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Simulation.Towers;

namespace PathsPlusPlus;

/// <summary>
/// Extensions related to getting the tiers of PathsPlusPlus(s) from Towers
/// </summary>
public static class PathPlusPlusExtensions
{
    /// <summary>
    /// Gets what tier a tower is for a given PathPlusPlus path id
    /// </summary>
    /// <param name="tower">The tower</param>
    /// <param name="pathId">The path id</param>
    /// <returns></returns>
    public static int GetTier(this Tower tower, string pathId) =>
        PathsPlusPlusMod.PathsById.TryGetValue(pathId, out var path) ? path.GetTier(tower) : 0;

    /// <summary>
    /// Gets what tier a tower is for a given path number
    /// </summary>
    /// <param name="tower">The tower</param>
    /// <param name="path">The path number, 0,1,2 for vanilla or 3+ for PathsPlusPlus</param>
    /// <returns></returns>
    public static int GetTier(this Tower tower, int path) =>
        PathPlusPlus.TryGetPath(tower.towerModel.baseId, path, out var pathPlusPlus)
            ? pathPlusPlus.GetTier(tower)
            : 0;

    /// <summary>
    /// Gets all PathPlusPlus tiers for this tower. Only includes PathPlusPlus(s) that apply to the base tower.
    /// </summary>
    /// <param name="tower">The tower</param>
    /// <returns>Mapping from PathPlusPlus to tier</returns>
    public static Dictionary<PathPlusPlus, int> GetTiers(this Tower tower) =>
        PathsPlusPlusMod.PathsByTower.TryGetValue(tower.towerModel.baseId, out var paths)
            ? paths.ToDictionary(path => path, path => tower.GetTier(path.Id))
            : new Dictionary<PathPlusPlus, int>();


    private static int[] GetExtendedTiers(this Tower tower) =>
        PathsPlusPlusMod.ExtendedPathsByTower.TryGetValue(tower.towerModel.baseId, out var paths)
            ? paths.Select((path, i) => path?.GetTier(tower) ?? tower.towerModel.tiers[i]).ToArray()
            : tower.towerModel.tiers;

    /// <summary>
    /// Gets all the tiers for this tower, both base and PathPlusPlus. List index corresponds to path number
    /// </summary>
    /// <param name="tower"></param>
    /// <returns>List of all tiers</returns>
    public static List<int> GetAllTiers(this Tower tower) =>
        tower.GetExtendedTiers().Concat(tower.GetTiers().Values).ToList();


    /// <summary>
    /// Sets the tier for a given PathPlusPlus pathId to be a particular value.
    /// </summary>
    /// <param name="tower">The tower</param>
    /// <param name="pathId">The PathPlusPlus id</param>
    /// <param name="tier">The new desired tier</param>
    /// <param name="onUpgrade">Whether onUpgrade effects are triggered</param>
    public static void SetTier(this Tower tower, string pathId, int tier, bool onUpgrade = false)
    {
        if (!PathsPlusPlusMod.PathsById.TryGetValue(pathId, out var path))
        {
            ModHelper.Warning<PathsPlusPlusMod>($"No path found with id {pathId}");
            return;
        }

        path.SetTier(tower, tier, onUpgrade);
    }

    /// <summary>
    /// Sets the tier for a given PathPlusPlus number to be a particular value.
    /// </summary>
    /// <param name="tower">The tower</param>
    /// <param name="path">The PathPlusPlus number</param>
    /// <param name="tier">The new desired tier</param>
    /// <param name="onUpgrade">Whether onUpgrade effects are triggered</param>
    public static void SetTier(this Tower tower, int path, int tier, bool onUpgrade = false)
    {
        if (PathPlusPlus.TryGetPath(tower.towerModel.baseId, path, out var pathPlusPlus))
        {
            pathPlusPlus.SetTier(tower, tier, onUpgrade);
        }
    }


    /// <summary>
    /// Gets the PathPlusPlus tiers for a TowerModel based on the upgrade ids stored within its appliedUpgrades.
    /// Only includes PathPlusPlus(s) that could apply to the base tower.
    /// </summary>
    /// <param name="towerModel"></param>
    /// <returns>Mapping of PathPlusPlus to tier</returns>
    public static Dictionary<PathPlusPlus, int> GetTiers(this TowerModel towerModel) => towerModel.appliedUpgrades
        .Select(PathsPlusPlusMod.UpgradesById.GetValueOrDefault)
        .Where(upgrade => upgrade?.Path.Tower == towerModel.baseId)
        .GroupBy(upgrade => upgrade!.Path)
        .ToDictionary(grouping => grouping.Key, grouping => grouping.DefaultIfEmpty().Max(plus => plus?.Tier ?? 0));


    /// <summary>
    /// Gets all the tiers for this TowerModel, both base and PathPlusPlus.
    /// </summary>
    /// <param name="towerModel"></param>
    /// <returns>List of all tiers</returns>
    public static List<int> GetAllTiers(this TowerModel towerModel) =>
        towerModel.tiers.Concat(towerModel.GetTiers().Values).ToList();


    internal static PathPlusPlus? GetPath(this UpgradeObjectPlusPlus upgradePlusPlus) =>
        string.IsNullOrEmpty(upgradePlusPlus.pathId)
            ? null
            : PathsPlusPlusMod.PathsById.GetValueOrDefault(upgradePlusPlus.pathId, null!);
}