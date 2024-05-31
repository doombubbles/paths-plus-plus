using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api;

namespace PathsPlusPlus;

/// <summary>
/// A PathPlusPlus class that can apply to multiple base towers
/// </summary>
public abstract class MultiPathPlusPlus : PathPlusPlus
{
    /// <inheritdoc />
    public override string Name => base.Name + "-" + Tower;

    private string tower = null!;

    /// <inheritdoc />
    public sealed override string Tower =>
        tower ?? throw new ArgumentException($"Tower was null for MultiPathPlusPlus {Name}");

    /// <summary>
    /// The multiple towers that this will be added as a path for
    /// </summary>
    public abstract IEnumerable<string> Towers { get; }

    /// <summary>
    /// The generated MultiPathPlusPlus instances for each tower
    /// </summary>
    public readonly Dictionary<string, MultiPathPlusPlus> PathsByTower = new();

    /// <inheritdoc />
    public override IEnumerable<ModContent> Load() => Towers.Select(t =>
    {
        var path = (MultiPathPlusPlus) Activator.CreateInstance(GetType())!;
        path.tower = t;
        PathsByTower[t] = path;
        return path;
    });
}