﻿using System;
using BTD_Mod_Helper;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using MelonLoader;
using UnityEngine;

namespace PathsPlusPlus;

[RegisterTypeInIl2Cpp(false)]
internal class UpgradeObjectPlusPlus : MonoBehaviour
{
    public UpgradeObject upgradeObject = null!;

    public string? pathId;

    public bool getLowerUpgrade;

    public UpgradeObjectPlusPlus(IntPtr ptr) : base(ptr)
    {
    }

    public void InitForTower(TowerToSimulation tts, string path)
    {
        var pathPlusPlus = PathsPlusPlusMod.PathsById[path];

        pathId = pathPlusPlus.Id;

        upgradeObject.path = pathPlusPlus.Path;
        upgradeObject.tts = tts;
        upgradeObject.upgradeButton.tts = tts;
        upgradeObject.tier = tts.tower.GetTier(path);

        try
        {
            if (IsExtra)
            {
                upgradeObject.LoadUpgrades();
            }
        }
        catch (Exception e)
        {
            ModHelper.Warning<PathsPlusPlusMod>(e);
        }
    }

    public bool IsExtra => upgradeObject.path >= 3 || upgradeObject.tier >= 5;
}