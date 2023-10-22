using System.Linq;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Towers;
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
    public virtual SpriteReference? PortraitReference => string.IsNullOrEmpty(Portrait)
        ? GetSpriteReferenceOrNull(Path.Name)
        : GetSpriteReferenceOrDefault(Portrait);

    /// <summary>
    /// Override the texture to use for the container for the upgrade in the upgrades screen
    /// </summary>
    public virtual string? Container => null;

    /// <summary>
    /// Sprite reference for the container
    /// </summary>
    public virtual SpriteReference? ContainerReference =>
        string.IsNullOrEmpty(Container) ? null : GetSpriteReferenceOrDefault(Container);

    /// <summary>
    /// Whether this upgrade requires a confirmation popup
    /// </summary>
    public virtual bool NeedsConfirmation => false;

    /// <summary>
    /// The title for the confirmation popup, if needed
    /// </summary>
    public virtual string? ConfirmationTitle => null;

    /// <summary>
    /// The body text for the confirmation popup, if needed
    /// </summary>
    public virtual string? ConfirmationBody => null;

    private UpgradeModel? upgradeModel;

    /// <summary>
    /// A Platinum version of the Tier 5 Upgrade Container
    /// </summary>
    protected static string UpgradeContainerPlatinum => GetTextureGUID<PathsPlusPlusMod>("UpgradeContainerPlatinum");

    /// <summary>
    /// A Diamond version of the Tier 5 Upgrade Container
    /// </summary>
    protected static string UpgradeContainerDiamond => GetTextureGUID<PathsPlusPlusMod>("UpgradeContainerDiamond");

    /// <summary>
    /// A Rainbow version of the Tier 5 Upgrade Container
    /// </summary>
    protected static string UpgradeContainerRainbow => GetTextureGUID<PathsPlusPlusMod>("UpgradeContainerRainbow");

    /// <inheritdoc />
    public override void Register()
    {
        PathsPlusPlusMod.UpgradesById[Id] = this;
        Path.Upgrades[Tier - 1] = this;

        Game.instance.model.AddUpgrade(GetUpgradeModel());
    }

    /// <inheritdoc />
    public override void RegisterText(Il2CppSystem.Collections.Generic.Dictionary<string, string> textTable)
    {
        base.RegisterText(textTable);
        if (NeedsConfirmation)
        {
            if (ConfirmationTitle != null)
            {
                textTable[Id + " Title"] = ConfirmationTitle;
            }

            if (ConfirmationBody != null)
            {
                textTable[Id + " Body"] = ConfirmationBody;
            }
        }
    }


    /// <summary>
    /// Gets or constructs the UpgradeModel for this UpgradePlusPlus
    /// </summary>
    /// <returns></returns>
    public UpgradeModel GetUpgradeModel() => upgradeModel ??= new UpgradeModel(Id, Cost, 0, IconReference, Path.Path,
        Tier - 1, 0, NeedsConfirmation ? Id : "", "");

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

    /// <summary>
    /// Runs in game when this upgrade is applied to a Tower for the first time
    /// </summary>
    /// <param name="tower">The tower being upgraded</param>
    public virtual void OnUpgraded(Tower tower)
    {
        
    }
}

/// <inheritdoc />
public abstract class UpgradePlusPlus<T> : UpgradePlusPlus where T : PathPlusPlus
{
    /// <inheritdoc />
    public sealed override PathPlusPlus Path => GetInstance<T>();
}