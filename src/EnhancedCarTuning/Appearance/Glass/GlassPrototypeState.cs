using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EnhancedCarTuning.Appearance.Vinyls;
using EnhancedCarTuning.Patches;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Appearance.Glass
{
    internal sealed class GlassPrototypeState : MonoBehaviour
    {
        internal const float InteriorTintFactor = .85f;
        private sealed class Target { internal bool Glass; }
        private sealed class Surface
        { internal CarWindow Window; internal MeshFilter Filter; internal MeshRenderer Renderer; internal int Slot; internal Material Original, FinishMaterial; internal GlassGroup Group; }
        private static readonly ConditionalWeakTable<CarPaintItems.Item, Target> targets = new ConditionalWeakTable<CarPaintItems.Item, Target>();
        private readonly List<Surface> surfaces = new List<Surface>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Material> shared = new List<Material>();
        private MaterialPropertyBlock tintProperties;
        private Material tint;
        private CommandBuffer drawingBuffer;
        private Camera drawingCamera;
        private float nextScan;
        private bool failed;
        internal CarPaintItems Paint { get; private set; }
        internal Color Active = new Color(0, 0, 0, 0);
        internal Color Committed = new Color(0, 0, 0, 0);
        internal GlassFinish ActiveFinish, CommittedFinish;
        internal Dictionary<GlassGroup, GlassGroupPaint> ActiveGroups = new Dictionary<GlassGroup, GlassGroupPaint>();
        internal Dictionary<GlassGroup, GlassGroupPaint> CommittedGroups = new Dictionary<GlassGroup, GlassGroupPaint>();
        internal GlassGroupPaint Effective(GlassGroup group) => ActiveGroups.TryGetValue(group, out var paint) ? paint : GlassGroupPaint.IsLamp(group) ? new GlassGroupPaint(new Color(0,0,0,0), null) : new GlassGroupPaint(Active, ActiveFinish);
        internal int GroupCount(GlassGroup group) { int count = 0; foreach (var surface in surfaces) if (surface.Group == group) count++; return count; }
        private bool HasTint { get { if (Active.a > .001f) return true; foreach (var setting in ActiveGroups.Values) if (setting.Tint.a > .001f) return true; return false; } }
        internal bool SerializePreview;
        internal int WindowCount { get { int count = 0; foreach (var surface in surfaces) if (!GlassGroupPaint.IsLamp(surface.Group)) count++; return count; } }
        internal int GlassCount { get { int count = 0; if (Paint) foreach (var item in Paint.items) if (IsGlass(item)) count++; return count; } }
        internal static bool IsGlass(CarPaintItems.Item item) => item != null && targets.TryGetValue(item, out var target) && target.Glass;
        internal static void SetGlass(CarPaintItems.Item item, bool glass)
        { if (item != null) targets.GetValue(item, _ => new Target()).Glass = glass; }
        internal bool HasWindowTint(CarWindow window)
        {
            if (!isActiveAndEnabled || failed) return false;
            foreach (var surface in surfaces)
                if (surface.Window == window && !GlassGroupPaint.IsLamp(surface.Group) && Effective(surface.Group).Tint.a > .001f) return true;
            return false;
        }
        internal static GlassPrototypeState Ensure(CarPaintItems paint)
        {
            if (!paint) return null;
            var state = paint.GetComponent<GlassPrototypeState>() ?? paint.gameObject.AddComponent<GlassPrototypeState>();
            state.Paint = paint; GlassOverlayRenderer.Register(state); return state;
        }
        internal void Scan()
        {
            nextScan = Time.unscaledTime + 1;
            var previous = new List<Surface>(surfaces);
            surfaces.Clear();
            if (!Paint || !Paint.car || !Paint.car.tr_view) { foreach (var old in previous) Restore(old, true); return; }
            foreach (var window in Paint.car.tr_view.GetComponentsInChildren<CarWindow>(true))
            {
                var loader = window.GetComponent<MeshLoadFromResources>();
                GlassGroup lampGroup;
                bool lamp = GlassWindowClassifier.TryLampGroup(window.name, out lampGroup) || loader && GlassWindowClassifier.TryLampGroup(loader.mesh_name, out lampGroup);
                if (!lamp && !GlassWindowClassifier.IsCabinWindow(window, Paint.car.tr_view)) continue;
                var filter = window.GetComponent<MeshFilter>(); var renderer = window.GetComponent<MeshRenderer>();
                if (!filter || !filter.sharedMesh || !renderer) continue;
                renderer.GetSharedMaterials(shared);
                for (int slot = 0; slot < shared.Count && slot < filter.sharedMesh.subMeshCount; slot++)
                {
                    Surface binding = null;
                    foreach (var old in previous) if (old.Renderer == renderer && old.Slot == slot) { binding = old; break; }
                    var material = binding != null && shared[slot] == binding.FinishMaterial ? binding.Original : shared[slot];
                    if (!material || !material.shader || !(material.shader.name.StartsWith("Custom/Car/CarWindow", StringComparison.Ordinal) ||
                        lamp && material.shader.name.StartsWith("Custom/Car/Car_Light_Glass", StringComparison.Ordinal))) continue;
                    if (binding != null && binding.Original == material)
                    { binding.Group = lamp ? lampGroup : GlassWindowClassifier.Classify(window, renderer, Paint.car.tr_view); previous.Remove(binding); surfaces.Add(binding); }
                    else surfaces.Add(new Surface { Window = window, Filter = filter, Renderer = renderer, Slot = slot, Original = material,
                        Group = lamp ? lampGroup : GlassWindowClassifier.Classify(window, renderer, Paint.car.tr_view) });
                }
                shared.Clear();
            }
            foreach (var old in previous) Restore(old, true);
        }
        private void LateUpdate()
        {
            if (NetManager.isServer || failed || !Paint || !Paint.car || !Paint.car.tr_view || !Paint.car.tr_view.gameObject.activeInHierarchy) return;
            if (Paint.car.Dist2CamType == Control.DistType2Cam.Far_Unvisible && !Paint.car.isUsedInMenu) return;
            bool hasVinyl = false;
            foreach (var item in Paint.items) if (IsGlass(item)) { hasVinyl = true; break; }
            bool finishBound = false;
            foreach (var surface in surfaces) if (surface.FinishMaterial) { finishBound = true; break; }
            if (!HasTint && !hasVinyl && ActiveFinish == null && ActiveGroups.Count == 0 && !finishBound) return;
            try
            {
                if (Time.unscaledTime >= nextScan) Scan();
                var shader = GlassPrototypeAssets.Shader;
                if (!shader) return;
                SyncFinish();
            }
            catch (Exception error) { failed = true; foreach (var surface in surfaces) Restore(surface, true); Plugin.Warn("Glass prototype renderer stopped: " + error); }
        }
        internal void RenderOverlay(Camera camera, CommandBuffer commands)
        {
            if (NetManager.isServer || failed || !Paint || !Paint.car || !Paint.car.tr_view || !Paint.car.tr_view.gameObject.activeInHierarchy) return;
            if (Paint.car.Dist2CamType == Control.DistType2Cam.Far_Unvisible && !Paint.car.isUsedInMenu) return;
            if (camera.cullingMask == 0) return;
            if (!HasTint)
            {
                bool hasVinyl = false;
                foreach (var item in Paint.items) if (IsGlass(item)) { hasVinyl = true; break; }
                if (!hasVinyl) return;
            }
            var shader = GlassPrototypeAssets.Shader;
            if (!shader) return;
            drawingBuffer = commands; drawingCamera = camera;
            try
            {
                if (!tint) tint = new Material(shader) { name = "ECT.Glass.Tint", renderQueue = 3100 };
                float inside = Paint.car.isContainUser() && Cam.me && Cam.me.isFirstPerson ? InteriorTintFactor : 1;
                tint.SetFloat("_Projected", 0); tint.SetColor("_Color", new Color(Active.r, Active.g, Active.b, TintOpacity(Active.a) * inside));
                if (HasTint) Draw(tint, true);
                int index = 0, drawn = 0;
                foreach (var item in Paint.items)
                {
                    if (!IsGlass(item) || item.spriteInfo == null || drawn >= 32) continue;
                    var texture = VinylSymmetryPatch.ResolveTexture(item.spriteInfo);
                    if (!texture) continue;
                    drawn++;
                    while (index >= materials.Count) materials.Add(new Material(shader) { name = "ECT.Glass.Vinyl", renderQueue = 3101 + materials.Count });
                    var material = materials[index++];
                    material.SetFloat("_Projected", 1); material.SetTexture("_MainTex", texture); material.SetColor("_Color", VinylColor(item.color));
                    material.SetFloat("_Invert", item.spriteInfo.invert ? 1 : 0);
                    Project(material, item.pos, item.getQuat, item.size_y, item.acpect, item.depth, false);
                    if (!item.isSymetry) continue;
                    var forward = item.getQuat * Vector3.forward; var up = item.getQuat * Vector3.up;
                    if (!VinylSymmetry.HasSecondSurface(item.pos, forward)) continue;
                    while (index >= materials.Count) materials.Add(new Material(shader) { name = "ECT.Glass.Mirror", renderQueue = 3101 + materials.Count });
                    var mirror = materials[index++]; mirror.CopyPropertiesFromMaterial(material);
                    Project(mirror, VinylSymmetry.Reflect(item.pos), Quaternion.LookRotation(VinylSymmetry.Reflect(forward), VinylSymmetry.Reflect(up)),
                        item.size_y, item.acpect, item.depth, !TextVinylData.IsText(item.spriteName));
                }
            }
            catch (Exception error) { failed = true; foreach (var surface in surfaces) Restore(surface, true); Plugin.Warn("Glass prototype renderer stopped: " + error); }
            finally { drawingBuffer = null; drawingCamera = null; }
        }
        internal static float TintOpacity(float density)
        { float clear = 1 - Mathf.Clamp01(density); return 1 - clear * clear * clear; }
        internal static Color VinylColor(Color color) => new Color(color.r,color.g,color.b,color.a * .5f);
        private void SyncFinish()
        {
            bool hasFinish = ActiveFinish != null;
            foreach (var setting in ActiveGroups.Values) if (setting.Finish != null) hasFinish = true;
            var shader = hasFinish ? GlassPrototypeAssets.FinishShader : null;
            foreach (var surface in surfaces)
            {
                var setting = Effective(surface.Group); var finish = setting.Finish;
                if (surface.Window && Cam.me && Paint && Paint.car && Paint.car.isContainUser() && Cam.me.isFirstPerson)
                {
                    float opacity = !GlassGroupPaint.IsLamp(surface.Group) && setting.Tint.a > .001f ? InteriorTintFactor : .6f;
                    if (Mathf.Abs(surface.Window.lastSetColor.a - opacity) > .001f) surface.Window.CheckCameraInside();
                }
                if (!surface.Window || !surface.Renderer || !surface.Original) { Restore(surface, true); continue; }
                if (!shader || finish == null || surface.Window.state != CarWindow.State.Normal) { Restore(surface, finish == null); continue; }
                if (!surface.FinishMaterial) surface.FinishMaterial = new Material(shader) { name = "ECT.Glass.Finish" };
                var color = new Color(setting.Tint.r, setting.Tint.g, setting.Tint.b, TintOpacity(setting.Tint.a));
                surface.FinishMaterial.SetColor("_Color", color);
                surface.FinishMaterial.SetFloat("_Gloss", Mathf.Clamp(finish.Gloss, 0, .98f));
                surface.FinishMaterial.SetFloat("_Metallic", finish.Metallic);
                var array = surface.Renderer.sharedMaterials;
                if (surface.Slot < array.Length && array[surface.Slot] == surface.Original)
                { array[surface.Slot] = surface.FinishMaterial; surface.Renderer.sharedMaterials = array; }
            }
        }
        private static void Restore(Surface surface, bool release)
        {
            if (!surface.FinishMaterial) return;
            if (surface.Renderer)
            {
                var array = surface.Renderer.sharedMaterials;
                if (surface.Slot < array.Length && array[surface.Slot] == surface.FinishMaterial)
                { array[surface.Slot] = surface.Original; surface.Renderer.sharedMaterials = array; }
            }
            if (release) { Object.Destroy(surface.FinishMaterial); surface.FinishMaterial = null; }
        }
        private void Project(Material material, Vector3 position, Quaternion rotation, float size, float aspect, float depth, bool mirror)
        {
            var view = Paint.car.tr_view;
            var worldRotation = view.rotation * rotation;
            material.SetMatrix("_WorldToDecal", Matrix4x4.TRS(view.TransformPoint(position), worldRotation, Vector3.one).inverse);
            material.SetVector("_DecalSize", new Vector4(Mathf.Max(.001f, size * 2 * aspect), Mathf.Max(.001f, size * 2), Mathf.Max(.001f, depth), mirror ? -1 : 1));
            material.SetVector("_DecalForward", worldRotation * Vector3.forward);
            Draw(material);
        }
        private void Draw(Material material, bool nativeOnly = false)
        {
            foreach (var surface in surfaces)
            {
                if (!surface.Window || !surface.Renderer || !surface.Filter || !surface.Filter.sharedMesh ||
                    !surface.Renderer.enabled || !surface.Renderer.gameObject.activeInHierarchy || surface.Slot >= surface.Filter.sharedMesh.subMeshCount) continue;
                if (surface.Window.state == CarWindow.State.Destroed || !nativeOnly && GlassGroupPaint.IsLamp(surface.Group)) continue;
                if ((drawingCamera.cullingMask & (1 << surface.Renderer.gameObject.layer)) == 0) continue;
                var bound = surface.Renderer.sharedMaterials;
                if (surface.Slot >= bound.Length) continue;
                if (nativeOnly && surface.FinishMaterial && bound[surface.Slot] == surface.FinishMaterial) continue;
                if (nativeOnly)
                {
                    var color = Effective(surface.Group).Tint;
                    if (color.a <= .001f) continue;
                    float inside = Paint.car.isContainUser() && Cam.me && Cam.me.isFirstPerson ? InteriorTintFactor : 1;
                    if (tintProperties == null) tintProperties = new MaterialPropertyBlock();
                    tintProperties.SetColor("_Color", new Color(color.r, color.g, color.b, TintOpacity(color.a) * inside));
                    drawingBuffer.DrawMesh(surface.Filter.sharedMesh, surface.Filter.transform.localToWorldMatrix, material, surface.Slot, 0, tintProperties);
                }
                else drawingBuffer.DrawMesh(surface.Filter.sharedMesh, surface.Filter.transform.localToWorldMatrix, material, surface.Slot, 0);
            }
        }
        internal void Place(CarPaintItems.Item item, int side)
        {
            Scan(); if (item == null || surfaces.Count == 0) return;
            Surface best = null; float score = float.NegativeInfinity;
            var view = Paint.car.tr_view;
            foreach (var surface in surfaces)
            {
                if (GlassGroupPaint.IsLamp(surface.Group)) continue;
                var point = view.InverseTransformPoint(surface.Renderer.bounds.center);
                float value = side == 0 ? point.z : side == 1 ? -point.z : side == 2 ? -point.x : point.x;
                if (value > score) { score = value; best = surface; }
            }
            if (best == null) return;
            var center = view.InverseTransformPoint(best.Renderer.bounds.center);
            var outward = side == 0 ? new Vector3(0, .4f, 1).normalized : side == 1 ? new Vector3(0, .4f, -1).normalized :
                side == 2 ? new Vector3(-1, .15f, 0).normalized : new Vector3(1, .15f, 0).normalized;
            item.pos = center + outward * .35f;
            var rotation = Quaternion.LookRotation(-outward, Vector3.up).eulerAngles;
            item.rot = new Vector3(rotation.x, rotation.y, -rotation.z);
            item.size_y = .18f; item.depth = .7f;
        }
        internal void Reset()
        {
            ActiveFinish = CommittedFinish = null; ActiveGroups.Clear(); CommittedGroups.Clear();
            Active = Committed = new Color(0, 0, 0, 0); SerializePreview = false;
            foreach (var surface in surfaces)
            {
                Restore(surface, true);
                if (surface.Window && surface.Window.car && Cam.me && Mathf.Abs(surface.Window.lastSetColor.a - InteriorTintFactor) < .001f)
                    surface.Window.CheckCameraInside();
            }
            surfaces.Clear(); nextScan = 0; failed = false;
        }
        private void OnEnable() { GlassOverlayRenderer.Register(this); }
        private void OnDisable() { GlassOverlayRenderer.Unregister(this); Reset(); Release(); }
        private void OnDestroy() { GlassOverlayRenderer.Unregister(this); Release(); }
        private void Release()
        { foreach (var surface in surfaces) Restore(surface, true); if (tint) Object.Destroy(tint); tint = null; foreach (var material in materials) if (material) Object.Destroy(material); materials.Clear(); }
    }
}
