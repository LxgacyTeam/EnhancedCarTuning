using System;
using System.Collections.Generic;
using EnhancedCarTuning.Appearance.Vinyls;
using EnhancedCarTuning.Patches;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Appearance.Body
{
    internal sealed class BodyPaintRenderer : IDisposable
    {
        private sealed class Binding
        { internal MeshRenderer Renderer; internal int Slot; internal BodyPart Part; internal Material Original; }
        private sealed class Surface
        { internal Material Material; internal RenderTexture Texture; }
        private readonly BodyPaintState state;
        private readonly List<Binding> bindings = new List<Binding>();
        private readonly List<Material> shared = new List<Material>(), layerMaterials = new List<Material>();
        private readonly Surface[] surfaces = new Surface[BodyPaintSettings.Count];
        private CommandBuffer commands;
        private BodyPart? drawingPart;
        private bool rendering, failed, forceScan;
        private float nextScan;
        private static readonly int Main = Shader.PropertyToID("_Main"), Metallic = Shader.PropertyToID("_Mettalic"), Gloss = Shader.PropertyToID("_Gloss");
        internal BodyPaintRenderer(BodyPaintState state)
        { this.state = state; for (int i = 0; i < surfaces.Length; i++) surfaces[i] = new Surface(); }
        internal int AvailableMask { get; private set; }
        internal void ScanNow() { nextScan = 0; forceScan = true; Sync(); state.Invalidate(); }
        internal void Sync()
        {
            var carMat = state.CarMaterial;
            if (!carMat || !carMat.body.usedMatreial || !carMat.car) return;
            if (carMat.overrideMat && carMat.overrideMat.isOverrideBodyTex)
            {
                for (int i = 0; i < surfaces.Length; i++)
                {
                    Restore((BodyPart)i); ReleaseTexture(surfaces[i]);
                    if (surfaces[i].Material) { Object.Destroy(surfaces[i].Material); surfaces[i].Material = null; }
                }
                return;
            }
            if (state.Active.IsEmpty && !forceScan && !HasSurfaces()) return;
            if ((forceScan || !state.Active.IsEmpty) && Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 1;
                Discover(carMat.car);
                forceScan = false;
            }
            var paint = carMat.paintItems;
            bool usesVinyl = paint && paint.isNeedUse();
            for (int i = 0; i < surfaces.Length; i++)
            {
                var finish = state.Active[(BodyPart)i]; var surface = surfaces[i];
                if (finish == null)
                {
                    Restore((BodyPart)i);
                    ReleaseTexture(surface);
                    if (surface.Material) { Object.Destroy(surface.Material); surface.Material = null; }
                    continue;
                }
                if ((AvailableMask & (1 << i)) == 0) continue;
                if (!surface.Material)
                { surface.Material = new Material(carMat.body.usedMatreial) { name = "ECT.Body." + (BodyPart)i }; }
                bool separateColor = usesVinyl && !finish.SameColor(carMat.body.color);
                if (!separateColor) ReleaseTexture(surface);
                surface.Material.color = usesVinyl ? Color.white : finish.Color;
                surface.Material.SetFloat(Metallic, finish.Metallic);
                surface.Material.SetFloat(Gloss, Mathf.Min(finish.Gloss, .98f));
                surface.Material.SetTexture(Main, separateColor && surface.Texture ? surface.Texture :
                    usesVinyl ? paint.textureRt : carMat.texture);
            }
            foreach (var binding in bindings)
            {
                if (!binding.Renderer || state.Active[binding.Part] == null) continue;
                var material = surfaces[(int)binding.Part].Material;
                if (!material) continue;
                var array = binding.Renderer.sharedMaterials;
                if (binding.Slot >= array.Length || array[binding.Slot] == material) continue;
                if (!IsSource(array[binding.Slot]) && array[binding.Slot] != binding.Original) continue;
                array[binding.Slot] = material; binding.Renderer.sharedMaterials = array;
            }
        }
        private bool HasSurfaces()
        { foreach (var surface in surfaces) if (surface.Material || surface.Texture) return true; return false; }
        private bool IsSource(Material material)
        {
            var carMat = state.CarMaterial;
            if (material == carMat.body.usedMatreial || material == carMat.body.mat) return true;
            foreach (var surface in surfaces) if (surface.Material && material == surface.Material) return true;
            if (carMat.parts != null) foreach (var part in carMat.parts)
                if (part && part.mat.mat == carMat.body.mat && material == part.mat.usedMatreial) return true;
            return false;
        }
        private void Discover(CarControl car)
        {
            bindings.RemoveAll(b => !b.Renderer);
            if (car.meshes_loader != null && car.meshes_loader.childs != null)
                foreach (var loader in car.meshes_loader.childs)
                {
                    if (!loader) continue;
                    var renderer = loader.GetComponent<MeshRenderer>(); if (!renderer) continue;
                    renderer.GetSharedMaterials(shared);
                    for (int i = 0; i < shared.Count; i++)
                    {
                        if (!shared[i] || !IsSource(shared[i])) continue;
                        bool found = false;
                        foreach (var binding in bindings) if (binding.Renderer == renderer && binding.Slot == i) { found = true; break; }
                        if (found) continue;
                        var part = Identify(loader, car.tr_view);
                        var original = shared[i] == state.CarMaterial.body.mat ? state.CarMaterial.body.usedMatreial : shared[i];
                        bindings.Add(new Binding { Renderer = renderer, Slot = i, Part = part, Original = original });
                        state.Invalidate();
                    }
                    shared.Clear();
                }
            AvailableMask = 0;
            foreach (var binding in bindings) AvailableMask |= 1 << (int)binding.Part;
        }
        private static BodyPart Identify(MeshLoadFromResources loader, Transform view)
        {
            var part = BodyPartNames.Classify(string.IsNullOrEmpty(loader.mesh_name) ? loader.name : loader.mesh_name);
            if (part != BodyPart.Body) return part;
            for (var node = loader.transform; node && node != view; node = node.parent)
            {
                part = BodyPartNames.Classify(node.name);
                if (part != BodyPart.Body) return part;
            }
            return BodyPart.Body;
        }
        private void Restore(BodyPart part)
        {
            var material = surfaces[(int)part].Material;
            if (!material) return;
            foreach (var binding in bindings)
            {
                if (!binding.Renderer || binding.Part != part) continue;
                var array = binding.Renderer.sharedMaterials;
                if (binding.Slot < array.Length && array[binding.Slot] == material)
                { array[binding.Slot] = binding.Original; binding.Renderer.sharedMaterials = array; }
            }
        }
        internal void DrawGeometry(CarPaintItems paint, CarPaintItems.Item item, CommandBuffer buffer, Camera camera,
            MeshLoadFromResources[] meshes, Material body, Material layer)
        {
            buffer.SetGlobalMatrix("matVP", camera.projectionMatrix * camera.worldToCameraMatrix);
            foreach (var loader in meshes)
            {
                if (!loader || !loader.gameObject.activeSelf || !loader.transform.parent || loader.transform.parent.GetComponent<CarMeshDistance>()) continue;
                var filter = loader.GetComponent<MeshFilter>(); var renderer = loader.GetComponent<MeshRenderer>();
                if (!filter || !filter.sharedMesh || !renderer) continue;
                renderer.GetSharedMaterials(shared);
                try
                {
                    for (int i = 0; i < shared.Count && i < filter.sharedMesh.subMeshCount; i++)
                    {
                        bool draw = !drawingPart.HasValue && shared[i] == body;
                        foreach (var binding in bindings)
                            if (binding.Renderer == renderer && binding.Slot == i &&
                                (!drawingPart.HasValue || binding.Part == drawingPart.Value) && IsSource(shared[i])) { draw = true; break; }
                        if (draw) buffer.DrawMesh(filter.sharedMesh, Matrix4x4.zero, layer, i);
                    }
                }
                finally { shared.Clear(); }
            }
        }
        internal void DrawVinyls(CarPaintItems paint)
        {
            if (state.CarMaterial.overrideMat && state.CarMaterial.overrideMat.isOverrideBodyTex) return;
            if (rendering || failed || state.Active.IsEmpty || !paint || !paint.isNeedUse() || paint.needDraw || !paint.textureRt || !CarPaintItems.cam) return;
            Sync();
            var camera = CarPaintItems.cam; var view = paint.car.tr_view;
            var position = camera.transform.position; var rotation = camera.transform.rotation;
            float size = camera.orthographicSize, aspect = camera.aspect, near = camera.nearClipPlane, far = camera.farClipPlane;
            rendering = true;
            try
            {
                if (commands == null) commands = new CommandBuffer { name = "ECT.BodyPaint" };
                for (int group = 0; group < surfaces.Length; group++)
                {
                    var finish = state.Active[(BodyPart)group]; var surface = surfaces[group];
                    if (finish == null || !surface.Material || finish.SameColor(state.CarMaterial.body.color)) continue;
                    int resolution = Mathf.Min(paint.textureRt.width, 2048);
                    if (surface.Texture && (surface.Texture.width != resolution || !surface.Texture.IsCreated())) ReleaseTexture(surface);
                    if (!surface.Texture)
                    {
                        var texture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                        { name = "ECT.BodyPaint." + (BodyPart)group, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat,
                            antiAliasing = 1, useMipMap = false, autoGenerateMips = false };
                        surface.Texture = texture;
                        if (!texture.Create()) throw new InvalidOperationException("Cannot allocate part vinyl texture.");
                    }
                    commands.Clear(); commands.SetRenderTarget(surface.Texture);
                    commands.ClearRenderTarget(false, true, finish.Color);
                    commands.SetGlobalMatrix("matW", view.localToWorldMatrix);
                    drawingPart = (BodyPart)group;
                    for (int i = 0; i < paint.items.Count; i++)
                    {
                        var item = paint.items[i]; if (item.spriteInfo == null) continue;
                        while (i >= layerMaterials.Count) layerMaterials.Add(new Material(paint.CarPaintShader) { name = "ECT.BodyPaint.Layer" });
                        var material = layerMaterials[i];
                        bool fill = item.spriteInfo.type == CarPaintSprites.TypeVinil.FillCollor;
                        material.mainTexture = fill ? null : VinylSymmetryPatch.ResolveTexture(item.spriteInfo);
                        material.mainTextureOffset = Vector2.zero; material.mainTextureScale = Vector2.one; material.color = item.color;
                        material.SetFloat(CarPaintItems.shaderID_IsInvert, item.spriteInfo.invert ? 1 : 0);
                        camera.ResetProjectionMatrix(); camera.orthographicSize = fill ? 100 : item.size_y;
                        camera.aspect = fill ? 1 : item.acpect; camera.farClipPlane = fill ? 100 : item.depth; camera.nearClipPlane = fill ? -100 : -1;
                        camera.transform.SetPositionAndRotation(view.position + view.rotation * item.pos, view.rotation * item.getQuat);
                        VinylSymmetryPatch.DrawLayer(paint, item, commands, camera, paint.car.meshes_loader.childs,
                            state.CarMaterial.body.usedMatreial, material);
                    }
                    Graphics.ExecuteCommandBuffer(commands); commands.Clear();
                    surface.Material.SetTexture(Main, surface.Texture);
                }
            }
            catch (Exception error)
            {
                failed = true; commands?.Clear();
                foreach (var surface in surfaces) { if (surface.Material) surface.Material.SetTexture(Main, paint.textureRt); ReleaseTexture(surface); }
                Plugin.Warn("Body-part vinyl rendering failed: " + error);
            }
            finally
            {
                drawingPart = null; rendering = false;
                camera.orthographicSize = size; camera.aspect = aspect; camera.nearClipPlane = near; camera.farClipPlane = far;
                camera.ResetProjectionMatrix(); camera.transform.SetPositionAndRotation(position, rotation);
            }
        }
        private static void ReleaseTexture(Surface surface)
        {
            if (!surface.Texture) return;
            if (surface.Material) surface.Material.SetTexture(Main, null);
            surface.Texture.Release(); Object.Destroy(surface.Texture); surface.Texture = null;
        }
        internal void Reset()
        {
            commands?.Clear();
            for (int i = 0; i < surfaces.Length; i++)
            { Restore((BodyPart)i); ReleaseTexture(surfaces[i]); if (surfaces[i].Material) Object.Destroy(surfaces[i].Material); surfaces[i].Material = null; }
            bindings.Clear(); AvailableMask = 0; nextScan = 0; failed = false;
        }
        public void Dispose()
        {
            Reset(); commands?.Release(); commands = null;
            foreach (var material in layerMaterials) if (material) Object.Destroy(material);
            layerMaterials.Clear();
        }
    }
}
