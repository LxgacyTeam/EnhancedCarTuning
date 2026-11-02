using System;
using System.Xml;
using System.Collections.Generic;
using System.Reflection.Emit;
using EnhancedCarTuning.Appearance.Vinyls;
using EnhancedCarTuning.Appearance.Body;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using EnhancedCarTuning.UI;

namespace EnhancedCarTuning.Patches
{
    [HarmonyPatch(typeof(MenuFocusManager), "Update")]
    internal static class VinylTypingPatch
    {
        private static bool Prefix(MenuFocusManager __instance)
        {
            if(ColorPickerHex.AnyTyping)return false;
            var garage = __instance.GetComponentInParent<CarGarageUI>();
            var panel = garage ? garage.GetComponent<GarageTuningPanel>() : null;
            return !panel || !panel.IsTypingVinyl;
        }
    }
    [HarmonyPatch(typeof(CarGarageUI), "SetEditItem")]
    internal static class VinylSelectionPatch
    {
        private static void Prefix(CarGarageUI __instance)
        { var panel = __instance.GetComponent<GarageTuningPanel>(); if (panel) panel.FlushVinyls(); }
    }
    [HarmonyPatch(typeof(CarGarageUI), "CarPaint_Clone")]
    internal static class VinylClonePatch
    {
        private static void Prefix(CarGarageUI __instance)
        { var panel = __instance.GetComponent<GarageTuningPanel>(); if (panel) panel.FlushVinyls(); }
    }
    [HarmonyPatch(typeof(CarPaintItems.Item), "SaveLoad")]
    internal static class VinylSavePatch
    {
        private static void Postfix(CarPaintItems.Item __instance, XmlElement xml, bool isSave)
        {
            if (isSave)
            {
                VinylXml.WriteSymmetry(xml, __instance.isSymetry);
                TextVinylData.TryRead(__instance.spriteName, out var data);
                VinylXml.WriteText(xml, data);
            }
            else
            {
                __instance.isSymetry = VinylXml.ReadSymmetry(xml);
                if (VinylXml.TryReadText(xml, out var data)) __instance.SetSpriteInfo(TextVinylTextures.Info(data.Name));
            }
        }
    }
    [HarmonyPatch(typeof(CarPaintItems.Item), "isHaveChanges")]
    internal static class VinylChangesPatch
    {
        private static void Postfix(CarPaintItems.Item __instance, CarPaintItems.Item w, ref bool __result)
        { __result |= __instance.isSymetry != w.isSymetry || __instance.spriteName != w.spriteName; }
    }
    [HarmonyPatch(typeof(CarPaintSprites), "FindSpriteByName")]
    internal static class TextVinylLookupPatch
    {
        private static bool Prefix(string spriteName, ref CarPaintSprites.SpriteInfo __result)
        {
            if (!TextVinylData.IsText(spriteName)) return true;
            __result = TextVinylTextures.Info(spriteName); return false;
        }
    }
    [HarmonyPatch(typeof(CarPaintSprites.SpriteInfo), "get_texture")]
    internal static class TextVinylTexturePatch
    {
        private static bool Prefix(CarPaintSprites.SpriteInfo __instance, ref Texture __result)
        {
            if (!TextVinylData.IsText(__instance.texutreName)) return true;
            try { __result = TextVinylTextures.Texture(__instance.texutreName); }
            catch (Exception error) { Plugin.Warn("Text vinyl rasterization failed: " + error); }
            return false;
        }
    }
    [HarmonyPatch(typeof(CarPaintSprites.SpriteInfo), "isLoaded")]
    internal static class TextVinylLoadedPatch
    {
        private static bool Prefix(CarPaintSprites.SpriteInfo __instance, ref bool __result)
        {
            if (!TextVinylData.IsText(__instance.texutreName)) return true;
            __result = TextVinylData.TryRead(__instance.texutreName, out _) &&
                (NetManager.isServer || TextVinylTextures.Texture(__instance.texutreName)); return false;
        }
    }
    [HarmonyPatch(typeof(CarPaintItems), "DrawAll")]
    internal static class VinylSymmetryPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int changed = 0;
            var draw = AccessTools.Method(typeof(CarPaintItems), "DrawIt");
            var texture = AccessTools.PropertyGetter(typeof(CarPaintSprites.SpriteInfo), "texture");
            var loaded = AccessTools.Method(typeof(CarPaintItems), "isAllItemsTexturesLoaded");
            var target = AccessTools.Method(typeof(CarPaintItems), "TryGetTextureRt");
            foreach (var instruction in instructions)
            {
                string replacement = instruction.Calls(draw) ? nameof(DrawLayer) :
                    instruction.Calls(texture) ? nameof(ResolveTexture) : instruction.Calls(loaded) ? nameof(AllLoaded) :
                    instruction.Calls(target) ? nameof(PrepareTexture) : null;
                if (replacement != null)
                { instruction.opcode = OpCodes.Call; instruction.operand = AccessTools.Method(typeof(VinylSymmetryPatch), replacement); changed++; }
                yield return instruction;
            }
            if (changed != 4) throw new InvalidOperationException("Unexpected CarPaintItems.DrawAll: expected four vinyl call sites, got " + changed);
        }
        internal static bool PrepareTexture(CarPaintItems paint) => VinylRenderTextures.Ensure(paint);
        internal static Texture ResolveTexture(CarPaintSprites.SpriteInfo info) => TextVinylData.IsText(info.texutreName)
            ? TextVinylTextures.Texture(info.texutreName) : info.texture;
        internal static bool AllLoaded(CarPaintItems paint)
        {
            bool loaded = true;
            foreach (var item in paint.items)
                if (item.spriteInfo != null && !(TextVinylData.IsText(item.spriteName) ?
                    ResolveTexture(item.spriteInfo) != null : item.spriteInfo.isLoaded())) loaded = false;
            return loaded;
        }
        internal static void DrawLayer(CarPaintItems __instance, CarPaintItems.Item item, CommandBuffer buf, Camera cam,
            MeshLoadFromResources[] arr, Material carMat, Material mat)
        {
            DrawGeometry(__instance, item, buf, cam, arr, carMat, mat);
            if (!item.isSymetry || item.spriteInfo == null || item.spriteInfo.type == CarPaintSprites.TypeVinil.FillCollor) return;
            var view = __instance.car.tr_view;
            var localRotation = item.getQuat;
            var forward = localRotation * Vector3.forward;
            var up = localRotation * Vector3.up;
            if (!VinylSymmetry.HasSecondSurface(item.pos, forward)) return;
            var pos = VinylSymmetry.Reflect(item.pos); forward = VinylSymmetry.Reflect(forward); up = VinylSymmetry.Reflect(up);
            var previousPos = cam.transform.position; var previousRot = cam.transform.rotation;
            var projection = cam.projectionMatrix;
            try
            {
                cam.transform.SetPositionAndRotation(view.position + view.rotation * pos,
                    view.rotation * Quaternion.LookRotation(forward, up));
                if (!TextVinylData.IsText(item.spriteName))
                    cam.projectionMatrix = projection * Matrix4x4.Scale(new Vector3(-1, 1, 1));
                DrawGeometry(__instance, item, buf, cam, arr, carMat, mat);
            }
            finally { cam.ResetProjectionMatrix(); cam.transform.SetPositionAndRotation(previousPos, previousRot); }
        }
        private static void DrawGeometry(CarPaintItems paint, CarPaintItems.Item item, CommandBuffer buffer, Camera camera,
            MeshLoadFromResources[] meshes, Material body, Material material)
        {
            if (EnhancedCarTuning.Appearance.Glass.GlassPrototypeState.IsGlass(item)) return;
            var state = paint.GetComponent<BodyPaintState>();
            if (!state || state.Renderer == null || state.Active.IsEmpty) paint.DrawIt(item, buffer, camera, meshes, body, material);
            else state.Renderer.DrawGeometry(paint, item, buffer, camera, meshes, body, material);
        }
    }
}
