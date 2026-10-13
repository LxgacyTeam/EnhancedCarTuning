using EnhancedCarTuning.Appearance.Vinyls;
using HarmonyLib;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarPaintItems), "UnloadRt")]
    internal static class VinylTextureQualityPatch
    {
        private static bool Prefix(CarPaintItems __instance) => !VinylRenderTextures.Release(__instance);
    }
}
