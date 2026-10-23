using EnhancedCarTuning.Appearance.Vinyls;
using HarmonyLib;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarPaintItems), "AddItem")]
    internal static class TextVinylDefaultSizePatch
    {
        private static void Postfix(CarPaintSprites.SpriteInfo setSprite, CarPaintItems.Item __result)
        { if (__result != null && setSprite != null && TextVinylData.IsText(setSprite.texutreName)) __result.size_y = .2f; }
    }
}
