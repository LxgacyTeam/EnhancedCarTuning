using EnhancedCarTuning.Core;
using HarmonyLib;
using UnityEngine;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(EditCar_Preview), "UpPos")]
    internal static class MenuPreviewPositionPatch
    {
        private static void Postfix(EditCar_Preview __instance)
        {
            var car = __instance.car;
            if (!car || !car.isUsedInMenu || !car.body) return;
            var state = car.GetComponent<VehicleTuningState>();
            if (!state || state.WheelService == null) return;
            state.WheelService.GetPreviewCorrection(out float lift, out float pitch);
            if (Mathf.Abs(lift) < 0.00001f && Mathf.Abs(pitch) < 0.00001f) return;
            car.SetPosAndRot(car.body_pos + new Vector3(0, lift, 0), car.body_rot * Quaternion.Euler(pitch, 0, 0));
            // In a paused menu these caches otherwise keep contact data for the former body pose.
            foreach (var wheel in car.ph_car.wheels)
                if (wheel) { wheel.ResetCacheData(); wheel.WasMovedResetForces(); }
        }
    }
}
