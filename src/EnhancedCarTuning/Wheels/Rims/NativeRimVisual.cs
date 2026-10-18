using System;
using System.Collections.Generic;
using EnhancedCarTuning.Wheels.Models;
using EnhancedCarTuning.Wheels.Suspension;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Wheels.Rims
{
    internal sealed class NativeRimVisual : IDisposable
    {
        private static readonly HashSet<NativeRimVisual> live = new HashSet<NativeRimVisual>();
        private bool disposed;
        private float nextMaterialRefresh;
        private readonly CarPhWheel wheel;
        private readonly MeshRenderer original;
        private readonly bool originallyEnabled;
        private readonly StockTireGeometry tire;
        private StockTireGeometry customTire;
        private readonly GameObject root;
        private GameObject customTireObject;
        private readonly MeshRenderer rubber;
        private MeshRenderer stockRim;
        private Mesh stockRimMesh;
        private RimVisual rim;
        private readonly LODGroup lod;
        private readonly LOD[] originalLods;
        private readonly Vector3 referenceScale;
        private readonly WheelModelPart donor;
        private readonly MaterialPropertyBlock finish = new MaterialPropertyBlock();
        private float customTireScale;
        internal string ModelId { get; }
        internal string TireId { get; }
        internal Renderer[] Renderers { get; private set; }
        internal Mesh Source { get; }
        internal float DiameterCm { get; private set; }
        internal float WidthCm { get; private set; }
        internal float MaxDiameterCm => tire.OuterRadius * .94f * 200 * Mathf.Abs(wheel.transform.lossyScale.y);

        private NativeRimVisual(CarPhWheel wheel, MeshRenderer original, Mesh mesh, StockTireGeometry tire,
            string id, string tireId, Vector3 referenceScale, WheelModelPart donor, GameObject tirePrefab)
        {
            this.wheel = wheel; this.original = original; this.tire = tire; this.donor = donor;
            this.referenceScale = referenceScale; Source = mesh; ModelId = id; TireId = tireId;
            originallyEnabled = original.enabled;
            root = new GameObject("ECT.RimWheel"); root.SetActive(false); root.layer = wheel.gameObject.layer;
            root.transform.SetParent(wheel.transform, false);
            try
            {
                if (donor != null)
                {
                    if (!WheelModelGeometry.Fit(mesh.bounds, donor.Mesh.bounds, out var scale, out var offset)) throw new InvalidOperationException("Cannot fit donor wheel.");
                    root.transform.localScale = scale; root.transform.localPosition = offset;
                }
                var rubberObject = new GameObject("Stock tire"); rubberObject.layer = root.layer;
                rubberObject.transform.SetParent(root.transform, false);
                rubberObject.AddComponent<MeshFilter>().sharedMesh = tire.Mesh;
                rubber = rubberObject.AddComponent<MeshRenderer>(); rubber.sharedMaterials = donor == null ? original.sharedMaterials : donor.Materials;
                rubber.shadowCastingMode = original.shadowCastingMode; rubber.receiveShadows = original.receiveShadows;
                var renderers = new List<Renderer>();
                if (id.StartsWith("rim_", StringComparison.Ordinal))
                {
                    rim = new RimVisual(wheel.transform, original);
                    if (!rim.Replace(id, wheel.IsLeft)) throw new InvalidOperationException("Cannot load rim " + id);
                    rim.Instance.transform.localPosition = tire.Center;
                    renderers.AddRange(rim.Instance.GetComponentsInChildren<MeshRenderer>(true));
                }
                else
                {
                    var stock = new GameObject("Stock rim"); stock.layer = root.layer; stock.transform.SetParent(root.transform, false);
                    stockRimMesh = tire.CreateRimMesh(); stock.AddComponent<MeshFilter>().sharedMesh = stockRimMesh;
                    stockRim = stock.AddComponent<MeshRenderer>(); stockRim.sharedMaterials = donor == null ? original.sharedMaterials : donor.Materials;
                    stockRim.shadowCastingMode = original.shadowCastingMode; stockRim.receiveShadows = original.receiveShadows;
                    renderers.Add(stockRim);
                }
                if (tirePrefab)
                {
                    customTireObject = Object.Instantiate(tirePrefab, root.transform, false); customTireObject.name = "ECT.Tire." + tireId;
                    customTireObject.transform.localPosition = tire.Center;
                    foreach (var tr in customTireObject.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = root.layer;
                    var filter = customTireObject.GetComponentInChildren<MeshFilter>();
                    customTire = StockTireGeometry.FromTire(filter.sharedMesh);
                    if (customTire == null) throw new InvalidOperationException("Invalid separate tire geometry: " + tireId);
                    filter.sharedMesh = customTire.Mesh;
                    customTireScale = tire.OuterRadius / customTire.OuterRadius;
                    float width = (donor == null ? mesh : donor.Mesh).bounds.size.x;
                    customTireObject.transform.localScale = new Vector3(width / Mathf.Max(customTire.Mesh.bounds.size.x, .001f), customTireScale, customTireScale);
                    rubberObject.SetActive(false);
                    renderers.AddRange(customTireObject.GetComponentsInChildren<MeshRenderer>(true));
                }
                else renderers.Add(rubber);
                Renderers = renderers.ToArray();
                lod = wheel.GetComponent<LODGroup>();
                if (lod)
                {
                    originalLods = lod.GetLODs(); var replacement = lod.GetLODs();
                    for (int i = 0; i < replacement.Length; i++)
                    {
                        var list = new List<Renderer>();
                        foreach (var renderer in replacement[i].renderers)
                            if (renderer == original) list.AddRange(Renderers); else list.Add(renderer);
                        replacement[i].renderers = list.ToArray();
                    }
                    lod.SetLODs(replacement);
                }
                original.enabled = false; root.SetActive(true); live.Add(this);
            }
            catch { Dispose(); throw; }
        }
        internal static NativeRimVisual Create(CarPhWheel wheel, MeshFilter filter, MeshRenderer renderer, string id,
            string tireId = "stock", Vector3? referenceScale = null, WheelModelPart donor = null)
        {
            if (id.StartsWith("rim_", StringComparison.Ordinal) && (!EmbeddedAssets.TryGetRim(id, out _) || !EmbeddedAssets.RimPrefab(id, wheel.IsLeft))) return null;
            var prefab = tireId == "stock" ? null : EmbeddedAssets.TirePrefab(tireId, wheel.IsLeft);
            if (tireId != "stock" && !prefab) return null;
            var source = donor == null ? filter.sharedMesh : donor.Mesh;
            var tire = StockTireGeometry.Create(source);
            if (tire == null) { Plugin.Warn("Cannot separate tire from wheel mesh: " + source.name); return null; }
            try { return new NativeRimVisual(wheel, renderer, filter.sharedMesh, tire, id, tireId, referenceScale ?? wheel.transform.lossyScale, donor, prefab); }
            catch { tire.Dispose(); throw; }
        }
        internal void Refresh(AxleSettings settings, CarMaterial material, float killed)
        {
            if (!root || !wheel || !original) return;
            original.enabled = false;
            if (Time.unscaledTime >= nextMaterialRefresh)
            {
                nextMaterialRefresh = Time.unscaledTime + .5f;
                if (donor == null) { rubber.sharedMaterials = original.sharedMaterials; if (stockRim) stockRim.sharedMaterials = original.sharedMaterials; }
                foreach (var renderer in Renderers) if (renderer) renderer.sortingOrder = original.sortingOrder;
            }
            float radius = rim == null ? tire.InnerRadius : settings.Get(WheelParameter.RimDiameter).HasValue ?
                settings.Get(WheelParameter.RimDiameter).Value / (200 * Mathf.Max(Mathf.Abs(referenceScale.y), .0001f)) : tire.OuterRadius * .72f;
            radius = Mathf.Clamp(radius, tire.OuterRadius * .3f, tire.OuterRadius * .94f);
            float width = settings.Get(WheelParameter.RimWidth).HasValue ?
                settings.Get(WheelParameter.RimWidth).Value / (100 * Mathf.Max(Mathf.Abs(referenceScale.x), .0001f)) : Source.bounds.size.x;
            if (rim != null) { rim.SetLocalSize(radius, width); tire.FitOpening(radius); }
            if (customTire != null) customTire.FitOpening(radius / customTireScale);
            DiameterCm = radius * 200 * Mathf.Abs(wheel.transform.lossyScale.y);
            WidthCm = width * 100 * Mathf.Abs(wheel.transform.lossyScale.x);
            var color = material && material.wheel != null ? material.wheel.color : Color.white;
            if (killed > .5f) color *= .25f;
            if (rim != null) rim.SetFinish(color, material && material.wheel != null ? material.wheel.Gloss : .5f, material && material.wheel != null ? material.wheel.Mettalic : .5f);
            if (stockRim && material && material.wheel != null)
            {
                finish.SetColor("_Color", color); finish.SetFloat("_Gloss", material.wheel.Gloss);
                finish.SetFloat("_Mettalic", material.wheel.Mettalic); finish.SetFloat("_Killed", killed);
                stockRim.SetPropertyBlock(finish);
            }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; live.Remove(this);
            if (root) root.SetActive(false);
            if (lod && originalLods != null) lod.SetLODs(originalLods);
            rim?.Dispose();
            if (original) original.enabled = originallyEnabled;
            if (root) Object.Destroy(root);
            if (stockRimMesh) Object.Destroy(stockRimMesh);
            customTire?.Dispose(); tire.Dispose();
        }
        internal static void ClearAll() { foreach (var visual in new List<NativeRimVisual>(live)) visual.Dispose(); }
    }
}
