using System.Xml;
using EnhancedCarTuning.Appearance.Glass;
using HarmonyLib;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarPaintItems), "SaveLoad")]
    internal static class GlassPrototypeSavePatch
    {
        private static void Prefix(CarPaintItems __instance, bool isSave)
        {
            if (isSave) return;
            __instance.items.RemoveAll(GlassPrototypeState.IsGlass);
            __instance.GetComponent<GlassPrototypeState>()?.Reset();
        }
        private static void Postfix(CarPaintItems __instance, XmlElement _el, bool isSave)
        { if (isSave) GlassPrototypeXml.Write(__instance, _el); else GlassPrototypeXml.Read(__instance, _el); }
    }
    [HarmonyPatch(typeof(CarPaintItems.Item), "Clone")]
    internal static class GlassPrototypeItemClonePatch
    {
        private static void Postfix(CarPaintItems.Item __instance, CarPaintItems.Item __result)
        { GlassPrototypeState.SetGlass(__result, GlassPrototypeState.IsGlass(__instance)); }
    }
    [HarmonyPatch(typeof(CarPaintItems), "Clone")]
    internal static class GlassPrototypeClonePatch
    {
        private static bool Prefix(CarPaintItems __instance, CarPaintItems.Item of, ref CarPaintItems.Item __result)
        {
            if (!GlassPrototypeState.IsGlass(of) || GlassPrototypeState.Ensure(__instance).GlassCount < 32) return true;
            __result = of; Plugin.Warn("Glass prototype supports at most 32 decals per vehicle."); return false;
        }
        private static void Postfix(CarPaintItems.Item of, CarPaintItems.Item __result)
        { GlassPrototypeState.SetGlass(__result, GlassPrototypeState.IsGlass(of)); }
    }
    [HarmonyPatch(typeof(CarPaintItems.Item), "isHaveChanges")]
    internal static class GlassPrototypeChangesPatch
    {
        private static void Postfix(CarPaintItems.Item __instance, CarPaintItems.Item w, ref bool __result)
        { __result |= GlassPrototypeState.IsGlass(__instance) != GlassPrototypeState.IsGlass(w); }
    }
}
