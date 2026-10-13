using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Xml;
using EnhancedCarTuning.Appearance.Body;
using EnhancedCarTuning.Appearance.Police;
using EnhancedCarTuning.UI;
using HarmonyLib;
using UnityEngine;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(CarMaterial), "Apply")]
    internal static class BodyPaintMaterialPatch
    {
        private static void Prefix(CarMaterial __instance)
        {
            var state = __instance.GetComponent<BodyPaintState>();
            if (state) state.ApplyBase(state.Active);
            __instance.GetComponent<PolicePaintState>()?.PrepareOverride();
        }
        private static void Postfix(CarMaterial __instance) => __instance.GetComponent<BodyPaintState>()?.Renderer?.Sync();
    }
    [HarmonyPatch(typeof(CarGarageUI_Buy), "BodyPaint")]
    internal static class BodyPaintPurchasePatch
    {
        // The ECT checkout item includes Body and its native fallback; avoid a second purchase.
        private static bool Prefix(CarGarageUI_Buy __instance) => GarageTuningPanel.Find(__instance)?.BodyPaintSession == null;
    }
    [HarmonyPatch(typeof(CarMaterial), "CheckSharedMats")]
    internal static class BodyPaintBindingsPatch
    {
        private static void Postfix(CarMaterial __instance, MeshRenderer mr, ref Material[] __result)
        {
            var state = __instance.GetComponent<BodyPaintState>();
            if (!state || state.Active.IsEmpty || state.Renderer == null) return;
            state.Renderer.ScanNow(); __result = mr.sharedMaterials;
        }
    }
    [HarmonyPatch(typeof(MeshLoadFromResources), "Loaded")]
    internal static class BodyPaintMeshPatch
    {
        private static void Postfix(MeshLoadFromResources __instance)
        { var state = __instance.GetComponentInParent<BodyPaintState>(); if (state) state.Renderer?.ScanNow(); }
    }
    [HarmonyPatch(typeof(CarPaintItems), "DrawAll")]
    internal static class BodyPaintVinylPatch
    {
        private static void Postfix(CarPaintItems __instance) => __instance.GetComponent<BodyPaintState>()?.Renderer?.DrawVinyls(__instance);
    }
    [HarmonyPatch(typeof(CarGarageUI), "Update")]
    internal static class BodyPaintPickerPatch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int changed = 0;
            foreach (var instruction in instructions)
            {
                string replacement = instruction.Calls(AccessTools.Method(typeof(MenuColor), "GetColor")) ? nameof(Color) :
                    instruction.Calls(AccessTools.Method(typeof(MenuColor), "GetMettalic")) ? nameof(Metallic) :
                    instruction.Calls(AccessTools.Method(typeof(MenuColor), "GetGloss")) ? nameof(Gloss) : null;
                if (replacement != null)
                { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(BodyPaintPickerPatch), replacement); changed++; }
                yield return instruction;
            }
            if (changed != 6) throw new InvalidOperationException("Unexpected CarGarageUI.Update color call sites: " + changed);
        }
        private static PaintFinish Current(MenuColor picker)
        {
            var garage = CarGarageUI.me;
            if (!garage || picker != garage.bodyColor) return null;
            return garage.GetComponent<GarageTuningPanel>()?.BodyPaintSession?.Base;
        }
        internal static Color Color(MenuColor picker) => Current(picker)?.Color ?? picker.GetColor();
        internal static float Metallic(MenuColor picker) => Current(picker)?.Metallic ?? picker.GetMettalic();
        internal static float Gloss(MenuColor picker) => Current(picker)?.Gloss ?? picker.GetGloss();
    }
}
