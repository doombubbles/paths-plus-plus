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
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public static readonly Dictionary<string, PathPlusPlus> PathsById = [];

    /// <summary>
    /// Map of Tower ID to list of all PathPlusPlus objects added
    /// </summary>
    public static readonly Dictionary<string, List<PathPlusPlus>> PathsByTower = [];

    /// <summary>
    /// Map of ID to UpgradePlusPlus object
    /// </summary>
    public static readonly Dictionary<string, UpgradePlusPlus> UpgradesById = [];

    /// <summary>
    /// Map of Tower Id to Path index to list of PathsPlusPlus objects
    /// </summary>
    public static readonly Dictionary<string, Dictionary<int, List<PathPlusPlus>>> ExtendedPathsByTower = [];

    /// <summary>
    /// Map of Tower ID to 3-length array of possibly null PathPlusPlus ids
    /// </summary>
    public static readonly Dictionary<string, string?[]> DefaultExtendedPaths = [];

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
                if (towerDetails.pathOneMax == 5 && paths.TryGetValue(0, out var topPaths))
                {
                    towerDetails.pathOneMax = topPaths.Max(p => p.UpgradeCount);
                }

                if (towerDetails.pathTwoMax == 5 && paths.TryGetValue(1, out var middlePaths))
                {
                    towerDetails.pathTwoMax = middlePaths.Max(p => p.UpgradeCount);
                }

                if (towerDetails.pathThreeMax == 5 && paths.TryGetValue(2, out var bottomPaths))
                {
                    towerDetails.pathThreeMax = bottomPaths.Max(p => p.UpgradeCount);
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
    public override void OnSaveSettings(JObject settings)
    {
        settings[nameof(DefaultExtendedPaths)] = JObject.FromObject(DefaultExtendedPaths);
    }

    /// <inheritdoc />
    public override void OnLoadSettings(JObject settings)
    {
        var dict = settings[nameof(DefaultExtendedPaths)]?.ToObject<Dictionary<string, string?[]>>();
        if (dict != null)
        {
            foreach (var (key, value) in dict)
            {
                DefaultExtendedPaths[key] = value;
            }
        }
    }

    /// <inheritdoc />
    public override void OnTowerCreated(Tower tower, Entity target, Model modelToUse)
    {
        if (DefaultExtendedPaths.TryGetValue(tower.towerModel.baseId, out var defaults))
        {
            foreach (var path in defaults.Where(pathId => pathId != null && PathsById.ContainsKey(pathId))
                         .Select(pathId => PathsById[pathId!]))
            {
                path.SetTier(tower, -1);
            }
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
            case "OnTowerPasted" when parameters.CheckTypes(out Tower towerPasted, out bool isQueued):
                foreach (var (path, tier) in Clipboard)
                {
                    path.SetTier(towerPasted, isQueued ? -1 : tier);
                }

                break;
            case "OnClipboardCleared":
                Clipboard.Clear();
                break;
            case "GetPathIds":
                return PathsById.Keys;
            case "GetTiers" when parameters.CheckTypes(out Tower tower):
                return tower.GetAllTiers();
#pragma warning disable CS0618 // Type or member is obsolete
            case "ValidTiers" when parameters.CheckTypes(out string towerId, out int path, out int[] tiers):
                return !PathPlusPlus.TryGetPath(towerId, path, out var p) || p.ValidTiers(tiers);
            case "GetUpgrade" when parameters.CheckTypes(out string towerId, out int path, out int tier):
                return PathPlusPlus.GetPath(towerId, path)?.Upgrades.GetValueOrDefault(tier)?.Id;
#pragma warning restore CS0618 // Type or member is obsolete
            case "GetUpgrade" when parameters.CheckTypes(out Tower tower, out int path, out int tier):
                return PathPlusPlus.GetPath(tower, path)?.Upgrades.GetValueOrDefault(tier)?.Id;
            case "ValidTiers" when parameters.CheckTypes(out Tower tower, out int path, out int[] tiers):
                return !PathPlusPlus.TryGetPath(tower, path, out var ps) || ps.ValidTiers(tiers);
        }

        return base.Call(operation, parameters);
    }
}