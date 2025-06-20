﻿using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Towers;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Models.TowerSets;
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

    internal int StartTier => ExtendVanillaPath >= 0 ? 5 : 0;

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
    public readonly SortedDictionary<int, UpgradePlusPlus> Upgrades = new();

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
    /// Ensure that either Ultimate Crosspathing is being used, or that the tiers have at most 1 total path upgraded
    /// past tier 2, and at most 2 total paths upgraded past tier 0.
    /// </summary>
    /// <param name="tiers"></param>
    /// <returns></returns>
    public static bool DefaultValidTiers(int[] tiers) =>
        ModHelper.HasMod("UltimateCrosspathing") || tiers.Count(i => i > 2) <= 1 && tiers.Count(i => i > 0) <= 2;

    internal MelonPreferences_Entry<bool> Override { get; private set; } = null!;

    /// <inheritdoc />
    public override void Register()
    {
        var highest = Upgrades.Values.Select(upgrade => upgrade.Tier).DefaultIfEmpty(0).Max();
        for (var tier = StartTier; tier < highest; tier++)
        {
            if (!Upgrades.ContainsKey(tier))
            {
                throw new Exception($"Path {Id} is missing Upgrade for tier {tier + 1}");
            }
        }

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        Override ??= PathsPlusPlusMod.Preferences.CreateEntry(Id, false);
        PathsPlusPlusMod.PathsById[Id] = this;
        
        AssignPath();

        foreach (var upgrade in Upgrades.Values)
        {
            Game.instance.model.AddUpgrade(upgrade.GetUpgradeModel());
        }
    }

    internal void AssignPath()
    {
        if (ExtendVanillaPath is >= Top and <= Bottom)
        {
            Path = ExtendVanillaPath;

            if (!PathsPlusPlusMod.ExtendedPathsByTower.TryGetValue(Tower, out var array))
                array = PathsPlusPlusMod.ExtendedPathsByTower[Tower] = new PathPlusPlus[3];

            if (array[Path] is PathPlusPlus other)
            {
                if (!Override.Value) return;

                ModHelper.Msg<PathsPlusPlusMod>($"Overriding {other.Id} with {Id} for {Tower} path {Path}");
            }

            array[Path] = this;
        }
        else
        {
            if (!PathsPlusPlusMod.PathsByTower.TryGetValue(Tower, out var list))
                list = PathsPlusPlusMod.PathsByTower[Tower] = [];
            Path = 3 + list.Count;
            list.Add(this);
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
        for (var i = 0; i < tier; i++)
        {
            if (!Upgrades.TryGetValue(i, out var upgrade) || tower.appliedUpgrades.Contains(upgrade.Id)) continue;

            upgrade.ApplyUpgrade(tower);
            upgrade.ApplyUpgrade(tower, tier);
            if (upgrade.IsHighestUpgrade(tower) && upgrade.PortraitReference is not null)
            {
                tower.portrait = upgrade.PortraitReference;
            }

            tower.appliedUpgrades = tower.appliedUpgrades.AddTo(upgrade.Id);
        }
    }

    /// <summary>
    /// Gets what tier a tower has for this path++
    /// </summary>
    /// <param name="tower"></param>
    /// <returns></returns>
    public int GetTier(Tower tower)
    {
        var mutatorById = tower.GetMutatorById(Id);
        if (mutatorById == null || !mutatorById.mutator.Is(out RateSupportModel.RateSupportMutator mutator))
            return Path <= 2 ? tower.towerModel.tiers[Path] : 0;

        return Convert.ToInt32(mutator.multiplier);
    }


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
        tower.AddMutator(new RateSupportModel.RateSupportMutator(true, Id, tier, Priority, null));
        if (onUpgrade && Upgrades[tier - 1] is UpgradePlusPlus upgrade)
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
    public static PathPlusPlus? GetPath(string tower, int path) => path < 3
        ? PathsPlusPlusMod.ExtendedPathsByTower.TryGetValue(tower, out var array) ? array[path] : null
        : PathsPlusPlusMod.PathsByTower.TryGetValue(tower, out var list)
            ? list.FirstOrDefault(plus => plus.Path == path)
            : null;

    /// <summary>
    /// Tries to get the PathPlusPlus for a given tower and path number, or null if not found
    /// </summary>
    /// <param name="tower"></param>
    /// <param name="path"></param>
    /// <param name="pathPlusPlus"></param>
    /// <returns>Whether one was found</returns>
    public static bool TryGetPath(string tower, int path, out PathPlusPlus pathPlusPlus) =>
        (pathPlusPlus = GetPath(tower, path)!) != null;
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