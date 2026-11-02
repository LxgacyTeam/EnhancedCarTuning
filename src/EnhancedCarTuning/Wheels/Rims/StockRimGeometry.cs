using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Wheels.Rims
{
    // Keep the native mesh's topology, UVs, submeshes and materials. A continuous
    // radial mapping scales the metal core and reshapes its tire without opening seams.
    internal sealed class StockRimGeometry : IDisposable
    {
        internal Mesh Mesh { get; private set; }
        internal float InnerRadius { get; private set; }
        internal float OuterRadius { get; private set; }
        internal float Radius { get; private set; }
        private Vector3 center;
        private Vector3[] original, normals;
        private StockRimGeometry() { }
        internal static StockRimGeometry Create(Mesh source)
        {
            if (!StockTireGeometry.MeasureRim(source, out float inner, out float outer)) return null;
            return new StockRimGeometry { Mesh = Object.Instantiate(source), InnerRadius = inner, Radius = inner,
                OuterRadius = outer, center = source.bounds.center, original = source.vertices, normals = source.normals };
        }
        internal void Fit(float radius)
        {
            radius = Mathf.Clamp(radius, OuterRadius * .3f, OuterRadius * .94f);
            if (Mathf.Abs(Radius - radius) < .000001f) return;
            Radius = radius;
            var vertices = new Vector3[original.Length];
            var adjustedNormals = normals.Length == original.Length ? new Vector3[normals.Length] : null;
            for (int i = 0; i < vertices.Length; i++)
            {
                var delta = original[i] - center; float r = new Vector2(delta.y, delta.z).magnitude;
                bool core = r <= InnerRadius;
                float mapped = core ? r * radius / InnerRadius : Mathf.Lerp(radius, OuterRadius, Mathf.InverseLerp(InnerRadius, OuterRadius, r));
                float factor = r > .000001f ? mapped/r : radius/InnerRadius;
                vertices[i] = new Vector3(original[i].x, center.y + delta.y*factor, center.z + delta.z*factor);
                if (adjustedNormals != null)
                {
                    // Inverse transpose of the local radial deformation; preserve smooth
                    // normals across existing UV seams rather than recalculating flat faces.
                    var n = normals[i]; var radial = r > .000001f ? new Vector3(0,delta.y/r,delta.z/r) : Vector3.up;
                    float slope = core ? radius/InnerRadius : (OuterRadius-radius)/(OuterRadius-InnerRadius);
                    var nr = radial * Vector3.Dot(n,radial); var nx = new Vector3(n.x,0,0);
                    adjustedNormals[i] = (nx + nr/Mathf.Max(slope,.000001f) + (n-nx-nr)/Mathf.Max(factor,.000001f)).normalized;
                }
            }
            Mesh.vertices = vertices;
            if (adjustedNormals != null) Mesh.normals = adjustedNormals;
            Mesh.RecalculateBounds(); if (Mesh.uv.Length == vertices.Length) Mesh.RecalculateTangents();
        }
        public void Dispose()
        {
            if (Mesh) { if (Application.isPlaying) Object.Destroy(Mesh); else Object.DestroyImmediate(Mesh); }
            Mesh = null;
        }
    }
}
