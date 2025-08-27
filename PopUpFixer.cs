using System;
using BTD_Mod_Helper;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using MelonLoader;
using UnityEngine;

namespace PathsPlusPlus;

[RegisterTypeInIl2Cpp(false)]
internal class PopUpFixer(IntPtr ptr) : MonoBehaviour(ptr)
{
    public GameObject upgradeObj = null!;

    private void LateUpdate()
    {
        try
        {
            var popup = GetComponent<UpgradeInfoPopup>();
            if (popup == null || upgradeObj == null) return;
            var t = popup.transform;
            t.position = t.position with
            {
                y = upgradeObj.transform.position.y
            };
        }
        catch (Exception e)
        {
            ModHelper.Warning<PathsPlusPlusMod>(e);
        }
    }
}