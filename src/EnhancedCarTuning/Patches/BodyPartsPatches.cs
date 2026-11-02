using EnhancedCarTuning.Appearance.Parts;
using HarmonyLib;
using System.Xml;
namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarMaterial),"SaveLoad")]
    internal static class BodyPartsSavePatch
    {
        private static void Postfix(CarMaterial __instance,XmlElement __0,bool __1)
        {
            var car=__instance.car?__instance.car:__instance.GetComponent<CarControl>();if(!car)return;
            if(!__1&&VinylPortBridge.IsApplyingPaint(car))return;
            var state=car.GetComponent<BodyPartsState>();
            if(__1){BodyPartsXml.Write(__0,state?(state.SerializePreview?state.Active:state.Committed):BodyPartsSettings.Stock);return;}
            if(!BodyPartsXml.TryRead(__0,out var settings)){Plugin.Warn("Invalid body parts XML; restoring native parts.");settings=BodyPartsSettings.Stock;}
            if(state||BodyPartsState.IsBedVehicle(car)||!settings.Equals(BodyPartsSettings.Stock))BodyPartsState.Ensure(car)?.Commit(settings);
        }
    }
    [HarmonyPatch(typeof(CarUpgrade),"SetSelectItem")]
    internal static class BodyPartsUpgradePatch
    {private static void Postfix(CarUpgrade __instance)=>__instance.GetComponentInParent<BodyPartsState>()?.Apply(true);}
    [HarmonyPatch(typeof(CarDetail),"Fix")]
    internal static class BodyPartsFixPatch
    {private static void Postfix(CarDetail __instance)=>__instance.GetComponentInParent<BodyPartsState>()?.Apply(true);}
    [HarmonyPatch(typeof(MeshLoadFromResources),"Loaded")]
    internal static class BodyPartsLoadedPatch
    {private static void Postfix(MeshLoadFromResources __instance)=>__instance.GetComponentInParent<BodyPartsState>()?.Apply(true);}
}
