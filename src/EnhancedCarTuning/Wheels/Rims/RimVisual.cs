using System;
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
        private Color lastColor;
        private float lastGloss = -1, lastMetallic = -1;
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
            baseWidth = Mathf.Max(maxX - minX, .001f); lastGloss = -1; lastRadius = lastWidth = -1;
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
        internal void SetFinish(Color color,float gloss,float metallic)
        {
            if (!instance) return;
            if (lastGloss == gloss && lastMetallic == metallic && lastColor == color) return;
            lastColor = color; lastGloss = gloss; lastMetallic = metallic;
            properties.SetColor("_Color",new Color(color.r,color.g,color.b,1));
            properties.SetFloat("_Glossiness",Mathf.Clamp01(gloss)); properties.SetFloat("_Metallic",Mathf.Clamp01(metallic));
            foreach (var renderer in renderers) if (renderer) renderer.SetPropertyBlock(properties);
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
