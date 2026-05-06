using System;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.Upgrade;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PathsPlusPlus;

[RegisterTypeInIl2Cpp(false)]
internal class UpgradeScreenPlusPlus(IntPtr ptr) : MonoBehaviour(ptr)
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

    private float paragonOffset;

    public readonly List<GameObject> createdUpgradePaths = new();

    public readonly List<UpgradeDetails>[] extraUpgradeDetails =
    [
        new(),
        new(),
        new()
    ];

    public void UpdateUi(string towerId)
    {
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
            : [];

        var allExtendedPaths = PathsPlusPlusMod.ExtendedPathsByTower.TryGetValue(towerId, out var ps) ? ps : [];
        var currentExtendedPaths = new System.Collections.Generic.List<PathPlusPlus>();
        for (var p = 0; p <= 2; p++)
        {
            if (!allExtendedPaths.TryGetValue(p, out var paths)) continue;

            if (PathsPlusPlusMod.DefaultExtendedPaths.TryGetValue(towerId, out var defaults) &&
                defaults[p] is { } d &&
                PathsPlusPlusMod.PathsById.ContainsKey(d))
            {
                currentExtendedPaths.Add(paths.Single(plus => plus.Id == d));
            }
            else
            {
                foreach (var path in paths.Where(path => path.StartTier >= 5))
                {
                    currentExtendedPaths.Add(path);
                    break;
                }
            }
        }

        var allPaths = extraPaths.Concat(currentExtendedPaths).ToArray();

        var paragonPanel = upgradeScreen.paragonPanel.transform;

        foreach (var cycle in upgradeScreen.GetComponentsInChildren<Button>(true)
                     .Where(button => button.name == "Cycle"))
        {
            cycle.gameObject.SetActive(false);
        }

        var scrollRect = scrollPanel.ScrollRect;
        if (allPaths.Length == 0 && allExtendedPaths.Count == 0)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = false;
            TaskScheduler.ScheduleTask(() =>
            {
                scrollRect.SetHorizontalNormalizedPosition(0);
                scrollRect.SetVerticalNormalizedPosition(1);
            });

            paragonPanel.localPosition = Vector3.zero;
            paragonOffset = 0;
            return;
        }

        var maxUpgrades = allPaths.Length == 0 ? 5 : allPaths.Max(path => path!.UpgradeCount);
        var minForScrolling = upgradeScreen.ShowParagonPanel() ? 6 : 7;

        scrollRect.horizontal = maxUpgrades >= minForScrolling;
        scrollRect.vertical = extraPaths.Any();

        TaskScheduler.ScheduleTask(() =>
        {
            if (!scrollRect.horizontal) scrollRect.SetHorizontalNormalizedPosition(0);
            if (!scrollRect.vertical) scrollRect.SetVerticalNormalizedPosition(1);
        });


        var before = upgradePaths.localPosition;
        var deltaY = 0f;
        var deltaX = 0f;

        for (var path = 0; path < extraPaths.Count; path++)
        {
            var pathPlusPlus = extraPaths[path];
            deltaY += PathsSpacing;
            deltaX = Math.Max(deltaX, (pathPlusPlus.UpgradeCount - 5) * UpgradeSpacing);

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

        foreach (var extendedPath in currentExtendedPaths)
        {
            deltaX = Math.Max(deltaX, (extendedPath!.UpgradeCount - 5) * UpgradeSpacing);

            var upgrades = GetUpgrades(extendedPath.ExtendVanillaPath);
            var extraUpgrades = extraUpgradeDetails[extendedPath.ExtendVanillaPath];
            var container = GetContainer(extendedPath.ExtendVanillaPath);

            foreach (var upgradePlusPlus in extendedPath.Upgrades.Values)
            {
                try
                {
                    if (upgradePlusPlus.Tier > upgrades.Count + extraUpgrades.Count)
                    {
                        var newUpgradeDetails = Instantiate(upgrades.Last(), container.transform);
                        newUpgradeDetails.gameObject.name = "Upgrade(Clone)";
                        extraUpgrades.Add(newUpgradeDetails);
                    }
                    var combinedUpgrades = upgrades.Concat(extraUpgrades.ToArray()).ToArray();

                    var upgrade = combinedUpgrades[upgradePlusPlus.Tier - 1];

                    SetUpgrade(upgrade, upgradePlusPlus);
                }
                catch (Exception e)
                {
                    ModHelper.Warning<PathsPlusPlusMod>(e);
                }
            }


            upgradeScreen.ResetUpgradeUnlocks(
                extraUpgrades.Where(details => details.gameObject.active).ToIl2CppReferenceArray(), null);

            foreach (var upgradeDetails in extraUpgrades)
            {
                upgradeDetails.SetUpgradeScreen(upgradeScreen);
            }
        }

        for (var p = 0; p <= 2; p++)
        {
            if (!allExtendedPaths.TryGetValue(p, out var paths)) continue;

            var upgrades = GetUpgrades(p).Concat(extraUpgradeDetails[p].ToArray()).ToArray();

            var showingPath = PathsPlusPlusMod.DefaultExtendedPaths.TryGetValue(towerId, out var defaultPaths)
                ? defaultPaths[p]
                : null;

            for (var tier = 0; tier <= 5; tier++)
            {
                var currentPaths = paths.Where(plus => plus.StartTier == tier).Select(path => path.Id).ToList();
                if (currentPaths.Count <= (tier == 5 ? 1 : 0)) continue;

                var upgrade = upgrades[tier];

                var cycle = upgrade.transform.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(button => button.name == "Cycle");

                if (cycle == null)
                {
                    var button = ModHelperButton.Create(new Info("Cycle", 150, 150, new Vector2(0, 0.5f)),
                        VanillaSprites.RestartIcon, null);
                    button.transform.Rotate(0, 0, 180);
                    button.SetParent(upgrade.transform);
                    cycle = button.Button;
                }

                cycle.gameObject.SetActive(true);
                var finalP = p;
                var finalTier = tier;
                cycle.SetOnClick(() => CycleExtendedPath(towerId, finalP, finalTier, upgrade));

                if (showingPath != null && currentPaths.Contains(showingPath))
                {
                    break;
                }
            }
        }


        paragonOffset = deltaX;

        scrollContent.sizeDelta = new Vector2(deltaX, deltaY);
        upgradePaths.localPosition = before;
    }

    private Il2CppReferenceArray<UpgradeDetails> GetUpgrades(int path) => path switch
    {
        PathPlusPlus.Top => upgradeScreen.path1Upgrades,
        PathPlusPlus.Middle => upgradeScreen.path2Upgrades,
        PathPlusPlus.Bottom => upgradeScreen.path3Upgrades,
        _ => throw new IndexOutOfRangeException()
    };

    private GameObject GetContainer(int path) => path switch
    {
        PathPlusPlus.Top => upgradeScreen.path1Container,
        PathPlusPlus.Middle => upgradeScreen.path2Container,
        PathPlusPlus.Bottom => upgradeScreen.path3Container,
        _ => throw new IndexOutOfRangeException()
    };

    private static void SetUpgrade(UpgradeDetails upgrade, UpgradePlusPlus upgradePlusPlus)
    {
        upgrade.gameObject.SetActive(true);

        upgrade.SetUpgrade(upgrade.baseTowerID, upgradePlusPlus.GetUpgradeModel(),
            new List<AbilityModel>().Cast<ICollection<AbilityModel>>(), upgradePlusPlus.Path.Path,
            upgradePlusPlus.PortraitReference);

        upgrade.icon.gameObject.SetActive(true);
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
    private void CycleExtendedPath(string towerId, int path, int tier, UpgradeDetails upgradeDetails)
    {
        MenuManager.instance.buttonClick3Sound.Play();

        var before = scrollPanel.ScrollContent.transform.localPosition;

        var current = PathsPlusPlusMod.DefaultExtendedPaths.TryGetValue(towerId, out var os) ? os[path] : null;

        var paths = PathsPlusPlusMod.ExtendedPathsByTower[towerId][path].Where(p => p.StartTier == tier).ToList();
        var index = paths.FindIndex(p => p.Id == current);
        if (tier >= 5 && index < 0) index = 0;
        index++;

        var newPath = index >= paths.Count ? null : paths[index];

        if (!PathsPlusPlusMod.DefaultExtendedPaths.TryGetValue(towerId, out var overrides))
            overrides = PathsPlusPlusMod.DefaultExtendedPaths[towerId] = new string?[3];

        overrides[path] = newPath?.Id;

        var selected = upgradeScreen.selectedDetails;

        upgradeScreen.PopulatePaths(Game.instance.model.GetTower(towerId), false);
        UpdateUi(towerId);

        scrollPanel.ScrollContent.transform.localPosition = before;

        upgradeDetails.OnPointerExit(null);
        upgradeDetails.OnPointerEnter(null);

        if (selected != null && selected.gameObject.activeInHierarchy)
        {
            upgradeScreen.SelectUpgrade(selected);
        }
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

    private static string lastDefaults = null!;

    [HarmonyPatch(typeof(UpgradeScreen), nameof(UpgradeScreen.Open))]
    internal static class UpgradeScreen_Open
    {
        [HarmonyPostfix]
        private static void Postfix(UpgradeScreen __instance)
        {
            lastDefaults = JsonConvert.SerializeObject(PathsPlusPlusMod.DefaultExtendedPaths);

            foreach (var (_, defaults) in PathsPlusPlusMod.DefaultExtendedPaths)
            {
                for (var p = 0; p < defaults.Length; p++)
                {
                    if (defaults[p] is { } pathId && !PathsPlusPlusMod.PathsById.ContainsKey(pathId))
                    {
                        defaults[p] = null;
                    }
                }
            }

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

            if (JsonConvert.SerializeObject(PathsPlusPlusMod.DefaultExtendedPaths) != lastDefaults)
            {
                ModContent.GetInstance<PathsPlusPlusMod>().SaveModSettings();
            }
        }
    }
}