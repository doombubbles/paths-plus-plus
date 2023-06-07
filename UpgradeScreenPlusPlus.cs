using System;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Unity.UI_New.Upgrade;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PathsPlusPlus;

[RegisterTypeInIl2Cpp(false)]
internal class UpgradeScreenPlusPlus : MonoBehaviour
{
    private const int TopBarHeight = 345;
    private const int PathsSpacing = 525;

    public UpgradeScreen upgradeScreen = null!;
    public ModHelperScrollPanel scrollPanel = null!;
    public RectTransform upgradePaths = null!;
    public RectTransform scrollContent = null!;

    public readonly List<GameObject> createdUpgradePaths;

    public UpgradeScreenPlusPlus(IntPtr ptr) : base(ptr)
    {
        createdUpgradePaths = new List<GameObject>();
    }

    public void UpdateUi(string towerId)
    {
        scrollPanel.ScrollRect.normalizedPosition = new Vector2(0, 1);

        foreach (var createdUpgradePath in createdUpgradePaths)
        {
            createdUpgradePath.gameObject.SetActive(false);
        }

        var paths = PathsPlusPlusMod.PathsByTower.TryGetValue(towerId, out var allPaths)
            ? allPaths.Where(path => path.ShowInMenu).ToList()
            : new System.Collections.Generic.List<PathPlusPlus>();

        if (paths.Count == 0)
        {
            scrollPanel.ScrollRect.enabled = false;
            return;
        }

        scrollPanel.ScrollRect.enabled = true;
        var y = upgradePaths.localPosition.y;
        var delta = 0f;
        for (var path = 0; path < paths.Count; path++)
        {
            var pathPlusPlus = paths[path];
            delta += PathsSpacing;

            GameObject upgradePath;
            if (path >= createdUpgradePaths.Count)
            {
                upgradePath = Instantiate(upgradeScreen.path3Container, upgradePaths);
                upgradePath.name = $"Path {pathPlusPlus.Path + 1} Upgrades";
                upgradePath.transform.TranslateScaled(new Vector3(0, -delta, 0));

                for (var i = 0; i < upgradePath.transform.childCount; i++)
                {
                    var animator = upgradePath.transform.GetChild(i).gameObject.AddComponent<Animator>();
                    animator.runtimeAnimatorController = Animations.PopupAnim;
                    animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                }

                createdUpgradePaths.Add(upgradePath);
            }
            else
            {
                upgradePath = createdUpgradePaths.Get(path);
            }

            upgradePath.SetActive(true);
            var upgradeDetails = upgradePath.GetComponentsInChildren<UpgradeDetails>();

            for (var i = 0; i < pathPlusPlus.Upgrades.Length; i++)
            {
                try
                {
                    var upgrade = upgradeDetails[i];
                    upgrade.gameObject.SetActive(true);
                    var upgradePlusPlus = pathPlusPlus.Upgrades[i];
                    if (upgradePlusPlus == null) continue;
                    
                    
                    upgrade.SetUpgrade(towerId, upgradePlusPlus.GetUpgradeModel(),
                        new List<AbilityModel>().Cast<ICollection<AbilityModel>>(), pathPlusPlus.Path,
                        upgradePlusPlus.PortraitReference);
                    
                    upgrade.abilityObject.SetActive(upgradePlusPlus.Ability);
                }
                catch (Exception e)
                {
                    ModHelper.Warning<PathsPlusPlusMod>(e);
                }
            }

            for (var i = pathPlusPlus.Upgrades.Length; i < 5; i++)
            {
                upgradeDetails[i].gameObject.SetActive(false);
            }

            upgradeScreen.ResetUpgradeUnlocks(
                upgradeDetails.Take(pathPlusPlus.Upgrades.Length).ToIl2CppReferenceArray(), null);
        }

        scrollContent.sizeDelta = scrollContent.sizeDelta with { y = delta };
        upgradePaths.localPosition = upgradePaths.localPosition with { y = y };
    }

    public static UpgradeScreenPlusPlus Setup(UpgradeScreen upgradeScreen)
    {
        var upgradeScreenPlusPlus = upgradeScreen.gameObject.AddComponent<UpgradeScreenPlusPlus>();
        upgradeScreenPlusPlus.upgradeScreen = upgradeScreen;
        var scrollPanel = upgradeScreenPlusPlus.scrollPanel =
            upgradeScreen.gameObject.AddModHelperScrollPanel(new Info("UpgradesScroll", InfoPreset.FillParent), null);
        scrollPanel.GetComponent<Mask>().showMaskGraphic = false;
        scrollPanel.AddComponent<Image>();
        scrollPanel.ScrollRect.horizontal = false;
        scrollPanel.ScrollContent.AddComponent<Text>();
        var scrollContent = upgradeScreenPlusPlus.scrollContent =
            scrollPanel.ScrollContent.transform.Cast<RectTransform>();
        scrollContent.anchorMin = Vector2.zero;
        scrollContent.anchorMax = Vector2.one;

        var upgradePaths = upgradeScreenPlusPlus.upgradePaths = upgradeScreen
            .GetComponentFromChildrenByName<LayoutElement>("UpgradePaths").transform
            .Cast<RectTransform>();
        upgradePaths.SetParent(scrollPanel.ScrollContent.transform);
        upgradePaths.sizeDelta = Vector2.zero;
        scrollContent.pivot = new Vector2(0.5f, 0.5f);
        upgradePaths.localPosition = new Vector3(280, 0, 0);
        scrollContent.pivot = new Vector2(0.5f, 1);

        var y = upgradePaths.localPosition.y;
        scrollPanel.RectTransform.pivot = new Vector2(0.5f, 0);
        scrollPanel.RectTransform.sizeDelta = new Vector2(0, -TopBarHeight);
        upgradePaths.localPosition = upgradePaths.localPosition with { y = y + TopBarHeight };

        scrollPanel.transform.SetSiblingIndex(4);

        return upgradeScreenPlusPlus;
    }

    [HarmonyPatch(typeof(UpgradeScreen), nameof(UpgradeScreen.UpdateUi))]
    internal static class UpgradeScreen_UpdateUi
    {
        [HarmonyPostfix]
        private static void Postfix(UpgradeScreen __instance)
        {
            var upgradeScreenPlusPlus = __instance.GetComponentInChildren<UpgradeScreenPlusPlus>();
            if (upgradeScreenPlusPlus == null)
            {
                upgradeScreenPlusPlus = Setup(__instance);
            }

            upgradeScreenPlusPlus.UpdateUi(__instance.currTowerId);
        }
    }

    [HarmonyPatch(typeof(UpgradeScreen), nameof(UpgradeScreen.Open))]
    internal static class UpgradeScreen_Open
    {
        [HarmonyPostfix]
        private static void Postfix(UpgradeScreen __instance)
        {
            foreach (var upgradeDetails in __instance.GetComponentsInChildren<UpgradeDetails>())
            {
                upgradeDetails.SetUpgradeScreen(__instance);
                if (upgradeDetails.gameObject.HasComponent(out Animator animator))
                {
                    animator.speed = .75f;
                    animator.Play("PopupScaleIn");
                }

                upgradeDetails.GetComponentInChildren<EventTrigger>(true).enabled = false;
            }
        }
    }

    [HarmonyPatch(typeof(UpgradeScreen), nameof(UpgradeScreen.Close))]
    internal static class UpgradeScreen_Close
    {
        [HarmonyPostfix]
        private static void Postfix(UpgradeScreen __instance)
        {
            foreach (var upgradeDetails in __instance.GetComponentsInChildren<UpgradeDetails>())
            {
                if (upgradeDetails.gameObject.HasComponent(out Animator animator))
                {
                    animator.speed = 1f;
                    animator.Play("PopupScaleOut");
                }
            }
        }
    }
}