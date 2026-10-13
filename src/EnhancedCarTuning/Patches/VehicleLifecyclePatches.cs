using EnhancedCarTuning.TuneUp.Engine;
using EnhancedCarTuning.Core;
using HarmonyLib;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarControl), "Awake")]
    internal static class CarAwakePatch
    {
        private static void Postfix(CarControl __instance) => VehicleTuningState.Ensure(__instance);
    }

    [HarmonyPatch(typeof(CarControl), "OnDisable")]
    internal static class CarPoolPatch
    {
        private static void Prefix(CarControl __instance) => __instance.GetComponent<VehicleTuningState>()?.ResetForPool();
    }

    [HarmonyPatch(typeof(NetManager), "NowStoped")]
    internal static class NetworkStopPatch
    {
        private static void Postfix() => EngineCatalog.Reset();
    }
}
