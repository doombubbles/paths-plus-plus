using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using MelonLoader;
using PathsPlusPlus;
using UnityEngine;
[assembly: MelonInfo(typeof(PathsPlusPlusMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace PathsPlusPlus;

internal class PathsPlusPlusMod : BloonsTD6Mod
{
    public static readonly Dictionary<string, PathPlusPlus> PathsById = new();
    public static readonly Dictionary<string, List<PathPlusPlus>> PathsByTower = new();
    public static readonly Dictionary<string, UpgradePlusPlus> UpgradesById = new();

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
        {3, UpgradePath4},
        {4, UpgradePath5},
        {5, UpgradePath6},
        {6, UpgradePath7},
        {7, UpgradePath8},
        {8, UpgradePath9},
    };

    internal static bool ValidTiers(List<int> tiers) =>
        ModHelper.HasMod("UltimateCrosspathing") || tiers.Count(i => i > 2) <= 1 && tiers.Count(i => i > 0) <= 2;


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

    public override void OnProfileLoaded(ProfileModel profile)
    {
        foreach (var upgrade in ModContent.GetContent<UpgradePlusPlus>())
        {
            profile.acquiredUpgrades.AddIfNotPresent(upgrade.Id);
        }
    }

    public override void PreCleanProfile(ProfileModel profile)
    {
        var ids = ModContent.GetContent<UpgradePlusPlus>().Select(upgrade => upgrade.Id).ToArray();
        profile.acquiredUpgrades.RemoveWhere(new Func<string, bool>(s => ids.Contains(s)));
    }

    public override void PostCleanProfile(ProfileModel profile) => OnProfileLoaded(profile);
}