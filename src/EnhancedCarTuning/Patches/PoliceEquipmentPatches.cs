using EnhancedCarTuning.Appearance.Police;
using HarmonyLib;
using UnityEngine.Networking;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarUpgrade), "SetSelectItem")]
    internal static class PoliceEquipmentUpgradePatch
    {
        private static void Postfix(CarUpgrade __instance)
        {
            if (!string.Equals(__instance.txt, "Police", System.StringComparison.OrdinalIgnoreCase)) return;
            __instance.GetComponentInParent<PolicePaintState>()?.ApplyEquipment();
        }
    }
    [HarmonyPatch(typeof(CarSirena), "PlayerPressHorn")]
    internal static class PoliceRemoteHornPatch
    {
        private static bool Prefix(CarSirena __instance)
        {
            var car = __instance.car;
            var state = car ? car.GetComponent<PolicePaintState>() : null;
            return !state || !state.Active || !NetworkClient.active || !car.net_control || car.net_control.hasAuthority;
        }
    }
    [HarmonyPatch(typeof(CarSirena), "SetState")]
    internal static class PoliceRemoteSirenStatePatch
    {
        private static bool Prefix(CarSirena __instance, CarSirena.State state)
        {
            var car = __instance.car;
            var tuning = car ? car.GetComponent<PolicePaintState>() : null;
            return !tuning || !tuning.Active || !car.isAlive || !NetworkClient.active || !car.net_control ||
                car.net_control.hasAuthority || (int)state == tuning.SirenMode;
        }
    }
}
