using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Wheels.Rims
{
    internal sealed class StockTireGeometry : IDisposable
    {
        internal Mesh Mesh { get; private set; }
        internal Vector3 Center { get; private set; }
        internal float OuterRadius { get; private set; }
        internal float InnerRadius { get; private set; }
        private Vector3[] original;
        private Mesh source;
        private bool[] used;
        private float appliedRadius = -1;

        internal static StockTireGeometry Create(Mesh source)
        {
            if (!source || !source.isReadable) return null;
            var vertices = source.vertices;
            if (vertices.Length < 12) return null;
            var center = source.bounds.center;
            var parent = new int[vertices.Length]; var radii = new float[vertices.Length];
            float outer = 0;
            for (int i = 0; i < vertices.Length; i++)
            { parent[i] = i; radii[i] = new Vector2(vertices[i].y - center.y, vertices[i].z - center.z).magnitude; outer = Mathf.Max(outer, radii[i]); }
            if (outer < .001f) return null;
            var triangles = new int[source.subMeshCount][];
            for (int s = 0; s < triangles.Length; s++)
            {
                triangles[s] = source.GetTriangles(s);
                for (int t = 0; t < triangles[s].Length; t += 3)
                { int a = Find(parent, triangles[s][t]); parent[Find(parent, triangles[s][t + 1])] = a; parent[Find(parent, triangles[s][t + 2])] = a; }
            }
            var min = new float[vertices.Length]; var max = new float[vertices.Length];
            for (int i = 0; i < min.Length; i++) min[i] = float.MaxValue;
            for (int i = 0; i < vertices.Length; i++) { int p = Find(parent, i); min[p] = Mathf.Min(min[p], radii[i]); max[p] = Mathf.Max(max[p], radii[i]); }
            var kept = new bool[vertices.Length]; var selected = new int[triangles.Length][];
            int count = 0; float inner = outer;
            for (int s = 0; s < triangles.Length; s++)
            {
                var list = new List<int>();
                for (int t = 0; t < triangles[s].Length; t += 3)
                {
                    int p = Find(parent, triangles[s][t]);
                    if (max[p] < outer * .88f || min[p] < outer * .3f) continue;
                    for (int j = 0; j < 3; j++) { int v = triangles[s][t + j]; list.Add(v); kept[v] = true; inner = Mathf.Min(inner, radii[v]); }
                }
                count += list.Count; selected[s] = list.ToArray();
            }
            if (count < 72 || inner >= outer * .98f) return null;
            var mesh = Object.Instantiate(source); mesh.name = "ECT.Tire." + source.name;
            for (int s = 0; s < selected.Length; s++) mesh.SetTriangles(selected[s], s, false);
            mesh.bounds = source.bounds;
            return new StockTireGeometry { source = source, Mesh = mesh, Center = center, OuterRadius = outer, InnerRadius = inner, original = vertices, used = kept };
        }
        internal static StockTireGeometry FromTire(Mesh source)
        {
            if (!source || !source.isReadable) return null;
            var vertices = source.vertices; var used = new bool[vertices.Length]; var center = source.bounds.center;
            foreach (int index in source.triangles) used[index] = true;
            float inner = float.MaxValue, outer = 0;
            for (int i = 0; i < vertices.Length; i++) if (used[i])
            {
                float r = new Vector2(vertices[i].y - center.y, vertices[i].z - center.z).magnitude;
                inner = Mathf.Min(inner, r); outer = Mathf.Max(outer, r);
            }
            if (outer < .001f || inner >= outer * .98f) return null;
            var mesh = Object.Instantiate(source); mesh.name = "ECT.CustomTire." + source.name;
            return new StockTireGeometry { source = source, Mesh = mesh, Center = center, OuterRadius = outer, InnerRadius = inner, original = vertices, used = used };
        }
        internal Mesh CreateRimMesh()
        {
            var mesh = Object.Instantiate(source); mesh.name = "ECT.StockRim." + source.name;
            for (int s = 0; s < source.subMeshCount; s++)
            {
                var t = source.GetTriangles(s); var keep = new List<int>();
                for (int i = 0; i < t.Length; i += 3) if (!used[t[i]]) { keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]); }
                mesh.SetTriangles(keep, s, false);
            }
            mesh.bounds = source.bounds; return mesh;
        }
        private static int Find(int[] parent, int i)
        { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }
        internal void FitOpening(float radius)
        {
            radius = Mathf.Clamp(radius, OuterRadius * .3f, OuterRadius * .94f);
            if (Mathf.Abs(appliedRadius - radius) < .00001f) return;
            appliedRadius = radius;
            var vertices = (Vector3[])original.Clone();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (!used[i]) continue;
                var delta = vertices[i] - Center; float r = new Vector2(delta.y, delta.z).magnitude;
                float target = Mathf.Lerp(radius, OuterRadius, Mathf.InverseLerp(InnerRadius, OuterRadius, r));
                vertices[i].y = Center.y + delta.y * target / Mathf.Max(r, .00001f);
                vertices[i].z = Center.z + delta.z * target / Mathf.Max(r, .00001f);
            }
            Mesh.vertices = vertices;
        }
        public void Dispose() { if (Mesh) Object.Destroy(Mesh); Mesh = null; }
    }
}
