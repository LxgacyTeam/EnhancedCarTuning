using EnhancedCarTuning.Core;
using HarmonyLib;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarWheel), "SetKilled")]
    internal static class WheelModelKilledPatch
    {
        private static void Postfix(CarWheel __instance) => __instance.GetComponentInParent<VehicleTuningState>()?.WheelModels?.SetKilled(__instance, 1);
    }
    [HarmonyPatch(typeof(CarWheel), "Fix")]
    internal static class WheelModelFixPatch
    {
        private static void Postfix(CarWheel __instance) => __instance.GetComponentInParent<VehicleTuningState>()?.WheelModels?.SetKilled(__instance, 0);
    }
}
