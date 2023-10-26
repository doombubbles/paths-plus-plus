using System;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.Menu;
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

    public bool overrideParagon = PathsPlusPlusMod.ParagonOverlapDefault;

    public ModHelperButton? cycle;

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
        var tier = upgradeObject.tier = tts.tower.GetTier(path);

        try
        {
            if (IsExtra)
            {
                upgradeObject.LoadUpgrades();
            }
            
            if (tts.CanUpgradeToParagon(true) && tier >= 5 && tier < pathPlusPlus.UpgradeCount)
            {
                if (cycle == null)
                {
                    cycle = ModHelperButton.Create(new Info("Cycle", 100, 100, new Vector2(0.55f, 0.5f)),
                        VanillaSprites.RetryIcon, null);
                    cycle.SetParent(upgradeObject.transform);
                }

                cycle.gameObject.SetActive(true);
                cycle.Button.SetOnClick(CycleParagon);
            }
            else if (cycle != null)
            {
                cycle.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            ModHelper.Warning<PathsPlusPlusMod>(e);
        }
    }

    /// <summary>
    /// Whether the current UpgradeObject will be showing an upgrade beyond what it could with just Vanilla ones
    /// </summary>
    public bool IsExtra
    {
        get
        {
            if (upgradeObject.path >= 3) return true;

            if (upgradeObject.tier < 5) return false;

            var tower = upgradeObject.towerSelectionMenu.selectedTower;
            var hasExtraPath = PathPlusPlus.TryGetPath(tower.Def.baseId, upgradeObject.path, out _);

            return hasExtraPath;
        }
    }

    public void CycleParagon()
    {
        if (!IsExtra) return;

        overrideParagon = !overrideParagon;
        upgradeObject.LoadUpgrades();
        MenuManager.instance.buttonClick3Sound.Play();
    }
}