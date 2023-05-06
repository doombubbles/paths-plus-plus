using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;

namespace PathsPlusPlus;

/// <summary>
/// Class to register your path, defining what tower its for and how many upgrades you're adding
/// </summary>
public abstract class PathPlusPlus : ModContent
{
    /// <inheritdoc />
    protected sealed override float RegistrationPriority => base.RegistrationPriority;

    /// <summary>
    /// The tower id this path is for, use <code>TowerType.XXX</code>
    /// </summary>
    public abstract string Tower { get; }

    /// <summary>
    /// How many upgrades this path has
    /// </summary>
    public abstract int UpgradeCount { get; }

    /// <summary>
    /// Which internal path id this PathPlusPlus will have. Will be 3 or higher since the vanilla paths are 0,1,2
    /// </summary>
    public int Path { get; private set; }

    private UpgradePlusPlus[]? upgrades;

    /// <summary>
    /// The UpgradePlusPlus(s) for this path
    /// </summary>
    public UpgradePlusPlus[] Upgrades => upgrades ??= new UpgradePlusPlus[UpgradeCount];

    /// <summary>
    /// Whether this path should appear in the Upgrades Screen for the tower
    /// </summary>
    public virtual bool ShowInMenu => true;

    /// <inheritdoc />
    public override void Register()
    {
        PathsPlusPlusMod.PathsById[Id] = this;

        if (!PathsPlusPlusMod.PathsByTower.TryGetValue(Tower, out var list))
            list = PathsPlusPlusMod.PathsByTower[Tower] = new List<PathPlusPlus>();
        Path = 3 + list.Count;
        list.Add(this);
    }

    internal int Priority => Order - 100;

    /// <summary>
    /// Applies all upgrades for this path up through the given tier on a TowerModel.
    /// </summary>
    /// <param name="tower">TowerModel to apply to</param>
    /// <param name="tier">Up to and including this tier number</param>
    public void Apply(TowerModel tower, int tier)
    {
        tower.tier = Math.Max(tower.tier, tier);
        for (var i = 0; i < tier; i++)
        {
            var upgrade = Upgrades[i];
            upgrade.ApplyUpgrade(tower);
            upgrade.ApplyUpgrade(tower, tier);
            if (upgrade.IsHighestUpgrade(tower))
            {
                tower.portrait = upgrade.PortraitReference;
            }

            tower.appliedUpgrades = tower.appliedUpgrades.AddTo(upgrade.Id);
        }
    }

    /// <summary>
    /// Gets the PathPlusPlus for a given tower and path number, or null if not found
    /// </summary>
    /// <param name="tower">The tower id</param>
    /// <param name="path">The path number</param>
    /// <returns>PathPlusPlus or null</returns>
    public static PathPlusPlus? GetPath(string tower, int path) =>
        PathsPlusPlusMod.PathsByTower.TryGetValue(tower, out var paths)
            ? paths.FirstOrDefault(plus => plus.Path == path)
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