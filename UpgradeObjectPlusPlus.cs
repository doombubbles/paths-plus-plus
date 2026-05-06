using System;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace PathsPlusPlus;

[RegisterTypeInIl2Cpp(false)]
internal class UpgradeObjectPlusPlus(IntPtr ptr) : MonoBehaviour(ptr)
{
    public UpgradeObject upgradeObject = null!;

    public string? pathId;

    public bool overrideParagon; // whether upgradeplusplus on this object should show even if paragon available

    public ModHelperButton? cycle;

    public Il2CppStringArray? cyclePathIds;

    public void InitForTower(TowerToSimulation tts, string path)
    {
        var pathPlusPlus = PathsPlusPlusMod.PathsById[path];

        pathId = pathPlusPlus.Id;

        upgradeObject.path = pathPlusPlus.Path;
        upgradeObject.tts = tts;
        upgradeObject.upgradeButton.tts = tts;
        if (tts.tower.GetTier(path) is var tier && tier > upgradeObject.tier)
        {
            upgradeObject.tier = tier;
        }

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

    public void CycleShowing(bool showing, Il2CppStringArray pathIds)
    {
        cyclePathIds = pathIds;
        if (showing)
        {
            if (cycle == null)
            {
                cycle = ModHelperButton.Create(new Info("Cycle", 150, 150, new Vector2(0.55f, 0.5f)),
                    VanillaSprites.RestartIcon, null);
                cycle.transform.Rotate(0, 0, 180);
                cycle.SetParent(upgradeObject.transform);
            }

            cycle.gameObject.SetActive(true);
            cycle.Button.SetOnClick(CycleUpgrade);
        }
        else if (cycle != null)
        {
            cycle.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Whether the current UpgradeObject will be showing an upgrade beyond what it could with just Vanilla ones
    /// </summary>
    public bool IsExtra =>
        upgradeObject.path >= 3 ||
        PathPlusPlus.TryGetPath(upgradeObject.towerSelectionMenu.selectedTower.tower, upgradeObject.path,
            upgradeObject.tier + 1, out var p) &&
        upgradeObject.tier >= p.StartTier;

    public void CycleUpgrade()
    {
        if (cyclePathIds == null || !cyclePathIds.Any() || cycle == null) return;

        MenuManager.instance.buttonClick3Sound.Play();

        var paths = cyclePathIds.Select(s => PathsPlusPlusMod.PathsById[s]).ToList();

        var tier = upgradeObject.tier;
        var tts = upgradeObject.tts;
        var path = upgradeObject.path;

        var paragonInvolved = tts.CanUpgradeToParagon(true) && tier >= 5;

        PathPlusPlus? nextPath;

        var showingPath = PathPlusPlus.GetPath(tts.tower, path, tier + 1);
        if (!overrideParagon && paragonInvolved)
        {
            overrideParagon = true;
            nextPath = paths.First();
        }
        else if (overrideParagon && showingPath == paths.Last() && paragonInvolved)
        {
            overrideParagon = false;
            nextPath = null;
        }
        else if (showingPath == null)
        {
            nextPath = paths.First();
        }
        else
        {
            var index = paths.IndexOf(showingPath) + 1;
            nextPath = index >= paths.Count ? null : paths[index];
        }

        foreach (var p in paths)
        {
            tts.tower.RemoveMutatorsById(p.Id);
        }

        nextPath?.SetTier(tts.tower, -1);

        pathId = nextPath?.Id;
        upgradeObject.LoadUpgrades();
    }

    private int delay;

    private void Update()
    {
        if (delay > 0)
        {
            delay--;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            delay = 30;
            CycleUpgrade();
        }
    }
}