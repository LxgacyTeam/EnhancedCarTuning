using EnhancedCarTuning.UI;
using HarmonyLib;
using UnityEngine;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarGarageUI), "GetText")]
    internal static class GarageTitlePatch
    {
        private static bool Prefix(CarGarageUI __instance, ref string __result)
        {
            var panel = __instance.GetComponent<GarageTuningPanel>();
            string title = panel ? panel.NativeTitle : null;
            if (title == null) return true;
            __result = title;
            return false;
        }
    }

    [HarmonyPatch(typeof(CarGarageUI), "Init")]
    internal static class GarageInitPatch
    {
        private static void Postfix(CarGarageUI __instance)
        {
            var panel = __instance.GetComponent<GarageTuningPanel>() ?? __instance.gameObject.AddComponent<GarageTuningPanel>();
            panel.Initialize(__instance);
        }
    }

    [HarmonyPatch(typeof(CarGarageUI_Buy), "AddNotOwnWinils")]
    internal static class GarageItemsPatch
    {
        private static void Postfix(CarGarageUI_Buy __instance, MenuBuy buy)
        {
            var panel = GarageTuningPanel.Find(__instance);
            if (panel) { panel.Session.AddPurchaseItems(buy); panel.BodyPartsSession?.AddPurchaseItems(buy);panel.BodyPaintSession?.AddPurchaseItems(buy); panel.PolicePaintSession?.AddPurchaseItems(buy); panel.GetComponent<EnhancedCarTuning.Appearance.Glass.GlassGaragePrototype>()?.AddPurchaseItems(buy); }
        }
    }

    [HarmonyPatch(typeof(CarGarageUI_Buy), "ShowBuy")]
    internal static class GarageBuyPatch
    {
        private static void Prefix(CarGarageUI_Buy __instance)
        { var panel = GarageTuningPanel.Find(__instance); if (panel) { panel.FlushVinyls(); panel.FlushBodyPaint(); } }
        private static void Postfix(CarGarageUI_Buy __instance, bool __result)
        {
            if (__result) return;
            var panel = GarageTuningPanel.Find(__instance);
            if (panel) { panel.BodyPartsSession?.WrapPurchase();panel.PolicePaintSession?.WrapPurchase(); panel.BodyPaintSession?.WrapPurchase(); panel.Session.WrapPurchase(); panel.GetComponent<EnhancedCarTuning.Appearance.Glass.GlassGaragePrototype>()?.WrapPurchase(); }
        }
    }

    [HarmonyPatch(typeof(CarGarageUI), "UpPaint")]
    internal static class GaragePointerPatch
    {
        private static bool Prefix(CarGarageUI __instance)
        {
            var panel = __instance.GetComponent<GarageTuningPanel>();
            if (panel && panel.IsBodyPaint) return false;
            if (panel && panel.ContainsPointer() && Input.GetMouseButtonDown(0)) __instance.isMousePressOnUI = true;
            // UpPaint also copies the picker color and redraws the vinyl texture.
            // Keep that live while the pointer is over any of our controls.
            return true;
        }
    }

    [HarmonyPatch(typeof(CarGarageUI), "RayCastAlign")]
    internal static class GarageVinylAlignPointerPatch
    {
        private static bool Prefix(CarGarageUI __instance)
        {
            var panel = __instance.GetComponent<GarageTuningPanel>();
            // Only placement must stop over UI; the native raycast ignores uGUI.
            return !panel || !panel.ContainsPointer();
        }
    }
}
