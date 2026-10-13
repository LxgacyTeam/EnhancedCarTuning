using EnhancedCarTuning.Networking;
using EnhancedCarTuning.Core;
using EnhancedCarTuning.Appearance.Body;
using EnhancedCarTuning.Appearance.Police;
using EnhancedCarTuning.Persistence;
using HarmonyLib;
using UnityEngine.Networking;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarSaveLoad), "Load")]
    internal static class LoadedTuningPriorityPatch
    {
        private static void Prefix(CarSaveLoad __instance)
        { if (__instance.car) VehicleTuningState.Ensure(__instance.car); }
        private static void Postfix(CarSaveLoad __instance)
        { if (__instance.car) __instance.car.GetComponent<VehicleTuningState>()?.RestoreNativeTuning(); }
    }
    [HarmonyPatch(typeof(NetControl), "SetXmlDataServer")]
    internal static class ServerXmlPatch
    {
        private static bool Prefix(NetControl __instance, string setXmlData)
        {
            if (!NetworkServer.active) return true;
            if (setXmlData != null && setXmlData.StartsWith("MDMP_LIGHTS:", System.StringComparison.Ordinal))
            {
                return byte.TryParse(setXmlData.Substring(12), out _);
            }
            bool car = __instance.control is CarControl || !__instance.control && !string.IsNullOrEmpty(__instance.objName) && __instance.objName != "Male";
            if (!car) return true;
            if (NetworkTuningPolicy.AcceptChange(__instance, setXmlData)) VehicleXmlSync.Publish(__instance, setXmlData);
            return false;
        }
    }

    [HarmonyPatch(typeof(NetInputControl), "DeliveryCar")]
    internal static class DeliveryPatch
    {
        private static bool Prefix(string xmlData) => NetworkTuningPolicy.Validate(xmlData, out _);
    }
}
