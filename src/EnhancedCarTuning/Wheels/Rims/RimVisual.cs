using System;
using EnhancedCarTuning.Appearance.Body;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Wheels.Rims
{
    internal sealed class RimVisual : IDisposable
    {
        private static readonly HashSet<RimVisual> live = new HashSet<RimVisual>();
        private readonly Transform carrier;
        private readonly MeshRenderer originalRim;
        private readonly bool originallyEnabled;
        private GameObject instance;
        private Vector3 baseScale;
        private float baseRadius;
        private float baseWidth;
        private MeshRenderer[] renderers;
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        private PaintFinish lastOuter, lastInner;
        private float lastKilled = -1;
        private float lastRadius = -1, lastWidth = -1;
        private bool disposed;
        internal string ModelId { get; private set; }
        internal GameObject Instance => instance;

        internal RimVisual(Transform rotatingCarrier, MeshRenderer separateOriginalRim)
        {
            if (!rotatingCarrier || !separateOriginalRim || !separateOriginalRim.transform.IsChildOf(rotatingCarrier))
                throw new ArgumentException("A rotating carrier and its separate stock rim renderer are required.");
            carrier = rotatingCarrier; originalRim = separateOriginalRim; originallyEnabled = separateOriginalRim.enabled;
            live.Add(this);
        }
        internal bool Replace(string id,bool left)
        {
            if (disposed || !carrier || !originalRim || NetManager.isServer) return false;
            if (!EmbeddedAssets.TryGetRim(id,out var model)) return false;
            var prefab = EmbeddedAssets.RimPrefab(id,left); if (!prefab) return false;
            var next = Object.Instantiate(prefab,carrier,false);
            next.name = "ECT.Rim." + id; next.transform.localPosition = Vector3.zero; next.transform.localRotation = Quaternion.identity;
            foreach (var node in next.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = originalRim.gameObject.layer;
            var previous = instance; instance = next; ModelId = id; baseScale = next.transform.localScale;
            baseRadius = left ? model.LeftRadius : model.RightRadius;
            renderers = next.GetComponentsInChildren<MeshRenderer>(true);
            float minX = float.MaxValue, maxX = float.MinValue;
            foreach (var filter in next.GetComponentsInChildren<MeshFilter>(true)) foreach (var vertex in filter.sharedMesh.vertices)
            {
                var point = next.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                minX = Mathf.Min(minX, point.x * baseScale.x); maxX = Mathf.Max(maxX, point.x * baseScale.x);
            }
            baseWidth = Mathf.Max(maxX - minX, .001f); lastOuter = lastInner = null; lastKilled = -1; lastRadius = lastWidth = -1;
            originalRim.enabled = false; next.SetActive(true);
            if (previous) { previous.SetActive(false); Object.Destroy(previous); }
            return true;
        }
        internal void SetDiameterCm(float diameter)
        {
            if (!instance) return;
            if (float.IsNaN(diameter) || float.IsInfinity(diameter) || diameter < 10 || diameter > 200)
                throw new ArgumentOutOfRangeException(nameof(diameter));
            float scale = diameter / (200 * baseRadius);
            instance.transform.localScale = new Vector3(baseScale.x,baseScale.y * scale,baseScale.z * scale);
        }
        internal void SetFinish(PaintFinish outer, PaintFinish inner, float killed)
        {
            if (!instance || Equals(lastOuter, outer) && Equals(lastInner, inner) && lastKilled == killed) return;
            lastOuter = outer; lastInner = inner; lastKilled = killed;
            foreach (var renderer in renderers)
            {
                if (!renderer) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i]; if (!material) continue;
                    PaintFinish finish = material.name.StartsWith("Outer", StringComparison.Ordinal) ? outer : material.name.StartsWith("Inner", StringComparison.Ordinal) ? inner : null;
                    if (finish == null) continue;
                    var color = finish.Color; if (killed > .5f) color *= .25f; color.a = 1;
                    properties.Clear(); properties.SetColor("_Color", color);
                    properties.SetFloat("_Glossiness", finish.Gloss); properties.SetFloat("_Metallic", finish.Metallic);
                    renderer.SetPropertyBlock(properties, i);
                }
            }
        }
        internal void SetLocalSize(float radius, float width)
        {
            if (!instance) return;
            if (lastRadius == radius && lastWidth == width) return;
            lastRadius = radius; lastWidth = width;
            float scale = radius / baseRadius;
            instance.transform.localScale = new Vector3(baseScale.x * width / baseWidth, baseScale.y * scale, baseScale.z * scale);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; live.Remove(this);
            if (instance) { instance.SetActive(false); Object.Destroy(instance); } instance = null; ModelId = null;
            if (originalRim) originalRim.enabled = originallyEnabled;
        }
        internal static void ClearAll() { foreach (var visual in new List<RimVisual>(live)) visual.Dispose(); }
    }
}
