using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Towers;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity;
using MelonLoader;

namespace PathsPlusPlus;

/// <summary>
/// Class to register your path, defining what tower its for and how many upgrades you're adding
/// </summary>
public abstract class PathPlusPlus : ModContent
{
    /// <inheritdoc />
    protected sealed override float RegistrationPriority => 11;

    internal int StartTier => ExtendVanillaPath >= 0 ? Upgrades.Keys.DefaultIfEmpty(5).Min() : 0;

    /// <summary>
    /// The tower id this path is for, use <code>TowerType.XXX</code>
    /// </summary>
    public abstract string Tower { get; }

    /// <summary>
    /// How many upgrades this path has.
    /// <br/>
    /// If you're setting <see cref="ExtendVanillaPath"/>, this will be the new max upgrade count for the path.
    /// </summary>
    public virtual int UpgradeCount => StartTier + Upgrades.Count;

    /// <summary>
    /// Which internal path id this PathPlusPlus will have.
    /// </summary>
    public int Path { get; private set; }

    /// <summary>
    /// Set this to one of the <see cref="Top"/>, <see cref="Middle"/>, or <see cref="Bottom"/> constants to cause
    /// <see cref="UpgradePlusPlus"/>s for this path to append to a vanilla path instead of creating a new path.
    /// </summary>
    public virtual int ExtendVanillaPath => -1;

    /// <summary>
    /// Path ID for the Top path
    /// </summary>
    protected internal const int Top = 0;

    /// <summary>
    /// Path ID for the Middle path
    /// </summary>
    protected internal const int Middle = 1;

    /// <summary>
    /// Path ID for the Bottom path
    /// </summary>
    protected internal const int Bottom = 2;

    /// <summary>
    /// The UpgradePlusPlus(s) for this path
    /// </summary>
    public readonly SortedDictionary<int, UpgradePlusPlus> Upgrades = [];

    /// <summary>
    /// Whether this path should appear in the Upgrades Screen for the tower
    /// </summary>
    public virtual bool ShowInMenu => true;

    /// <summary>
    /// Logic for whether an Upgrade++ should be allowed given the current tiers for a tower.
    ///<br/>
    /// By default, it will first check if <see cref="PathsPlusPlusMod.BalancedMode"/> is false and return false.
    /// <br/>
    /// Otherwise, will use <see cref="DefaultValidTiers"/>
    /// </summary>
    /// <param name="tiers">List of all tiers for this tower, will be length 3+</param>
    /// <returns>Whether to block the upgrade from happening</returns>
    public virtual bool ValidTiers(int[] tiers) => DefaultValidTiers(tiers);

    /// <summary>
    /// Used for paths utilizing <see cref="ExtendVanillaPath"/> with upgrade tiers below 6
    /// Set to true to make the base TowerModel for the upgrades be the corresponding upgraded version on the original path, rather than the last TowerModel before the branching off point
    /// </summary>
    public virtual bool UseUpgradedTowerModels => false;

    /// <summary>
    /// Ensure that either Ultimate Crosspathing is being used, or that the tiers have at most 1 total path upgraded
    /// past tier 2, and at most 2 total paths upgraded past tier 0.
    /// </summary>
    /// <param name="tiers"></param>
    /// <returns></returns>
    public static bool DefaultValidTiers(int[] tiers) =>
        ModHelper.HasMod("UltimateCrosspathing") || tiers.Count(i => i > 2) <= 1 && tiers.Count(i => i > 0) <= 2;

    /// <inheritdoc />
    public override void Register()
    {
        var highest = Upgrades.Values.Select(upgrade => upgrade.Tier).DefaultIfEmpty(0).Max();
        if (ExtendVanillaPath >= 0 && highest < 5)
        {
            highest = 5; // TODO allow non-contiguous upgrade path alternates?
        }

        for (var tier = StartTier; tier < highest; tier++)
        {
            if (!Upgrades.ContainsKey(tier))
            {
                throw new Exception($"Path {Id} is missing Upgrade for tier {tier + 1}");
            }
        }

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        PathsPlusPlusMod.PathsById[Id] = this;

        if (ExtendVanillaPath is >= Top and <= Bottom)
        {
            Path = ExtendVanillaPath;

            var pathsByPath = PathsPlusPlusMod.ExtendedPathsByTower.GetOrAdd(Tower, []);
            var paths = pathsByPath.GetOrAdd(Path, []);

            paths.Add(this);
        }
        else
        {
            var paths = PathsPlusPlusMod.PathsByTower.GetOrAdd(Tower, []);
            Path = 3 + paths.Count;
            paths.Add(this);
        }

        foreach (var upgrade in Upgrades.Values)
        {
            Game.instance.model.AddUpgrade(upgrade.GetUpgradeModel());
        }
    }

    /// <summary>
    /// Runs in game when one of the Upgrades of this path is first applied to a tower
    /// </summary>
    /// <param name="tower">The tower receiving the upgrade</param>
    /// <param name="tier">The tier of the upgrade</param>
    public virtual void OnUpgraded(Tower tower, int tier)
    {
    }

    /// <summary>
    /// Runs in game when one of the Upgrades of this path is attached to a Tower, whether by having just purchased the upgrade or the tower being loaded from save, etc
    /// Note that this may apply to a tower multiple times
    /// </summary>
    /// <param name="tower">The tower receiving the upgrade</param>
    /// <param name="tier">The tier of the upgrade</param>
    public virtual void OnAttached(Tower tower, int tier)
    {
    }

    /// <summary>
    /// The Priority given to the PathPlusPlus mutator on the tower
    /// </summary>
    protected virtual int Priority => 100 - Order;

    /// <summary>
    /// Applies all upgrades for this path up through the given tier on a TowerModel.
    /// </summary>
    /// <param name="tower">TowerModel to apply to</param>
    /// <param name="tier">Up to and including this tier number</param>
    public void Apply(TowerModel tower, int tier)
    {
        tower.tier = Math.Max(tower.tier, Math.Min(5, tier));
        var appliedUpgrades = tower.appliedUpgrades.ToList();

        if (UseUpgradedTowerModels)
        {
            foreach (var upgradeModel in tower.appliedUpgrades.Select(Game.instance.model.GetUpgrade))
            {
                if (upgradeModel.path == Path && upgradeModel.tier >= StartTier)
                {
                    appliedUpgrades.Remove(upgradeModel.name);
                }
            }
        }

        for (var i = 0; i < tier; i++)
        {
            if (!Upgrades.TryGetValue(i, out var upgrade) || appliedUpgrades.Contains(upgrade.Id)) continue;

            upgrade.ApplyUpgrade(tower);
            upgrade.ApplyUpgrade(tower, tier);
            if (upgrade.IsHighestUpgrade(tower) && upgrade.PortraitReference is not null)
            {
                tower.portrait = upgrade.PortraitReference;
            }
            appliedUpgrades.Add(upgrade.Id);
        }

        tower.appliedUpgrades = appliedUpgrades.ToArray();
    }

    /// <summary>
    /// Gets what tier a tower has for this path++
    /// </summary>
    /// <param name="tower"></param>
    /// <returns></returns>
    public virtual int GetTier(Tower tower)
    {
        var mutator = GetMutator(tower);
        return mutator == null ? 0 : Convert.ToInt32(mutator.multiplier);
    }

    /// <summary>
    /// Gets the mutator used to track this paths' state on a tower.
    /// The multiplier field stores the tier.
    /// A tier of -1 will sometimes be set on a tower to mark that path as the default to be chosen if there are multiple
    /// options for the same path index.
    /// </summary>
    /// <returns>mutator, or null if not present</returns>
    public RateSupportModel.RateSupportMutator? GetMutator(Tower tower) =>
        tower.GetMutatorById(Id)?.mutator?.TryCast<RateSupportModel.RateSupportMutator>();

    /// <summary>
    /// Sets the tier for a given PathPlusPlus number to be a particular value.
    /// Will handle the mutation of the tower but not any upgrade side effects
    /// </summary>
    /// <param name="tower">The tower</param>
    /// <param name="tier">The new desired tier</param>
    /// <param name="onUpgrade">Whether to perform OnUpgrade effects</param>
    public void SetTier(Tower tower, int tier, bool onUpgrade = false)
    {
        tower.RemoveMutatorsById(Id);
        tower.AddMutator(new RateSupportModel.RateSupportMutator(true, Id, tier, Priority, null)
        {
            cantBeAbsorbed = true
        });

        for (var i = 0; i < tier; i++)
        {
            if (Upgrades.TryGetValue(i, out var u))
            {
                OnAttached(tower, tier);
                u.OnAttached(tower);
                u.currentlyAppliedOn.Add(tower);
            }
        }

        if (onUpgrade && Upgrades.TryGetValue(tier - 1, out var upgrade))
        {
            OnUpgraded(tower, tier);
            upgrade.OnUpgraded(tower);
            if (mod is BloonsTD6Mod btd6Mod)
            {
                btd6Mod.OnTowerUpgraded(tower, upgrade.Name, tower.rootModel.Cast<TowerModel>());
            }
        }
    }

    /// <summary>
    /// Gets the PathPlusPlus for a given tower and path number, or null if not found
    /// </summary>
    /// <param name="tower">The tower id</param>
    /// <param name="path">The path number</param>
    /// <returns>PathPlusPlus or null</returns>
    [Obsolete("Should pass in an actual Tower object now")]
    public static PathPlusPlus? GetPath(string tower, int path) => path < 3
        ? PathsPlusPlusMod.ExtendedPathsByTower.TryGetValue(tower, out var pathsByPath)
            ? pathsByPath.TryGetValue(path, out var paths) ? paths.First() : null
            : null
        : PathsPlusPlusMod.PathsByTower.TryGetValue(tower, out var list)
            ? list.FirstOrDefault(plus => plus.Path == path)
            : null;

    /// <summary>
    /// Gets all possible PathsPlusPlus that are possible for a base tower at the given path index.
    /// For pathIndex > 2 this will always just be empty or 1 PathPlusPlus,
    /// but for pathIndex 0-2 this may be multiple paths that all extend the same VanillaPath
    /// </summary>
    /// <param name="tower">tower id</param>
    /// <param name="path">path index</param>
    /// <returns>all possible paths</returns>
    public static IList<PathPlusPlus> GetPaths(string tower, int path)
    {
        if (path >= 3)
            return PathsPlusPlusMod.PathsByTower.TryGetValue(tower, out var list)
                ? list.Where(p => p.Path == path).ToArray()
                : [];

        if (!PathsPlusPlusMod.ExtendedPathsByTower.TryGetValue(tower, out var pathsByPath))
            return [];

        if (!pathsByPath.TryGetValue(path, out var paths))
            return [];

        return paths;
    }

    /// <summary>
    /// Gets all possible PathsPlusPlus that are possible for a specific tower at the given path index.
    /// For pathIndex > 2 this will always just be empty or 1 PathPlusPlus,
    /// but for pathIndex 0-2 this may be multiple paths that all extend the same VanillaPath
    /// </summary>
    /// <param name="tower">tower</param>
    /// <param name="path">path index</param>
    /// <returns>all possible paths</returns>
    public static IList<PathPlusPlus> GetPaths(Tower tower, int path)
    {
        var paths = GetPaths(tower.towerModel.baseId, path);

        if (path >= 3) return paths;

        foreach (var p in paths)
        {
            if (p.GetTier(tower) > 0) return [p];
        }

        return paths.Where(p => tower.towerModel.tiers[path] <= p.StartTier).ToArray();
    }

    /// <summary>
    /// Gets the specific PathPlusPlus that this tower is using for the given path index, or null if it's not using a specific one
    /// </summary>
    /// <param name="tower">tower</param>
    /// <param name="path">path index</param>
    /// <returns>PathPlusPlus or null </returns>
    public static PathPlusPlus? GetPath(Tower tower, int path)
    {
        var paths = GetPaths(tower, path);
        if (path >= 3)
            return paths.FirstOrDefault();

        foreach (var p in paths)
        {
            if (p.GetMutator(tower) != null) return p; // If mutator already present, always choose the path
        }

        return null;
    }

    /// <summary>
    /// Gets the specific PathPlusPlus that this tower should be showing as what it's about to upgrade to
    /// </summary>
    /// <param name="tower">tower</param>
    /// <param name="path">path index</param>
    /// <param name="tier">tower tier about to be upgraded to, 1 indexed</param>
    /// <returns>PathPlusPlus or null </returns>
    public static PathPlusPlus? GetPath(Tower tower, int path, int tier)
    {
        if (path >= 3)
            return GetPath(tower, path);

        var paths = GetPaths(tower.towerModel.baseId, path);

        foreach (var p in paths)
        {
            if (p.GetTier(tower) > 0) return p; // If tier already set, always choose the path
        }

        foreach (var p in paths)
        {
            if (p.GetMutator(tower) != null) return p; // If mutator already present, always choose the path
        }

        paths = paths.Where(p => p.StartTier == tier - 1).ToArray();

        if (tier >= 6 && paths.Any())
        {
            return paths.First(); // default to showing the first path for tier 6+ vanilla path extensions
        }

        return null; // default to not showing a path for vanilla path extensions that start before tier 6
    }

    /// <summary>
    /// Tries to get the PathPlusPlus for a given tower and path number, or null if not found
    /// </summary>
    /// <param name="tower"></param>
    /// <param name="path"></param>
    /// <param name="pathPlusPlus"></param>
    /// <returns>Whether one was found</returns>
    [Obsolete("Should pass in an actual Tower object now")]
    public static bool TryGetPath(string tower, int path, out PathPlusPlus pathPlusPlus) =>
        (pathPlusPlus = GetPath(tower, path)!) != null;

    /// <summary>
    /// Tries getting the specific PathPlusPlus that this tower is using for the given path index, or null if it's not using a specific one
    /// </summary>
    /// <param name="tower">tower</param>
    /// <param name="path">path index</param>
    /// <param name="pathPlusPlus">PathPlusPlus or null</param>
    public static bool TryGetPath(Tower tower, int path, out PathPlusPlus pathPlusPlus) =>
        (pathPlusPlus = GetPath(tower, path)!) != null;


    /// <summary>
    /// Tries getting the specific PathPlusPlus that this tower should be showing as what it's about to upgrade to
    /// </summary>
    /// <param name="tower">tower</param>
    /// <param name="path">path index</param>
    /// <param name="tier">tower tier about to be upgraded to, 1 indexed</param>
    /// <param name="pathPlusPlus">PathPlusPlus or null</param>
    public static bool TryGetPath(Tower tower, int path, int tier, out PathPlusPlus pathPlusPlus) =>
        (pathPlusPlus = GetPath(tower, path, tier)!) != null;
}

/// <summary>
/// PathPlusPlus for a ModTower
/// </summary>
/// <typeparam name="T">The ModTower</typeparam>
public abstract class PathPlusPlus<T> : PathPlusPlus where T : ModTower
{
    /// <inheritdoc />
    public sealed override string Tower => GetInstance<T>().Id;
}