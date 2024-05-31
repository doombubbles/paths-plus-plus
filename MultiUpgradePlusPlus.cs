using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Extensions;

namespace PathsPlusPlus;

/// <summary>
/// An UpgradePlusPlus that is for multiple towers due to a MultiPathPlusPlus
/// </summary>
public abstract class MultiUpgradePlusPlus : UpgradePlusPlus
{
    /// <inheritdoc />
    public override string Name => base.Name + "-" + tower;

    private string tower = null!;
    
    /// <summary>
    /// The tower that this instance of the MultuUpgradePlusPlus is specifically for
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public string Tower =>
        tower ?? throw new ArgumentException($"Tower was null for MultiUpgradePlusPlus {Name}");

    /// <inheritdoc />
    public sealed override MultiPathPlusPlus Path => BasePath.PathsByTower[Tower];

    private protected abstract MultiPathPlusPlus BasePath { get; }

    /// <inheritdoc />
    public override IEnumerable<ModContent> Load() => BasePath.Towers.Select(t =>
    {
        var upgrade = (MultiUpgradePlusPlus) Activator.CreateInstance(GetType())!;
        upgrade.tower = t;
        return upgrade;
    });


    /// <inheritdoc />
    public override string Icon => GetType().Name;

    /// <inheritdoc />
    public override string DisplayName => GetType().Name.Spaced();
}

/// <inheritdoc />
public abstract class MultiUpgradePlusPlus<T> : MultiUpgradePlusPlus where T : MultiPathPlusPlus
{
    /// <inheritdoc />
    private protected sealed override MultiPathPlusPlus BasePath => GetInstance<T>();
}