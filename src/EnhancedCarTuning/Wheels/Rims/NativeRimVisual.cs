using System;
using System.Collections.Generic;
using EnhancedCarTuning.Wheels.Suspension;
using EnhancedCarTuning.Appearance.Body;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Wheels.Rims
{
    // Custom rims own a common deformable tire. Stock wheel sizing uses StockRimGeometry.
    internal sealed class NativeRimVisual : IDisposable
    {
        private static readonly HashSet<NativeRimVisual> live = new HashSet<NativeRimVisual>();
        private readonly CarPhWheel wheel;
        private readonly MeshRenderer original;
        private readonly bool originallyEnabled;
        private readonly LODGroup lod;
        private readonly LOD[] originalLods;
        private readonly float outerRadius, baseWidth;
        private PaintFinish fallback;
        private GameObject tireObject;
        private StockTireGeometry tire;
        private RimVisual rim;
        private bool disposed;
        internal string ModelId { get; }
        internal Renderer[] Renderers { get; private set; }
        internal Mesh Source { get; }
        internal float DiameterCm { get; private set; }
        internal float WidthCm { get; private set; }
        internal float MaxDiameterCm => outerRadius * .94f * 200 * Mathf.Abs(wheel.transform.lossyScale.y);

        private NativeRimVisual(CarPhWheel wheel, MeshFilter filter, MeshRenderer renderer, string id, Vector3 referenceScale, bool leftVariant)
        {
            this.wheel = wheel; original = renderer; Source = filter.sharedMesh; ModelId = id;
            originallyEnabled = original.enabled;
            var bounds = Source.bounds; var center = bounds.center;
            outerRadius = Mathf.Max(bounds.extents.y, bounds.extents.z); baseWidth = bounds.size.x;
            if (outerRadius < .001f || baseWidth < .001f) throw new InvalidOperationException("Invalid source wheel bounds.");
            try
            {
                var prefab = EmbeddedAssets.TirePrefab(leftVariant);
                if (!prefab) throw new InvalidOperationException("Shared tire prefab is unavailable.");
                tireObject = Object.Instantiate(prefab, wheel.transform, false); tireObject.name = "ECT.SharedTire";
                tireObject.transform.localPosition = center; tireObject.transform.localRotation = Quaternion.identity;
                foreach (var node in tireObject.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = original.gameObject.layer;
                var tireFilter = tireObject.GetComponent<MeshFilter>();
                tire = tireFilter ? StockTireGeometry.FromTire(tireFilter.sharedMesh) : null;
                if (tire == null) throw new InvalidOperationException("Cannot deform shared tire mesh.");
                tireFilter.sharedMesh = tire.Mesh;
                float radial = outerRadius / tire.OuterRadius;
                tireObject.transform.localScale = new Vector3(baseWidth / Mathf.Max(tire.Mesh.bounds.size.x, .001f), radial, radial);
                rim = new RimVisual(wheel.transform, original);
                if (!rim.Replace(id, leftVariant)) throw new InvalidOperationException("Cannot instantiate rim " + id);
                rim.Instance.transform.localPosition = center;
                var renderers = new List<Renderer>(); renderers.AddRange(tireObject.GetComponentsInChildren<MeshRenderer>(true));
                renderers.AddRange(rim.Instance.GetComponentsInChildren<MeshRenderer>(true)); Renderers = renderers.ToArray();
                foreach (var item in Renderers) { item.shadowCastingMode = item.name=="Rim"?UnityEngine.Rendering.ShadowCastingMode.On:original.shadowCastingMode; item.receiveShadows = original.receiveShadows; }
                lod = wheel.GetComponent<LODGroup>();
                if (lod)
                {
                    originalLods = lod.GetLODs(); var replacement = lod.GetLODs();
                    for (int i = 0; i < replacement.Length; i++)
                    {
                        var items = new List<Renderer>();
                        foreach (var item in replacement[i].renderers) if (item == original) items.AddRange(Renderers); else items.Add(item);
                        replacement[i].renderers = items.ToArray();
                    }
                    lod.SetLODs(replacement);
                }
                original.enabled = false; tireObject.SetActive(true); live.Add(this);
            }
            catch { Dispose(); throw; }
        }
        internal static NativeRimVisual Create(CarPhWheel wheel, MeshFilter filter, MeshRenderer renderer, string id, Vector3 referenceScale)
        {
            if (!wheel || !filter || !filter.sharedMesh || !renderer || !EmbeddedAssets.TryGetRim(id, out _)) return null;
            bool leftVariant=WheelSideResolver.CustomVariant(wheel);
            if(!EmbeddedAssets.RimPrefab(id,leftVariant))return null;
            return new NativeRimVisual(wheel, filter, renderer, id, referenceScale, leftVariant);
        }
        internal void Refresh(AxleSettings settings, CarMaterial material, float killed)
        {
            if (disposed || !wheel || !original || !tireObject || rim == null) return;
            original.enabled = false;
            var scale = wheel.transform.lossyScale;
            // Stored rim diameter is an absolute physical size, independent of outer wheel radius.
            float radius = RimSizing.LocalRadius(outerRadius, scale.y, settings.Get(WheelParameter.RimDiameter));
            float width = settings.Get(WheelParameter.RimWidth).HasValue ? settings.Get(WheelParameter.RimWidth).Value / (100 * Mathf.Max(Mathf.Abs(scale.x), .0001f)) : baseWidth;
            rim.SetLocalSize(radius, width);
            tire.FitOpening(radius * tire.OuterRadius / outerRadius);
            DiameterCm = radius * 200 * Mathf.Abs(scale.y); WidthCm = width * 100 * Mathf.Abs(scale.x);
            var mat = material ? material.wheel : null;
            var color = mat != null ? mat.color : Color.white;
            float metallic = mat != null ? mat.Mettalic : .5f, gloss = mat != null ? mat.Gloss : .5f;
            if (fallback == null || !fallback.SameColor(color) || Mathf.Abs(fallback.Metallic-metallic) > .001f || Mathf.Abs(fallback.Gloss-gloss) > .001f)
                fallback = new PaintFinish(color,metallic,gloss);
            rim.SetFinish(settings.OuterFinish ?? fallback, settings.InnerFinish ?? fallback, killed);
            foreach (var item in Renderers) if (item) item.sortingOrder = original.sortingOrder;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; live.Remove(this);
            if (tireObject) tireObject.SetActive(false);
            if (lod && originalLods != null) lod.SetLODs(originalLods);
            rim?.Dispose(); if (tireObject) Object.Destroy(tireObject); tire?.Dispose();
            if (original) original.enabled = originallyEnabled;
        }
        internal static void ClearAll() { foreach (var item in new List<NativeRimVisual>(live)) item.Dispose(); }
    }
}
