using System.Collections.Generic;
using System.Reflection;
using EnhancedCarTuning.UI;
using HarmonyLib;
namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(MenuColorPicker),"Awake")]
    internal static class ColorPickerHexCreatePatch
    {private static void Postfix(MenuColorPicker __instance)=>ColorPickerHex.Attach(__instance);}
    [HarmonyPatch(typeof(MenuColorPicker),"UpSize")]
    internal static class ColorPickerHexSizePatch
    {private static void Postfix(MenuColorPicker __instance)=>__instance.GetComponent<ColorPickerHex>()?.ApplySize();}
    [HarmonyPatch]
    internal static class ColorPickerHexTypingPatch
    {
        private static IEnumerable<MethodBase> TargetMethods(){foreach(string name in new[]{"UpLeftRight","UpSV","UpOpacity"})yield return AccessTools.Method(typeof(MenuColorPicker),name);}
        private static bool Prefix(MenuColorPicker __instance)=>!(__instance.GetComponent<ColorPickerHex>()?.IsTyping??false);
    }
}
