using System;
using System.Collections.Generic;
using EnhancedCarTuning.Wheels.Suspension;
using EnhancedCarTuning.Wheels.Rims;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Wheels.Models
{
    internal sealed class WheelModelService : IDisposable
    {
        private sealed class Slot
        {
            internal CarPhWheel Wheel;
            internal MeshFilter OriginalMesh;
            internal MeshRenderer OriginalRenderer, Renderer;
            internal LODGroup Lod;
            internal LOD[] OriginalLods;
            internal bool OriginalEnabled;
            internal GameObject Visual;
            internal Material[] Materials;
            internal bool[] Paintable;
            internal string Model = "stock";
            internal float Killed;
            internal NativeRimVisual Rim;
            internal StockRimGeometry StockRim;
            internal Mesh Source, MeasuredMesh;
            internal float InnerRadius, OuterRadius;
            internal bool CanResize;
            internal string FailedModel;
            internal Mesh FailedMesh;
            internal Vector3 ReferenceScale;
        }
        private readonly List<Slot> slots = new List<Slot>();
        private readonly CarControl car;
        private readonly CarMaterial material;
        private WheelSettings settings = WheelSettings.Stock;
        private float nextResolve;
        internal WheelModelService(CarControl car)
        {
            this.car = car; material = car.GetComponent<CarMaterial>();
            if (NetManager.isServer) return;
            foreach (var wheel in car.ph_car.wheels)
            {
                if (!wheel) continue;
                var mesh = wheel.GetComponent<MeshFilter>(); var renderer = wheel.GetComponent<MeshRenderer>();
                if (!mesh || !renderer) continue;
                slots.Add(new Slot { Wheel = wheel, OriginalMesh = mesh, OriginalRenderer = renderer, ReferenceScale = wheel.transform.lossyScale });
            }
        }
        internal void Apply(WheelSettings next)
        {
            settings = next; nextResolve = 0; Refresh();
        }
        internal void Refresh()
        {
            if (!car || NetManager.isServer) return;
            bool resolve = Time.unscaledTime >= nextResolve;
            if (resolve) nextResolve = Time.unscaledTime + .5f;
            foreach (var slot in slots)
            {
                if (!slot.Wheel || !slot.OriginalRenderer) continue;
                string id = settings.For(slot.Wheel.IsBack).ModelId;
                var axle = settings.For(slot.Wheel.IsBack);
                bool customRim = id.StartsWith("rim_", StringComparison.Ordinal);
                bool splitWheel = customRim;
                bool resizeStock = !customRim && axle.Get(WheelParameter.RimDiameter).HasValue;
                if (slot.Rim != null && (slot.Rim.Source != slot.OriginalMesh.sharedMesh || slot.Rim.ModelId != id)) Restore(slot);
                if (slot.Visual && (slot.Source != slot.OriginalMesh.sharedMesh || (slot.StockRim != null && !resizeStock))) Restore(slot);
                if ((id != slot.Model || splitWheel && slot.Rim == null || resizeStock && !slot.Visual) && (id == "stock" || resolve))
                {
                    if (splitWheel && slot.OriginalMesh.sharedMesh)
                    {
                        if (slot.FailedModel != id || slot.FailedMesh != slot.OriginalMesh.sharedMesh)
                        {
                            Restore(slot);
                            try { slot.Rim = NativeRimVisual.Create(slot.Wheel, slot.OriginalMesh, slot.OriginalRenderer, id, slot.ReferenceScale); }
                            catch (Exception error) { Plugin.Warn("Cannot replace wheel rim: " + error.Message); }
                            if (slot.Rim != null) { slot.Model = id; slot.FailedModel = null; slot.FailedMesh = null; }
                            else { slot.FailedModel = id; slot.FailedMesh = slot.OriginalMesh.sharedMesh; }
                        }
                    }
                    else if (id == "stock")
                    {
                        if (resizeStock) Replace(slot, StockPart(slot), id, true); else Restore(slot);
                    }
                    else if (slot.OriginalMesh.sharedMesh && WheelModelCatalog.TryGet(id, out var option)) Replace(slot, option.For(WheelSideResolver.NativeVariant(slot.Wheel)), id, resizeStock);
                }
                if (slot.Rim != null) { slot.Rim.Refresh(settings.For(slot.Wheel.IsBack), material, slot.Killed); continue; }
                // A previously selected native donor can already be visible when the
                // first radius edit arrives. Deform its owned copy, never its shared asset.
                if (resizeStock && slot.Visual && slot.StockRim == null && slot.MeasuredMesh && slot.CanResize)
                {
                    slot.StockRim = StockRimGeometry.Create(slot.MeasuredMesh);
                    if (slot.StockRim != null) slot.Visual.GetComponent<MeshFilter>().sharedMesh = slot.StockRim.Mesh;
                }
                if (slot.StockRim != null)
                {
                    float worldScale = Mathf.Abs(slot.Visual.transform.lossyScale.y);
                    float radius = axle.Get(WheelParameter.RimDiameter).Value / (200 * Mathf.Max(worldScale, .0001f));
                    slot.StockRim.Fit(radius);
                }
                if (!slot.Renderer) continue;
                slot.OriginalRenderer.enabled = false;
                slot.Renderer.sortingOrder = slot.OriginalRenderer.sortingOrder;
                for (int i = 0; i < slot.Materials.Length; i++)
                {
                    var m = slot.Materials[i]; if (!m) continue;
                    if (slot.Paintable[i] && material && material.wheel != null)
                    {
                        if (m.HasProperty("_Color")) m.color = material.wheel.color;
                        if (m.HasProperty("_Mettalic")) m.SetFloat("_Mettalic", material.wheel.Mettalic);
                        if (m.HasProperty("_Gloss")) m.SetFloat("_Gloss", Mathf.Min(material.wheel.Gloss, .98f));
                    }
                    if (m.HasProperty("_Killed")) m.SetFloat("_Killed", slot.Killed);
                }
            }
        }
        private WheelModelPart StockPart(Slot slot)
        {
            var materials = slot.OriginalRenderer.sharedMaterials; var paintable = new bool[materials.Length];
            for (int i = 0; i < materials.Length; i++)
                paintable[i] = materials[i] && (material && material.wheel != null && (materials[i] == material.wheel.mat || materials[i] == material.wheel.usedMatreial) ||
                    materials[i].shader && materials[i].shader.name.StartsWith("Custom/Car/CarWheel", StringComparison.Ordinal));
            return new WheelModelPart { Mesh = slot.OriginalMesh.sharedMesh, Materials = materials, Paintable = paintable };
        }
        private static void Measure(Slot slot, Mesh source)
        {
            if (slot.MeasuredMesh == source) return;
            slot.MeasuredMesh = source;
            slot.CanResize = StockTireGeometry.MeasureRim(source, out slot.InnerRadius, out slot.OuterRadius);
        }
        private void Replace(Slot slot, WheelModelPart donor, string requestedId, bool resize)
        {
            if (!donor.Mesh || !WheelModelGeometry.Fit(slot.OriginalMesh.sharedMesh.bounds, donor.Mesh.bounds, out var scale, out var offset)) return;
            Restore(slot);
            Measure(slot, donor.Mesh);
            slot.Source = slot.OriginalMesh.sharedMesh;
            slot.OriginalEnabled = slot.OriginalRenderer.enabled;
            slot.Visual = new GameObject("ECT.WheelModel");
            slot.Visual.SetActive(false); slot.Visual.layer = slot.Wheel.gameObject.layer;
            slot.Visual.transform.SetParent(slot.Wheel.transform, false);
            slot.Visual.transform.localScale = scale; slot.Visual.transform.localPosition = offset;
            slot.Visual.AddComponent<MeshFilter>().sharedMesh = donor.Mesh;
            if (resize && slot.CanResize)
            {
                slot.StockRim = StockRimGeometry.Create(donor.Mesh);
                if (slot.StockRim != null) slot.Visual.GetComponent<MeshFilter>().sharedMesh = slot.StockRim.Mesh;
            }
            slot.Renderer = slot.Visual.AddComponent<MeshRenderer>();
            slot.Materials = new Material[donor.Materials.Length];
            for (int i = 0; i < slot.Materials.Length; i++) if (donor.Materials[i]) slot.Materials[i] = new Material(donor.Materials[i]);
            slot.Paintable = donor.Paintable;
            slot.Renderer.sharedMaterials = slot.Materials;
            slot.Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            slot.Renderer.receiveShadows = slot.OriginalRenderer.receiveShadows;
            slot.Renderer.sortingOrder = slot.OriginalRenderer.sortingOrder;
            slot.Lod = slot.Wheel.GetComponent<LODGroup>();
            if (slot.Lod)
            {
                slot.OriginalLods = slot.Lod.GetLODs();
                var replacement = slot.Lod.GetLODs();
                for (int i = 0; i < replacement.Length; i++)
                {
                    var renderers = (Renderer[])replacement[i].renderers.Clone();
                    for (int j = 0; j < renderers.Length; j++) if (renderers[j] == slot.OriginalRenderer) renderers[j] = slot.Renderer;
                    replacement[i].renderers = renderers;
                }
                slot.Lod.SetLODs(replacement);
            }
            slot.Model = requestedId; slot.OriginalRenderer.enabled = false;
            slot.Visual.SetActive(true);
        }
        private static void Restore(Slot slot)
        {
            slot.Rim?.Dispose(); slot.Rim = null;
            slot.StockRim?.Dispose(); slot.StockRim = null; slot.Source = null;
            slot.FailedModel = null; slot.FailedMesh = null;
            if (slot.Visual)
            {
                slot.Visual.SetActive(false);
                if (slot.OriginalRenderer) slot.OriginalRenderer.enabled = slot.OriginalEnabled;
                if (slot.Lod && slot.OriginalLods != null) slot.Lod.SetLODs(slot.OriginalLods);
                foreach (var m in slot.Materials) if (m) DestroyOwned(m);
                DestroyOwned(slot.Visual);
            }
            slot.Visual = null; slot.Renderer = null; slot.Materials = null; slot.Paintable = null;
            slot.Lod = null; slot.OriginalLods = null; slot.Model = "stock";
        }
        private static void DestroyOwned(Object value)
        { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
        internal void SetKilled(CarWheel wheel, float value)
        {
            foreach (var slot in slots) if (slot.Wheel && slot.Wheel.gameObject == wheel.gameObject) slot.Killed = value;
            Refresh();
        }
        internal bool ReadRimSize(CarPhWheel wheel, WheelParameter parameter, out float value, out float max)
        {
            value = 0; max = WheelLimits.Max(parameter);
            foreach (var slot in slots) if (slot.Wheel == wheel)
            {
                if (slot.Rim != null)
                {
                    value = parameter == WheelParameter.RimDiameter ? slot.Rim.DiameterCm : slot.Rim.WidthCm;
                    if (parameter == WheelParameter.RimDiameter) max = Mathf.Min(max, slot.Rim.MaxDiameterCm);
                    return true;
                }
                string id = settings.For(wheel.IsBack).ModelId;
                if (id.StartsWith("rim_", StringComparison.Ordinal)) return false;
                Mesh source = slot.OriginalMesh.sharedMesh;
                if (id != "stock")
                { if (!WheelModelCatalog.TryGet(id, out var option)) return false; source = option.For(WheelSideResolver.NativeVariant(wheel)).Mesh; }
                Measure(slot, source);
                if (!slot.CanResize || !WheelModelGeometry.Fit(slot.OriginalMesh.sharedMesh.bounds, source.bounds, out var fit, out _)) return false;
                var scale = Vector3.Scale(wheel.transform.lossyScale, fit);
                value = parameter == WheelParameter.RimDiameter ? (slot.StockRim != null ? slot.StockRim.Radius : slot.InnerRadius) * 200 * Mathf.Abs(scale.y) : source.bounds.size.x * 100 * Mathf.Abs(scale.x);
                if (parameter == WheelParameter.RimDiameter) max = Mathf.Min(max, slot.OuterRadius * .94f * 200 * Mathf.Abs(scale.y));
                // Native wheel width remains the tire's full width; only radius is editable.
                return parameter == WheelParameter.RimDiameter;
            }
            return false;
        }
        public void Dispose() { foreach (var slot in slots) Restore(slot); slots.Clear(); }
    }
}
