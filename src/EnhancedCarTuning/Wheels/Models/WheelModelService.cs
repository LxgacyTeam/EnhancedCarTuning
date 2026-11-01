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
                string tireId = settings.For(slot.Wheel.IsBack).TireId;
                bool customRim = id.StartsWith("rim_", StringComparison.Ordinal);
                bool splitWheel = customRim || tireId != "stock";
                if (slot.Rim != null && (slot.Rim.Source != slot.OriginalMesh.sharedMesh || slot.Rim.ModelId != id || slot.Rim.TireId != tireId)) Restore(slot);
                if ((id != slot.Model || splitWheel && slot.Rim == null) && (id == "stock" || resolve))
                {
                    if (splitWheel && slot.OriginalMesh.sharedMesh)
                    {
                        if (slot.FailedModel != id + "/" + tireId || slot.FailedMesh != slot.OriginalMesh.sharedMesh)
                        {
                            WheelModelPart donor = null;
                            if (!customRim && id != "stock")
                            {
                                if (!WheelModelCatalog.TryGet(id, out var donorModel)) continue;
                                donor = donorModel.For(slot.Wheel.IsLeft);
                            }
                            Restore(slot);
                            try { slot.Rim = NativeRimVisual.Create(slot.Wheel, slot.OriginalMesh, slot.OriginalRenderer, id, tireId, slot.ReferenceScale, donor); }
                            catch (Exception error) { Plugin.Warn("Cannot replace wheel rim: " + error.Message); }
                            if (slot.Rim != null) { slot.Model = id; slot.FailedModel = null; slot.FailedMesh = null; }
                            else { slot.FailedModel = id + "/" + tireId; slot.FailedMesh = slot.OriginalMesh.sharedMesh; }
                        }
                    }
                    else if (id == "stock") Restore(slot);
                    else if (slot.OriginalMesh.sharedMesh && WheelModelCatalog.TryGet(id, out var option)) Replace(slot, option, id);
                }
                if (slot.Rim != null) { slot.Rim.Refresh(settings.For(slot.Wheel.IsBack), material, slot.Killed); continue; }
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
        private void Replace(Slot slot, WheelModelOption option, string requestedId)
        {
            var donor = option.For(slot.Wheel.IsLeft);
            if (!donor.Mesh || !WheelModelGeometry.Fit(slot.OriginalMesh.sharedMesh.bounds, donor.Mesh.bounds, out var scale, out var offset)) return;
            Restore(slot);
            slot.OriginalEnabled = slot.OriginalRenderer.enabled;
            slot.Visual = new GameObject("ECT.WheelModel");
            slot.Visual.SetActive(false); slot.Visual.layer = slot.Wheel.gameObject.layer;
            slot.Visual.transform.SetParent(slot.Wheel.transform, false);
            slot.Visual.transform.localScale = scale; slot.Visual.transform.localPosition = offset;
            slot.Visual.AddComponent<MeshFilter>().sharedMesh = donor.Mesh;
            slot.Renderer = slot.Visual.AddComponent<MeshRenderer>();
            slot.Materials = new Material[donor.Materials.Length];
            for (int i = 0; i < slot.Materials.Length; i++) if (donor.Materials[i]) slot.Materials[i] = new Material(donor.Materials[i]);
            slot.Paintable = donor.Paintable;
            slot.Renderer.sharedMaterials = slot.Materials;
            slot.Renderer.shadowCastingMode = slot.OriginalRenderer.shadowCastingMode;
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
            slot.FailedModel = null; slot.FailedMesh = null;
            if (slot.Visual)
            {
                slot.Visual.SetActive(false);
                if (slot.OriginalRenderer) slot.OriginalRenderer.enabled = slot.OriginalEnabled;
                if (slot.Lod && slot.OriginalLods != null) slot.Lod.SetLODs(slot.OriginalLods);
                foreach (var m in slot.Materials) if (m) Object.Destroy(m);
                Object.Destroy(slot.Visual);
            }
            slot.Visual = null; slot.Renderer = null; slot.Materials = null; slot.Paintable = null;
            slot.Lod = null; slot.OriginalLods = null; slot.Model = "stock";
        }
        internal void SetKilled(CarWheel wheel, float value)
        {
            foreach (var slot in slots) if (slot.Wheel && slot.Wheel.gameObject == wheel.gameObject) slot.Killed = value;
            Refresh();
        }
        internal bool ReadRimSize(CarPhWheel wheel, WheelParameter parameter, out float value, out float max)
        {
            value = 0; max = WheelLimits.Max(parameter);
            foreach (var slot in slots) if (slot.Wheel == wheel && slot.Rim != null)
            {
                value = parameter == WheelParameter.RimDiameter ? slot.Rim.DiameterCm : slot.Rim.WidthCm;
                if (parameter == WheelParameter.RimDiameter) max = Mathf.Min(max, slot.Rim.MaxDiameterCm);
                return true;
            }
            return false;
        }
        public void Dispose() { foreach (var slot in slots) Restore(slot); slots.Clear(); }
    }
}
