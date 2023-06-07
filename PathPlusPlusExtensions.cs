using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;

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
    public static int GetTier(this Tower tower, string pathId)
    {
        var mutatorById = tower.GetMutatorById(pathId);
        if (mutatorById == null || !mutatorById.mutator.Is(out RateSupportModel.RateSupportMutator mutator)) return 0;

        return Convert.ToInt32(mutator.Cast<RateSupportModel.RateSupportMutator>().multiplier);
    }

    /// <summary>
    /// Gets what tier a tower is for a given path number
    /// </summary>
    /// <param name="tower">The tower</param>
    /// <param name="path">The path number, 0,1,2 for vanilla or 3+ for PathsPlusPlus</param>
    /// <returns></returns>
    public static int GetTier(this Tower tower, int path) => path < 3
        ? tower.towerModel.tiers[path]
        : PathPlusPlus.TryGetPath(tower.towerModel.baseId, path, out var pathPlusPlus)
            ? GetTier(tower, pathPlusPlus.Id)
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

    /// <summary>
    /// Gets all the tiers for this tower, both base and PathPlusPlus. List index corresponds to path number
    /// </summary>
    /// <param name="tower"></param>
    /// <returns>List of all tiers</returns>
    public static List<int> GetAllTiers(this Tower tower) =>
        tower.towerModel.tiers.Concat(tower.GetTiers().Values).ToList();


    /// <summary>
    /// Sets the tier for a given PathPlusPlus pathId to be a particular value.
    /// Will handle the mutation of the tower but not any upgrade side effects
    /// </summary>
    /// <param name="tower">The tower</param>
    /// <param name="pathId">The PathPlusPlus id</param>
    /// <param name="tier">The new desired tier</param>
    public static void SetTier(this Tower tower, string pathId, int tier)
    {
        if (!PathsPlusPlusMod.PathsById.TryGetValue(pathId, out var path))
        {
            ModHelper.Warning<PathsPlusPlusMod>($"No path found with id {pathId}");
            return;
        }

        tower.RemoveMutatorsById(pathId);
        tower.AddMutator(new RateSupportModel.RateSupportMutator(true, pathId, tier, path.Priority, null));
    }

    /// <summary>
    /// Sets the tier for a given PathPlusPlus number to be a particular value.
    /// Will handle the mutation of the tower but not any upgrade side effects
    /// </summary>
    /// <param name="tower">The tower</param>
    /// <param name="path">The PathPlusPlus number</param>
    /// <param name="tier">The new desired tier</param>
    public static void SetTier(this Tower tower, int path, int tier)
    {
        if (PathPlusPlus.TryGetPath(tower.towerModel.baseId, path, out var pathPlusPlus))
        {
            SetTier(tower, pathPlusPlus.Id, tier);
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
    /// Gets all the tiers for this TowerModel, both base and PathPlusPlus. List index corresponds to path number
    /// </summary>
    /// <param name="towerModel"></param>
    /// <returns>List of all tiers</returns>
    public static List<int> GetAllTiers(this TowerModel towerModel) =>
        towerModel.tiers.Concat(towerModel.GetTiers().Values).ToList();
}