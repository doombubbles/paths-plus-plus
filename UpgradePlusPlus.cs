﻿using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Utils;

namespace PathsPlusPlus;

/// <summary>
/// Class to define an upgrade for your PathPlusPlus
/// </summary>
public abstract class UpgradePlusPlus : NamedModContent
{
    /// <inheritdoc />
    protected sealed override float RegistrationPriority => 10f;

    /// <summary>
    /// The PathPlusPlus this upgrade is a part of
    /// </summary>
    public abstract PathPlusPlus Path { get; }

    /// <summary>
    /// Name of the icon, either the name of a png within your mod or a VanillaSprites string
    /// </summary>
    public virtual string Icon => Name;

    /// <summary>
    /// Sprite reference for the icon
    /// </summary>
    public virtual SpriteReference IconReference => GetSpriteReferenceOrDefault(Icon);

    /// <summary>
    /// The base cost of this upgrade (medium difficulty)
    /// </summary>
    public abstract int Cost { get; }

    /// <summary>
    /// Whether this upgrade adds an Ability to the tower
    /// </summary>
    public virtual bool Ability => false;

    /// <summary>
    /// What tier this upgrade is, from 1 to 5
    /// </summary>
    public abstract int Tier { get; }

    /// <summary>
    /// Name of the icon, either the name of a png within your mod or a VanillaSprites string
    /// </summary>
    public virtual string? Portrait => null;

    /// <summary>
    /// Sprite reference for the portrait
    /// </summary>
    public virtual SpriteReference PortraitReference => string.IsNullOrEmpty(Portrait)
        ? GetSpriteReferenceOrNull(Path.Name) ??
          CreateSpriteReference(VanillaSprites.ByName.GetValueOrDefault(Path.Tower.Replace(TowerType.WizardMonkey, "Wizard") + "000"))
        : GetSpriteReferenceOrDefault(Portrait);

    private UpgradeModel? upgradeModel;

    /// <inheritdoc />
    public override void Register()
    {
        PathsPlusPlusMod.UpgradesById[Id] = this;
        Path.Upgrades[Tier - 1] = this;

        Game.instance.model.AddUpgrade(GetUpgradeModel());
    }


    /// <summary>
    /// Gets or constructs the UpgradeModel for this UpgradePlusPlus
    /// </summary>
    /// <returns></returns>
    public UpgradeModel GetUpgradeModel() =>
        upgradeModel ??= new UpgradeModel(Id, Cost, 0, IconReference, Path.Path, Tier - 1, 0, "", "");

    /// <summary>
    /// Returns whether this Upgrade is of a higher tier than any other base or PathsPlusPlus upgrade that the tower has
    /// </summary>
    /// <param name="towerModel">The TowerModel</param>
    /// <param name="highestOrEqual">Whether to still return true if this is tied for being the highest tier upgrade</param>
    /// <returns></returns>
    public bool IsHighestUpgrade(TowerModel towerModel, bool highestOrEqual = false) =>
        towerModel.GetAllTiers().All(t => highestOrEqual ? Tier >= t : Tier > t);

    /// <summary>
    /// The effects on the TowerModel that obtaining this upgrade should have.
    /// </summary>
    /// <param name="towerModel">The tower to apply to</param>
    public virtual void ApplyUpgrade(TowerModel towerModel)
    {
    }

    /// <summary>
    /// The effects on the TowerModel that obtaining this upgrade should have.
    /// <br/>
    /// The tier parameter is mostly useful for skipping applying any effects that would be overriden by later upgrades
    /// in this path
    /// </summary>
    /// <param name="towerModel">The tower to apply to</param>
    /// <param name="tier">The total tier that this tower has in this Upgrade Path</param>
    public virtual void ApplyUpgrade(TowerModel towerModel, int tier)
    {
    }
}

/// <inheritdoc />
public abstract class UpgradePlusPlus<T> : UpgradePlusPlus where T : PathPlusPlus
{
    /// <inheritdoc />
    public sealed override PathPlusPlus Path => GetInstance<T>();
}