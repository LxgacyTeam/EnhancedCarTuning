using EnhancedCarTuning.UI;
using HarmonyLib;

namespace EnhancedCarTuning.Patches
{
    // Only our two submenus change toggle semantics. All vanilla MenuButtonGroups
    // continue through their original MakeActive implementation.
    [HarmonyPatch(typeof(MenuButtonGroup), "MakeActive")]
    internal static class GarageMenuTogglePatch
    {
        private static bool Prefix(MenuButtonGroup __instance, bool isMouseKlick)
        {
            if (!__instance || !isMouseKlick) return true;
            var garage = __instance.GetComponentInParent<CarGarageUI>();
            if (!garage) return true;
            var panel = garage.GetComponent<GarageTuningPanel>();
            return !panel || !panel.TryCollapseTuningList(__instance, true);
        }
    }
}
