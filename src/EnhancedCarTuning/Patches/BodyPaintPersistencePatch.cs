using System.Xml;
using EnhancedCarTuning.Appearance.Body;
using EnhancedCarTuning.Appearance.Police;
using HarmonyLib;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarMaterial), "SaveLoad")]
    internal static class BodyPaintSavePatch
    {
        private static void Prefix(CarMaterial __instance, bool isSave)
        {
            var state = __instance.GetComponent<BodyPaintState>();
            if (!state) return;
            if (isSave) state.ApplyBase(state.SerializePreview ? state.Active : state.Committed);
            else state.ResetForPool(); // A previous Body override must not alter the native XML being loaded.
        }
        private static void Postfix(CarMaterial __instance, XmlElement _el, bool isSave)
        {
            var police = PolicePaintState.Ensure(__instance);
            if (isSave) PolicePaintXml.Write(_el, police.SerializePreview ? police.Active : police.Committed,
                police.SerializePreview ? police.SirenMode : police.CommittedSirenMode);
            else if (!VinylPortBridge.IsApplyingPaint(__instance.car ? __instance.car : __instance.GetComponent<CarControl>()))
            {
                if (!PolicePaintXml.TryRead(_el, out bool enabled, out int mode)) Plugin.Warn("Invalid police paint XML; using ordinary paint.");
                police.Commit(enabled, mode);
            }
            var state = BodyPaintState.Ensure(__instance);
            if (isSave) { BodyPaintXml.Write(_el, state.SerializePreview ? state.Active : state.Committed); return; }
            if (!BodyPaintXml.TryRead(_el, out var settings)) Plugin.Warn("Invalid body paint XML; using the native body color.");
            state.Commit(settings); state.Renderer?.ScanNow(); state.Invalidate();
        }
    }
}
