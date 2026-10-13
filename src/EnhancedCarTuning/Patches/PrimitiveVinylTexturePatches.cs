using EnhancedCarTuning.Appearance.Vinyls;
using HarmonyLib;
using UnityEngine;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarPaintSprites.SpriteInfo), "get_texture")]
    internal static class PrimitiveVinylTexturePatch
    {
        private static bool Prefix(CarPaintSprites.SpriteInfo __instance, ref Texture __result)
        {
            if (!PrimitiveVinylTextures.TryTexture(__instance, out Texture2D replacement)) return true;
            __result = replacement;
            return false;
        }
    }

    [HarmonyPatch(typeof(CarPaintSprites.SpriteInfo), "isLoaded")]
    internal static class PrimitiveVinylLoadedPatch
    {
        private static bool Prefix(CarPaintSprites.SpriteInfo __instance, ref bool __result)
        {
            if (!PrimitiveVinylTextures.TryTexture(__instance, out Texture2D replacement)) return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(CarPaintSprite), "SetSprite")]
    internal static class PrimitiveCatalogIconPatch
    {
        private static void Postfix(CarPaintSprite __instance, CarPaintSprites.SpriteInfo setSP)
        {
            if (!__instance || !PrimitiveVinylTextures.TryIcon(setSP, out Sprite icon)) return;
            if (__instance.image)
            {
                __instance.image.sprite = icon;
                __instance.image.preserveAspect = true;
                __instance.image.gameObject.SetActive(true);
            }
            if (__instance.rawImage) __instance.rawImage.gameObject.SetActive(false);
        }
    }
}
