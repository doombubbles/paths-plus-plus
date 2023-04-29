using System;
using BTD_Mod_Helper;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using MelonLoader;
using UnityEngine;

namespace PathsPlusPlus;

[RegisterTypeInIl2Cpp(false)]
internal class PopUpFixer : MonoBehaviour
{
    public PopUpFixer(IntPtr ptr) : base(ptr)
    {
    }

    private void LateUpdate()
    {
        try
        {
            var popup = GetComponent<UpgradeInfoPopup>();
            if (popup == null) return;
            var t = popup.transform;
            t.position = t.position with
            {
                y = popup.upgradeObj.transform.position.y
            };
        }
        catch (Exception e)
        {
            ModHelper.Warning<PathsPlusPlusMod>(e);
        }
    }
}