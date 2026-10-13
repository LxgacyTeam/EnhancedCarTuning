using EnhancedCarTuning.UI;
using HarmonyLib;
using UnityEngine;

namespace EnhancedCarTuning.Patches
{
    // The garage preview uses Menu_RotCamUI, not Cam.turn. Its native exclusion
    // check runs before mouse/touch capture and keeps UI-origin gestures excluded
    // until release, even when the pointer leaves the panel or slider.
    [HarmonyPatch(typeof(Menu_RotCamIgnore), "isIntersect", new[] { typeof(Vector2) })]
    internal static class GarageCameraPatch
    {
        private static void Postfix(Vector2 mouseOrTouchPos, ref bool __result)
        {
            if (__result) return;
            var garage = CarGarageUI.me;
            if (!garage || !garage.gameObject.activeInHierarchy || !garage.top_panel ||
                garage.top_panel.cur != garage || (MenuBuy.me && MenuBuy.me.gameObject.activeInHierarchy &&
                (!MenuBuy.me.root || MenuBuy.me.root.activeInHierarchy))) return;
            var panel = garage.GetComponent<GarageTuningPanel>();
            if (panel && panel.IsCameraInputArea(mouseOrTouchPos)) __result = true;
        }
    }
}
