using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using PathsPlusPlus;
using UnityEngine;

[assembly: MelonInfo(typeof(PathsPlusPlusMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace PathsPlusPlus;

/// <summary>
/// Main Paths++ mod class
/// </summary>
public class PathsPlusPlusMod : BloonsTD6Mod
{
    /// <summary>
    /// Map of ID to PathPlusPlus object
    /// </summary>
    public static readonly Dictionary<string, PathPlusPlus> PathsById = new();

    /// <summary>
    /// Map of Tower ID to list of all PathPlusPlus objects added
    /// </summary>
    public static readonly Dictionary<string, List<PathPlusPlus>> PathsByTower = new();

    /// <summary>
    /// Map of ID to UpgradePlusPlus object
    /// </summary>
    public static readonly Dictionary<string, UpgradePlusPlus> UpgradesById = new();

    /// <summary>
    /// Map of Tower ID to 3-length array of possibly null PathPlusPlus objects for extending vanilla paths
    /// </summary>
    public static readonly Dictionary<string, PathPlusPlus?[]> ExtendedPathsByTower = new();

    /// <summary>
    /// ModSetting to restrict Paths++ upgrading like vanilla upgrading
    /// </summary>
    public static readonly ModSettingBool BalancedMode = new(true)
    {
        description =
            "Restricts upgrading across all paths to be the standard 5 in one path, 2 in another, 0 in the rest. " +
            "When disabled, vanilla upgrade paths work as normal and you can additionally use any/all Paths++ upgrade.",
        icon = VanillaSprites.AchievementsIcon,
        button = true,
        enabledText = "On",
        disabledText = "Off"
    };

    /// <summary>
    /// ModSetting to affect default Paragon Upgrade overriding behavior
    /// </summary>
    public static readonly ModSettingBool ParagonOverlapDefault = new(true)
    {
        description =
            "When a Paragon Upgrade and a Paths++ upgrade on a tower overlap, changes which one is showed by default. " +
            "You'll always be able to right click the upgrade to toggle which is currently showing.",
        icon = VanillaSprites.UpgradeContainerParagonUnlocked,
        button = true,
        enabledText = "Paths++",
        enabledButton = VanillaSprites.BlueBtnLong,
        disabledText = "Paragon",
        disabledButton = VanillaSprites.ParagonBtnLong
    };

    #region Hotkeys

    internal static readonly ModSettingCategory Hotkeys = new("Hotkeys");

    internal static readonly ModSettingHotkey UpgradePath4 = new(KeyCode.Comma, HotkeyModifier.Shift)
    {
        category = Hotkeys,
        displayName = "Upgrade Fourth Path"
    };

    internal static readonly ModSettingHotkey UpgradePath5 = new(KeyCode.Period, HotkeyModifier.Shift)
    {
        category = Hotkeys,
        displayName = "Upgrade Fifth Path"
    };

    internal static readonly ModSettingHotkey UpgradePath6 = new(KeyCode.Slash, HotkeyModifier.Shift)
    {
        category = Hotkeys,
        displayName = "Upgrade Sixth Path"
    };

    internal static readonly ModSettingHotkey UpgradePath7 = new(KeyCode.Comma, HotkeyModifier.Ctrl)
    {
        category = Hotkeys,
        displayName = "Upgrade Seventh Path"
    };

    internal static readonly ModSettingHotkey UpgradePath8 = new(KeyCode.Period, HotkeyModifier.Ctrl)
    {
        category = Hotkeys,
        displayName = "Upgrade Eighth Path"
    };

    internal static readonly ModSettingHotkey UpgradePath9 = new(KeyCode.Slash, HotkeyModifier.Ctrl)
    {
        category = Hotkeys,
        displayName = "Upgrade Ninth Path"
    };

    internal static readonly Dictionary<int, ModSettingHotkey> HotKeysByPath = new()
    {
        { 3, UpgradePath4 },
        { 4, UpgradePath5 },
        { 5, UpgradePath6 },
        { 6, UpgradePath7 },
        { 7, UpgradePath8 },
        { 8, UpgradePath9 },
    };

    #endregion


    internal static MelonPreferences_Category Preferences { get; private set; } = null!;

    /// <inheritdoc />
    public override void OnApplicationStart()
    {
        Preferences = MelonPreferences.CreateCategory("PathsPlusPlusPreferences");
    }

    /// <inheritdoc />
    public override void OnNewGameModel(GameModel gameModel)
    {
        Clipboard.Clear();
        foreach (var towerDetails in gameModel.towerSet.OfIl2CppType<ShopTowerDetailsModel>())
        {
            if (ExtendedPathsByTower.TryGetValue(towerDetails.towerId, out var paths))
            {
                if (towerDetails.pathOneMax == 5 && paths[0] != null)
                {
                    towerDetails.pathOneMax = paths[0]!.UpgradeCount;
                }

                if (towerDetails.pathTwoMax == 5 && paths[1] != null)
                {
                    towerDetails.pathTwoMax = paths[1]!.UpgradeCount;
                }

                if (towerDetails.pathThreeMax == 5 && paths[2] != null)
                {
                    towerDetails.pathThreeMax = paths[2]!.UpgradeCount;
                }
            }
        }
    }


    /// <inheritdoc />
    public override void OnTowerSaved(Tower tower, TowerSaveDataModel saveData)
    {
        foreach (var pathId in PathsById.Keys)
        {
            var tier = tower.GetTier(pathId);
            if (tier > 0)
            {
                saveData.metaData[pathId] = tier.ToString();
            }
        }
    }

    /// <inheritdoc />
    public override void OnTowerLoaded(Tower tower, TowerSaveDataModel saveData)
    {
        foreach (var pathId in PathsById.Keys)
        {
            if (saveData.metaData.ContainsKey(pathId) && int.TryParse(saveData.metaData[pathId], out var tier))
            {
                tower.SetTier(pathId, tier);
            }
        }
    }

    /// <inheritdoc />
    public override void OnProfileLoaded(ProfileModel profile)
    {
        foreach (var upgrade in ModContent.GetContent<UpgradePlusPlus>())
        {
            profile.acquiredUpgrades.AddIfNotPresent(upgrade.Id);
        }
    }

    /// <inheritdoc />
    public override void PreCleanProfile(ProfileModel profile)
    {
        var ids = ModContent.GetContent<UpgradePlusPlus>().Select(upgrade => upgrade.Id).ToArray();
        profile.acquiredUpgrades.RemoveWhere(new Func<string, bool>(s => ids.Contains(s)));
    }

    /// <inheritdoc />
    public override void PostCleanProfile(ProfileModel profile) => OnProfileLoaded(profile);

    private static readonly Dictionary<PathPlusPlus, int> Clipboard = new();

    /// <inheritdoc />
    public override void OnTowerDestroyed(Tower tower)
    {
        foreach (var upgradePlusPlus in ModContent.GetContent<UpgradePlusPlus>())
        {
            upgradePlusPlus.currentlyAppliedOn.Remove(tower);
        }
    }

    /// <inheritdoc />
    public override void OnGameObjectsReset()
    {
        foreach (var upgradePlusPlus in ModContent.GetContent<UpgradePlusPlus>())
        {
            upgradePlusPlus.currentlyAppliedOn.Clear();
        }
    }

    /// <inheritdoc />
    public override object? Call(string operation, params object[] parameters)
    {
        switch (operation)
        {
            case "OnTowerCopied" when parameters.CheckTypes(out Tower towerCopied):
                Clipboard.Clear();
                foreach (var path in PathsById.Values)
                {
                    if (path.GetTier(towerCopied) is var tier and > 0)
                    {
                        Clipboard[path] = tier;
                    }
                }

                break;
            case "OnTowerPasted" when parameters.CheckTypes(out Tower towerPasted):
                foreach (var (path, tier) in Clipboard)
                {
                    path.SetTier(towerPasted, tier);
                }

                break;
            case "OnClipboardCleared":
                Clipboard.Clear();
                break;
            case "GetPathIds":
                return PathsById.Keys;
            case "GetTiers" when parameters.CheckTypes(out Tower tower):
                return tower.GetAllTiers();
            case "ValidTiers" when parameters.CheckTypes(out string towerId, out int path, out int[] tiers):
                return !PathPlusPlus.TryGetPath(towerId, path, out var p) || p.ValidTiers(tiers);
            case "GetUpgrade" when parameters.CheckTypes(out string towerId, out int path, out int tier):
                return PathPlusPlus.GetPath(towerId, path)?.Upgrades.GetValueOrDefault(tier)?.Id;
        }

        return base.Call(operation, parameters);
    }
}