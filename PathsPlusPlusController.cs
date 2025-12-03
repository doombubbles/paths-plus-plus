using System;
using System.Linq;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppNinjaKiwi.Common;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace PathsPlusPlus;

[RegisterTypeInIl2Cpp(false)]
internal class PathsPlusPlusController : MonoBehaviour
{
    public TowerSelectionMenu menu = null!;
    public ScrollRect scrollRect = null!;
    public List<UpgradeObjectPlusPlus> moreUpgradeButtons = null!;

    public PathsPlusPlusController(IntPtr ptr) : base(ptr)
    {
    }

    private void Update()
    {
        if (menu.selectedTower == null || PopupScreen.instance.IsPopupActive()) return;

        foreach (var (path, hotkey) in PathsPlusPlusMod.HotKeysByPath)
        {
            if (!hotkey.JustPressed() || path >= menu.upgradeButtons.Length) continue;

            var button = menu.upgradeButtons[path];

            if (button == null || !button.isActiveAndEnabled) continue;

            var tier = button.tier;
            button.OnUpgrade();
            if (button.tier > tier)
            {
                button.WobbleUpgrade();
            }
        }
    }


    public static PathsPlusPlusController? Create(TowerSelectionMenu menu)
    {
        var towerDetails = menu.towerDetails;
        var selectedTowerOptions = towerDetails.transform.parent.gameObject;
        if (selectedTowerOptions == null)
        {
            return null;
        }

        var controller = selectedTowerOptions.AddComponent<PathsPlusPlusController>();
        controller.menu = menu;
        controller.moreUpgradeButtons = new List<UpgradeObjectPlusPlus>();

        var mask = selectedTowerOptions.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        selectedTowerOptions.AddComponent<Image>();
        var scrollRect = controller.scrollRect = selectedTowerOptions.AddComponent<ScrollRect>();
        var content = towerDetails.transform.Cast<RectTransform>();
        content.pivot = new Vector2(0.5f, 1f);
        scrollRect.content = content;
        var viewport = selectedTowerOptions.transform.Cast<RectTransform>();
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 165;
        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        foreach (var menuUpgradeButton in menu.upgradeButtons)
        {
            ModifyDefaultUpgradeObject(menuUpgradeButton);
        }

        var verticalLayoutGroup = towerDetails.AddComponent<VerticalLayoutGroup>();
        verticalLayoutGroup.childForceExpandHeight = false;
        verticalLayoutGroup.childForceExpandWidth = false;
        verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        verticalLayoutGroup.padding = new RectOffset {bottom = 25, top = 25};
        verticalLayoutGroup.spacing = 15;

        var contentSizeFitter = towerDetails.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (var i = 0; i < menu.upgradeInfoPopups.Count; i++)
        {
            var menuUpgradeInfoPopup = menu.upgradeInfoPopups[i]!;
            var popUpFixer = menuUpgradeInfoPopup.gameObject.AddComponent<PopUpFixer>();
            popUpFixer.upgradeObj = menu.upgradeButtons[i]!.gameObject;
        }

        return controller;
    }

    public void SetMode(bool hasMorePaths)
    {
        var towerDetails = menu.towerDetails;
        var selectedTowerOptions = towerDetails.transform.parent.gameObject;
        var viewport = selectedTowerOptions.transform.Cast<RectTransform>();
        viewport.sizeDelta = new Vector2(-50, hasMorePaths ? -1046 : -1106);
        scrollRect.vertical = hasMorePaths;
        scrollRect.verticalNormalizedPosition = 1f;
        towerDetails.GetComponent<VerticalLayoutGroup>().spacing = hasMorePaths ? 15 : 20;
        selectedTowerOptions.GetComponent<Mask>().enabled = hasMorePaths;
        selectedTowerOptions.GetComponent<Image>().enabled = hasMorePaths;

        TaskScheduler.ScheduleTask(() =>
        {
            for (var i = 3; i < menu.upgradeInfoPopups.Count; i++)
            {
                var infoContainer = menu.upgradeInfoPopups[i]!.transform.parent;
                var pos = infoContainer.localPosition;
                infoContainer.localPosition = pos with
                {
                    x = Math.Abs(pos.x) * (menu.IsOpenedRight() ? 1 : -1)
                };
            }
        });
    }

    private void AddNewUpgradeObject()
    {
        var upgradeObject = menu.upgradeButtons[0]!;
        var row = 3 + moreUpgradeButtons.Count;
        var newUpgrade = upgradeObject.gameObject.Duplicate(menu.towerDetails.transform);

        var upgradeObjectPlusPlus = newUpgrade.GetComponent<UpgradeObjectPlusPlus>();

        upgradeObjectPlusPlus.upgradeObject = newUpgrade.GetComponent<UpgradeObject>();
        newUpgrade.name = "UpgradeObject_" + (row + 1);
        var upgradeObj = upgradeObjectPlusPlus.upgradeObject;
        upgradeObj.upgradeButton.row = row;
        upgradeObj.currentUpgrade.row = row;
        upgradeObj.buttonID = row;
        upgradeObj.upgradeButton.background = upgradeObj.upgradeButton.GetComponent<Image>();
        moreUpgradeButtons.Add(upgradeObjectPlusPlus);
        menu.upgradeButtons = menu.upgradeButtons.AddTo(upgradeObj);


        var basePopup = menu.upgradeInfoPopups[0]!.transform.parent.parent;
        var newPopupObject = basePopup.Duplicate(basePopup.transform.parent);
        newPopupObject.name = "Upgrade_" + (row + 1);
        var newPopUp = newPopupObject.GetComponentInChildren<UpgradeInfoPopup>();
        var popUpFixer = newPopUp.gameObject.GetComponentOrAdd<PopUpFixer>();
        popUpFixer.upgradeObj = newUpgrade;
        menu.upgradeInfoPopups = menu.upgradeInfoPopups.AddTo(newPopUp);

        scrollRect.verticalNormalizedPosition = 1;
    }

    private static void ModifyDefaultUpgradeObject(UpgradeObject upgradeObject)
    {
        // Slightly adjust size
        var layoutElement = upgradeObject.gameObject.AddComponent<LayoutElement>();
        var size = upgradeObject.transform.Cast<RectTransform>().sizeDelta;
        layoutElement.preferredWidth = layoutElement.minWidth = size.x;
        layoutElement.preferredHeight = layoutElement.minHeight = size.y;
        upgradeObject.locked.transform.Cast<RectTransform>().sizeDelta = new Vector2(900, 350);

        // Allow any number of tiers
        var tierIndicators = upgradeObject.tiers[0].transform.parent;
        tierIndicators.TranslateScaled(new Vector3(13, 0, 0));
        var grid = tierIndicators.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        grid.constraintCount = 5;
        grid.startAxis = GridLayoutGroup.Axis.Vertical;
        grid.childAlignment = TextAnchor.LowerLeft;
        grid.spacing = new Vector2(4, 4);

        // Add custom component
        var upgradeObjectPlusPlus = upgradeObject.gameObject.AddComponent<UpgradeObjectPlusPlus>();
        upgradeObjectPlusPlus.upgradeObject = upgradeObject;
    }

    public void InitUpgradeButtons()
    {
        if (menu.selectedTower is not {hero: null, tower.towerModel.isParagon: false} tower) return;

        if (!PathsPlusPlusMod.PathsByTower.TryGetValue(tower.Def.baseId, out var list)) list = [];

        while (list.Count > moreUpgradeButtons.Count)
        {
            AddNewUpgradeObject();
        }

        if (PathsPlusPlusMod.ExtendedPathsByTower.TryGetValue(tower.Def.baseId, out var array))
        {
            for (var p = 0; p < array.Length; p++)
            {
                var upgradeButton = menu.upgradeButtons[p]!;
                var button = upgradeButton.GetComponent<UpgradeObjectPlusPlus>();
                if (button == null || !button.isActiveAndEnabled) continue;

                var path = array[p];
                if (path == null)
                {
                    button.pathId = null;
                }
                else
                {
                    button.InitForTower(tower, path.Id);
                    upgradeButton.UpdateVisuals(path.Path, false);
                }
            }
        }

        var popups = menu.transform.GetComponentsInChildren<PopUpFixer>(true);

        for (var i = 0; i < moreUpgradeButtons.Count; i++)
        {
            var button = moreUpgradeButtons.Get(i);
            var popup = popups.First(fixer => fixer.upgradeObj == button.upgradeObject.gameObject)
                .GetComponent<UpgradeInfoPopup>();

            if (i < list.Count)
            {
                var path = list[i];
                button.gameObject.SetActive(true);
                button.upgradeObject.enabled = true;
                button.InitForTower(tower, path.Id);

                popup.enabled = true;
                popup.gameObject.SetActive(true);
            }
            else
            {
                button.upgradeObject.tts = null;
                button.upgradeObject.enabled = false;
                button.gameObject.SetActive(false);

                popup.enabled = false;
                popup.gameObject.SetActive(false);
            }
        }

        menu.upgradeButtons = menu.transform.GetComponentsInChildren<UpgradeObject>(false).ToArray();
        menu.upgradeInfoPopups = menu.transform.GetComponentsInChildren<UpgradeInfoPopup>(false).ToArray();

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (menu.selectedTower is not {hero: null, tower.towerModel.isParagon: false}) return;

        foreach (var upgradeObjectPlusPlus in moreUpgradeButtons)
        {
            if (upgradeObjectPlusPlus.gameObject.active)
            {
                upgradeObjectPlusPlus.upgradeObject.UpdateVisuals(upgradeObjectPlusPlus.upgradeObject.path, false);
            }
        }
    }

    public void UpdateCosts()
    {
        if (menu.selectedTower is not {hero: null, tower.towerModel.isParagon: false}) return;

        foreach (var upgradeObjectPlusPlus in moreUpgradeButtons)
        {
            if (upgradeObjectPlusPlus.gameObject.active)
            {
                upgradeObjectPlusPlus.upgradeObject.UpdateCost();
            }
        }

        CheckCash();
    }

    public void CheckCash()
    {
        if (menu.selectedTower is not {hero: null, tower.towerModel.isParagon: false}) return;

        foreach (var upgradeObjectPlusPlus in moreUpgradeButtons)
        {
            if (upgradeObjectPlusPlus.gameObject.active)
            {
                upgradeObjectPlusPlus.upgradeObject.CheckCash();
            }
        }
    }
}