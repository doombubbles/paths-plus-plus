using System;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.Upgrade;
using Il2CppInterop.Runtime.Attributes;
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
    private const int LeftSideWidth16X9 = 1240;
    private const int LeftSideWidth16X10 = 1010;
    private const int LeftSideWidth4X3 = 849;
    private const int PathsSpacing = 525;
    private const int UpgradeSpacing = 490;

    public UpgradeScreen upgradeScreen = null!;
    public ModHelperScrollPanel scrollPanel = null!;
    public RectTransform upgradePaths = null!;
    public RectTransform scrollContent = null!;

    public readonly List<GameObject> createdUpgradePaths;

    public readonly List<UpgradeDetails>[] extraUpgradeDetails;

    private float paragonOffset;

    public UpgradeScreenPlusPlus(IntPtr ptr) : base(ptr)
    {
        createdUpgradePaths = new List<GameObject>();
        extraUpgradeDetails = new List<UpgradeDetails>[]
        {
            new(),
            new(),
            new(),
        };
    }

    public void UpdateUi(string towerId)
    {
        scrollPanel.ScrollRect.normalizedPosition = new Vector2(0, 1);

        foreach (var createdUpgradePath in createdUpgradePaths)
        {
            createdUpgradePath.gameObject.SetActive(false);
        }

        foreach (var upgradeDetails in extraUpgradeDetails.SelectMany(list => list.ToArray()))
        {
            upgradeDetails.gameObject.SetActive(false);
        }

        var extraPaths = PathsPlusPlusMod.PathsByTower.TryGetValue(towerId, out var somePaths)
            ? somePaths.Where(path => path is { ShowInMenu: true }).ToList()
            : new System.Collections.Generic.List<PathPlusPlus>();

        var extendedPaths = PathsPlusPlusMod.ExtendedPathsByTower.TryGetValue(towerId, out var morePaths)
            ? morePaths.Where(path => path is { ShowInMenu: true }).ToList()!
            : new System.Collections.Generic.List<PathPlusPlus>();

        var allPaths = extraPaths.Concat(extendedPaths).ToArray();

        var paragonPanel = upgradeScreen.paragonPanel.transform;

        if (allPaths.Length == 0)
        {
            scrollPanel.ScrollRect.enabled = false;
            paragonPanel.localPosition = Vector3.zero;
            paragonOffset = 0;
            return;
        }

        var maxUpgrades = allPaths.Max(path => path.UpgradeCount);
        var minForScrolling = upgradeScreen.ShowParagonPanel() ? 6 : 7;

        scrollPanel.ScrollRect.enabled = true;
        scrollPanel.ScrollRect.horizontal = maxUpgrades >= minForScrolling;
        scrollPanel.ScrollRect.vertical = extraPaths.Any();


        var before = upgradePaths.localPosition;
        var deltaY = 0f;
        for (var path = 0; path < extraPaths.Count; path++)
        {
            var pathPlusPlus = extraPaths[path];
            deltaY += PathsSpacing;

            GameObject upgradePath;
            if (path >= createdUpgradePaths.Count)
            {
                upgradePath = Instantiate(upgradeScreen.path3Container, upgradePaths);
                upgradePath.name = $"Path {pathPlusPlus.Path + 1} Upgrades";
                upgradePath.transform.TranslateScaled(new Vector3(0, -deltaY, 0));

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

            for (var i = 0; i < pathPlusPlus.Upgrades.Count; i++)
            {
                try
                {
                    if (i >= upgradeDetails.Length)
                    {
                        var newUpgradeDetails = Instantiate(upgradeDetails[i - 1], upgradePath.transform);
                        newUpgradeDetails.gameObject.name = "Upgrade(Clone)";
                        upgradeDetails = upgradePath.GetComponentsInChildren<UpgradeDetails>();
                    }

                    var upgrade = upgradeDetails[i];
                    var upgradePlusPlus = pathPlusPlus.Upgrades[i];
                    if (upgradePlusPlus == null) continue;

                    SetUpgrade(upgrade, upgradePlusPlus);
                }
                catch (Exception e)
                {
                    ModHelper.Warning<PathsPlusPlusMod>(e);
                }
            }

            for (var i = pathPlusPlus.Upgrades.Count; i < upgradeDetails.Length; i++)
            {
                upgradeDetails[i].gameObject.SetActive(false);
            }

            upgradeScreen.ResetUpgradeUnlocks(
                upgradeDetails.Take(pathPlusPlus.Upgrades.Count).ToIl2CppReferenceArray(), null);

            foreach (var upgradeDetail in upgradeDetails)
            {
                upgradeDetail.SetUpgradeScreen(upgradeScreen);
            }
        }

        var deltaX = 0f;
        foreach (var extendedPath in extendedPaths)
        {
            var multiple = ModContent.GetContent<PathPlusPlus>().Any(path =>
                path != extendedPath && path.Tower == extendedPath.Tower && path.Path == extendedPath.Path);

            var extraUpgrades = extraUpgradeDetails[extendedPath.ExtendVanillaPath];
            var upgrades = extendedPath.ExtendVanillaPath switch
            {
                PathPlusPlus.Top => upgradeScreen.path1Upgrades,
                PathPlusPlus.Middle => upgradeScreen.path3Upgrades,
                PathPlusPlus.Bottom => upgradeScreen.path3Upgrades,
                _ => throw new IndexOutOfRangeException()
            };

            var container = extendedPath.ExtendVanillaPath switch
            {
                PathPlusPlus.Top => upgradeScreen.path1Container,
                PathPlusPlus.Middle => upgradeScreen.path2Container,
                PathPlusPlus.Bottom => upgradeScreen.path3Container,
                _ => throw new IndexOutOfRangeException()
            };

            foreach (var upgradePlusPlus in extendedPath.Upgrades.Values)
            {
                try
                {
                    var index = upgradePlusPlus.Tier - 6;

                    if (index >= extraUpgrades.Count)
                    {
                        var newUpgradeDetails = Instantiate(upgrades.Last(), container.transform);
                        newUpgradeDetails.gameObject.name = "Upgrade(Clone)";
                        extraUpgrades.Add(newUpgradeDetails);
                    }

                    var upgrade = extraUpgrades.Get(index);

                    SetUpgrade(upgrade, upgradePlusPlus);

                    var cycle = upgrade.transform.GetComponentsInChildren<Button>(true)
                        .FirstOrDefault(button => button.name == "Cycle");

                    if (multiple && InGame.instance == null)
                    {
                        if (cycle == null)
                        {
                            var button = ModHelperButton.Create(new Info("Cycle", 100, 100, new Vector2(0, 0.5f)),
                                VanillaSprites.RetryIcon, null);
                            button.SetParent(upgrade.transform);
                            cycle = button.Button;
                        }

                        cycle.gameObject.SetActive(true);
                        cycle.SetOnClick(() => CycleExtendedPath(extendedPath, upgrade));
                        multiple = false;
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

            deltaX = Math.Max(deltaX, (extendedPath.UpgradeCount - 5) * UpgradeSpacing);

            upgradeScreen.ResetUpgradeUnlocks(
                extraUpgrades.Where(details => details.gameObject.active).ToIl2CppReferenceArray(), null);

            foreach (var upgradeDetails in extraUpgrades)
            {
                upgradeDetails.SetUpgradeScreen(upgradeScreen);
            }
        }

        paragonOffset = deltaX;

        scrollContent.sizeDelta = new Vector2(deltaX, deltaY);
        upgradePaths.localPosition = before;
    }

    private static void SetUpgrade(UpgradeDetails upgrade, UpgradePlusPlus upgradePlusPlus)
    {
        upgrade.gameObject.SetActive(true);

        upgrade.SetUpgrade(upgradePlusPlus.Path.Tower, upgradePlusPlus.GetUpgradeModel(),
            new List<AbilityModel>().Cast<ICollection<AbilityModel>>(), upgradePlusPlus.Path.Path,
            upgradePlusPlus.PortraitReference ??
            ModContent.CreateSpriteReference(
                VanillaSprites.ByName.TryGetValue(
                    upgradePlusPlus.Path.Tower.Replace(TowerType.WizardMonkey, "Wizard") + "000", out var guid)
                    ? guid
                    : ""));

        upgrade.abilityObject.SetActive(upgradePlusPlus.Ability);

        var theme = upgradePlusPlus.Tier >= 5 ? upgrade.tier5Theme : upgrade.standardTheme;
        upgrade.theme = theme.MemberwiseClone().Cast<UpgradeDetails.UpgradeDetailsTheme>();
        if (upgradePlusPlus.ContainerReference is not null)
        {
            upgrade.theme.owned = upgrade.theme.paragonPurchased = upgradePlusPlus.ContainerReference;
        }
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
        scrollContent.pivot = new Vector2(0, 1);

        var rect = upgradeScreen.transform.Cast<RectTransform>().rect;
        var aspectRatio = rect.width / rect.height;
        var leftSideWidth = aspectRatio switch
        {
            < 1.5f => LeftSideWidth4X3,
            < 1.7f => LeftSideWidth16X10,
            _ => LeftSideWidth16X9
        };

        var before = upgradePaths.localPosition;
        scrollPanel.RectTransform.pivot = new Vector2(1, 0);
        scrollPanel.RectTransform.sizeDelta = new Vector2(-leftSideWidth, -TopBarHeight);
        upgradePaths.localPosition = before + new Vector3(-leftSideWidth, TopBarHeight, 0);

        scrollPanel.transform.SetSiblingIndex(4);

        return upgradeScreenPlusPlus;
    }

    private void LateUpdate()
    {
        var paragonPanel = upgradeScreen.paragonPanel.transform.Cast<RectTransform>();

        paragonPanel.anchoredPosition = paragonPanel.anchoredPosition with
        {
            x = scrollContent.anchoredPosition.x + paragonOffset
        };
    }

    [HideFromIl2Cpp]
    private void CycleExtendedPath(PathPlusPlus extendedPath, UpgradeDetails upgradeDetails)
    {
        MenuManager.instance.buttonClick3Sound.Play();

        var allPaths = ModContent.GetContent<PathPlusPlus>()
            .Where(path => path.Tower == extendedPath.Tower && path.Path == extendedPath.Path)
            .ToList();
        var currentIndex = allPaths.IndexOf(extendedPath);
        var nextPath = allPaths[(currentIndex + 1) % allPaths.Count];

        foreach (var path in allPaths)
        {
            path.Override.Value = path == nextPath;
        }

        nextPath.Register();

        PathsPlusPlusMod.Preferences.SaveToFile(false);

        var before = scrollPanel.ScrollContent.transform.localPosition;

        UpdateUi(extendedPath.Tower);

        scrollPanel.ScrollContent.transform.localPosition = before;

        upgradeDetails.OnPointerExit(null);
        upgradeDetails.OnPointerEnter(null);
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