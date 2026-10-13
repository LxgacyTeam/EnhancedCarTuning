using EnhancedCarTuning.Wheels.Geometry;
using HarmonyLib;
using UnityEngine;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarPhWheel), "GetFinalRotForView")]
    internal static class WheelCamberPatch
    {
        private static void Postfix(CarPhWheel __instance, ref Quaternion __result)
        {
            var visual = __instance.GetComponent<WheelVisualTuning>();
            if (!visual || Mathf.Abs(visual.Camber) < 0.001f) return;
            // Tilt the rendered wheel without tilting the suspension ray or changing the native grip model.
            Vector3 axis = __instance.local_rotation * Vector3.forward;
            if (axis.z < 0) axis = -axis;
            __result = Quaternion.AngleAxis(visual.Camber * (__instance.IsLeft ? 1 : -1), axis) * __result;
        }
    }
}
