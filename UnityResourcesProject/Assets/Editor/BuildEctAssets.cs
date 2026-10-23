using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Convert OBJ coordinates to Unity. Source rim L/R labels are reversed relative to
// the game's wheel selection; both rim and tire labels are reversed below.
public static class BuildEctAssets
{
    private const string Generated = "Assets/Generated";
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    [Serializable] private sealed class RimCatalog { public int version = 1; public RimEntry[] models; }
    [Serializable] private sealed class RimEntry
    { public string id, name, left, right; public int sourceModel; public float leftRadius, rightRadius; }
    private sealed class Obj
    {
        internal readonly List<Vector3> positions = new List<Vector3>(), normals = new List<Vector3>();
        internal readonly List<Vector2> uvs = new List<Vector2>();
        internal readonly Dictionary<string, List<string[]>> faces = new Dictionary<string, List<string[]>>();
        internal static Obj Read(string path)
        {
            var obj = new Obj(); string material = "Default";
            foreach (string line in File.ReadLines(path))
            {
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;
                switch (parts[0])
                {
                    case "v": obj.positions.Add(new Vector3(-F(parts[1]), F(parts[2]), F(parts[3]))); break;
                    case "vn": obj.normals.Add(new Vector3(-F(parts[1]), F(parts[2]), F(parts[3]))); break;
                    case "vt": obj.uvs.Add(new Vector2(F(parts[1]), F(parts[2]))); break;
                    case "usemtl": material = parts[1]; break;
                    case "f":
                        if (!obj.faces.ContainsKey(material)) obj.faces.Add(material, new List<string[]>());
                        for (int i = 2; i < parts.Length - 1; i++) obj.faces[material].Add(new[] { parts[1], parts[i], parts[i + 1] });
                        break;
                }
            }
            return obj;
        }
        private static float F(string value) { return float.Parse(value, Culture); }
        private static int Index(string value, int count)
        { int i = int.Parse(value, Culture); return i > 0 ? i - 1 : count + i; }
        internal Bounds BoundsFor(params string[] groups)
        {
            bool first = true; var bounds = new Bounds();
            foreach (var group in groups) foreach (var face in faces[group]) foreach (var key in face)
            {
                var point = positions[Index(key.Split('/')[0], positions.Count)];
                if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
            }
            if (first) throw new InvalidDataException("Empty model group");
            return bounds;
        }
        internal Mesh Mesh(string name, Vector3 center, params string[] groups)
        {
            var verts = new List<Vector3>(); var uv = new List<Vector2>(); var norm = new List<Vector3>();
            var map = new Dictionary<string, int>(); var triangles = new List<int[]>(); bool allNormals = true;
            foreach (string group in groups)
            {
                var indices = new List<int>();
                foreach (var face in faces[group])
                {
                    int[] tri = new int[3];
                    for (int j = 0; j < 3; j++)
                    {
                        string key = face[j]; int index;
                        if (!map.TryGetValue(key, out index))
                        {
                            var fields = key.Split('/'); index = verts.Count; map.Add(key, index);
                            verts.Add(positions[Index(fields[0], positions.Count)] - center);
                            uv.Add(fields.Length > 1 && fields[1].Length > 0 ? uvs[Index(fields[1], uvs.Count)] : Vector2.zero);
                            bool hasNormal = fields.Length > 2 && fields[2].Length > 0;
                            norm.Add(hasNormal ? normals[Index(fields[2], normals.Count)].normalized : Vector3.zero);
                            allNormals &= hasNormal;
                        }
                        tri[j] = index;
                    }
                    // Source tire exports can have reversed winding. Match the supplied normals.
                    var normal = norm[tri[0]] + norm[tri[1]] + norm[tri[2]];
                    if (Vector3.Dot(Vector3.Cross(verts[tri[1]] - verts[tri[0]], verts[tri[2]] - verts[tri[0]]), normal) < 0)
                    { int swap = tri[1]; tri[1] = tri[2]; tri[2] = swap; }
                    indices.AddRange(tri);
                }
                triangles.Add(indices.ToArray());
            }
            var mesh = new Mesh { name = name, indexFormat = verts.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(verts); mesh.SetUVs(0, uv); mesh.SetNormals(norm); mesh.subMeshCount = groups.Length;
            for (int i = 0; i < groups.Length; i++) mesh.SetTriangles(triangles[i], i, false);
            mesh.RecalculateBounds(); if (!allNormals) mesh.RecalculateNormals(); mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, Generated + "/" + name + ".asset"); return mesh;
        }
    }
    public static void BuildWindows() { Build(BuildTarget.StandaloneWindows64, "Windows"); }
    public static void BuildLinux() { Build(BuildTarget.StandaloneLinux64, "Linux"); }
    private static void Build(BuildTarget target, string platform)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target)) throw new InvalidOperationException("Missing Unity build support: " + target);
        Directory.CreateDirectory(Generated); AssetDatabase.Refresh();
        // Regenerate only assets owned by this builder, preserving the source models and shaders.
        foreach (string guid in AssetDatabase.FindAssets("", new[] { Generated }))
        { string path = AssetDatabase.GUIDToAssetPath(guid); if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path); }
        var standard = Shader.Find("Standard");
        if (!standard) throw new Exception("Standard wheel shader unavailable.");
        var outer = Material("Outer", standard, new Color(.78f, .82f, .87f), .85f, .72f);
        var inner = Material("Inner", standard, new Color(.83f, .58f, .15f), .8f, .6f);
        var brake = Material("BrakeDisc", standard, Color.white, .65f, .42f);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BrakeDisc_BaseColor-small.png");
        if (!texture) throw new Exception("Brake disc texture missing.");
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        importer.maxTextureSize = 512; importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        brake.mainTexture = texture;
        var tireShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/TireRubberTexture.shader");
        if (!tireShader || ShaderUtil.ShaderHasError(tireShader)) throw new Exception("Textured rubber shader unavailable.");
        var rubber = new Material(tireShader) { name = "Rubber", color = new Color(.65f, .65f, .65f) };
        AssetDatabase.CreateAsset(rubber, Generated + "/Rubber.mat");
        var tireTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TireRubber_BaseColor-small.png");
        if (!tireTexture) throw new Exception("Tire albedo texture missing.");
        var tireImporter = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tireTexture));
        tireImporter.textureType = TextureImporterType.Default; tireImporter.sRGBTexture = true;
        tireImporter.alphaSource = TextureImporterAlphaSource.None; tireImporter.npotScale = TextureImporterNPOTScale.None;
        tireImporter.maxTextureSize = 512; tireImporter.mipmapEnabled = true; tireImporter.anisoLevel = 8;
        tireImporter.wrapModeU = TextureWrapMode.Repeat; tireImporter.wrapModeV = TextureWrapMode.Clamp;
        tireImporter.SaveAndReimport(); rubber.mainTexture = tireTexture;
        EditorUtility.SetDirty(rubber); EditorUtility.SetDirty(brake);
        var entries = new List<RimEntry>(); var paths = new List<string>(); var addresses = new List<string>();
        string source = Path.GetFullPath("SourceModels");
        var sharedBrakes = new Mesh[2];
        for (int side = 0; side < 2; side++)
        {
            string suffix = side == 0 ? "R" : "L";
            var obj = Obj.Read(Path.Combine(source,"Rims/Rim_01_"+suffix+".obj"));
            sharedBrakes[side] = obj.Mesh("Brake_Shared_"+suffix,obj.BoundsFor("BrakeDisc").center,"BrakeDisc");
        }
        foreach (string file in Directory.GetFiles(Path.Combine(source, "Rims"), "Rim_*_L.obj").OrderBy(p => p, StringComparer.Ordinal))
        {
            int number = int.Parse(Path.GetFileNameWithoutExtension(file).Split('_')[1], Culture);
            string n = number.ToString("00", Culture); var entry = new RimEntry { id = "rim_" + n, name = "SB " + n, sourceModel = number, left = "rims/" + n + "/left", right = "rims/" + n + "/right" };
            for (int side = 0; side < 2; side++)
            {
                string suffix = side == 0 ? "R" : "L"; var obj = Obj.Read(Path.Combine(source, "Rims/Rim_" + n + "_" + suffix + ".obj"));
                if (obj.faces.Count != 3 || !obj.faces.ContainsKey("Outer") || !obj.faces.ContainsKey("Inner") || !obj.faces.ContainsKey("BrakeDisc")) throw new Exception("Unexpected rim groups: " + file);
                Vector3 center = obj.BoundsFor("Outer", "Inner").center;
                var mesh = obj.Mesh("Rim_" + n + "_" + suffix, center, "Outer", "Inner");
                var root = new GameObject("SB_Rim_" + n + "_" + suffix);
                Child(root, "Rim", mesh, new[] { outer, inner });
                Child(root, "BrakeDisc", sharedBrakes[side], new[] { brake });
                // Retain the supplied brake placement without duplicating its vertices.
                root.transform.Find("BrakeDisc").localPosition = obj.BoundsFor("BrakeDisc").center - center;
                string path = Generated + "/" + root.name + ".prefab"; PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root);
                paths.Add(path); addresses.Add(side == 0 ? entry.left : entry.right);
                float radius = 0; foreach (var v in mesh.vertices) radius = Mathf.Max(radius, new Vector2(v.y, v.z).magnitude);
                if (side == 0) entry.leftRadius = radius; else entry.rightRadius = radius;
            }
            entries.Add(entry);
        }
        if (entries.Count != 41) throw new Exception("Expected 41 rims, got " + entries.Count);
        File.WriteAllText(Generated + "/RimCatalog.json", JsonUtility.ToJson(new RimCatalog { models = entries.ToArray() }, true));
        paths.Add(Generated + "/RimCatalog.json"); addresses.Add("rims/catalog");
        var tirePaths = new List<string>(); var tireAddresses = new List<string>();
        for (int side = 0; side < 2; side++)
        {
            string suffix = side == 0 ? "R" : "L"; var obj = Obj.Read(Path.Combine(source, "Tires/Tire_01_" + suffix + ".obj"));
            var mesh = obj.Mesh("Tire_01_" + suffix, obj.BoundsFor(obj.faces.Keys.ToArray()).center, obj.faces.Keys.ToArray());
            MapTireAtlas(mesh);
            var root = new GameObject("ECT.SharedTire." + suffix); root.AddComponent<MeshFilter>().sharedMesh = mesh; root.AddComponent<MeshRenderer>().sharedMaterial = rubber;
            string path = Generated + "/SharedTire_" + suffix + ".prefab"; PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root);
            tirePaths.Add(path); tireAddresses.Add(side == 0 ? "tires/shared/left" : "tires/shared/right");
        }
        AssetDatabase.Refresh(); AssetDatabase.SaveAssets();
        var glass = new[] { "Assets/GlassOverlay.shader", "Assets/GlassFinish.shader" };
        foreach (string path in glass) if (ShaderUtil.ShaderHasError(AssetDatabase.LoadAssetAtPath<Shader>(path))) throw new Exception("Shader compilation failed: " + path);
        string output = Path.GetFullPath("../src/EnhancedCarTuning.Resources/assets/" + platform); Directory.CreateDirectory(output);
        var builds = new[] {
            new AssetBundleBuild { assetBundleName = "ect-rims.bundle", assetNames = paths.ToArray(), addressableNames = addresses.ToArray() },
            new AssetBundleBuild { assetBundleName = "ect-tires.bundle", assetNames = tirePaths.ToArray(), addressableNames = tireAddresses.ToArray() },
            new AssetBundleBuild { assetBundleName = "glass-prototype.bundle", assetNames = glass }
        };
        var manifest = BuildPipeline.BuildAssetBundles(output, builds, BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression, target);
        if (!manifest) throw new Exception("Asset bundle build failed: " + platform);
        File.WriteAllText(Path.Combine(output, "build-info.json"), "{\"platform\":\"" + platform + "\",\"unity\":\"" + Application.unityVersion + "\",\"rimModels\":" + entries.Count + ",\"rimPrefabs\":82,\"brakeMeshes\":2,\"tirePrefabs\":2,\"tireTexture\":\"TireRubber_BaseColor-small.png\",\"tireTextureSha256\":\""+Digest("Assets/TireRubber_BaseColor-small.png")+"\",\"brakeTextureSha256\":\""+Digest("Assets/BrakeDisc_BaseColor-small.png")+"\"}");
        Debug.Log("ECT_ASSETS_OK platform=" + platform + " rims=" + entries.Count + " prefabs=82 tires=2 output=" + output);
    }
    private static string Digest(string path)
    { using(var hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant(); }
    private static void MapTireAtlas(Mesh mesh)
    {
        // Give tread and sidewalls separate UV islands. Only the fixed outer barrel
        // samples the patterned lower half; the deforming walls sample blank rubber.
        var source = mesh.vertices; var normals = mesh.normals; var triangles = mesh.triangles;
        float outer = source.Max(v => new Vector2(v.y, v.z).magnitude);
        var vertices = new List<Vector3>(); var ns = new List<Vector3>(); var uv = new List<Vector2>();
        var indices = new List<int>(); var bounds = mesh.bounds;
        for (int t = 0; t < triangles.Length; t += 3)
        {
            bool tread = true; var angles = new float[3];
            for (int j = 0; j < 3; j++)
            {
                int i = triangles[t+j];
                tread &= new Vector2(source[i].y, source[i].z).magnitude >= outer * .95f && Mathf.Abs(normals[i].x) < .65f;
                angles[j] = Mathf.Repeat(Mathf.Atan2(source[i].z, source[i].y)/(2*Mathf.PI), 1);
            }
            bool seam = angles.Max() - angles.Min() > .5f;
            for (int j = 0; j < 3; j++)
            {
                int i = triangles[t+j]; float u = angles[j]; if (seam && u < .5f) u += 1;
                float v = tread ? Mathf.Lerp(.04f, .48f, Mathf.InverseLerp(bounds.min.x,bounds.max.x,source[i].x)) :
                    Mathf.Lerp(.63f, .93f, new Vector2(source[i].y,source[i].z).magnitude/outer);
                indices.Add(vertices.Count); vertices.Add(source[i]); ns.Add(normals[i]); uv.Add(new Vector2(u*3,v));
            }
        }
        mesh.Clear(); mesh.SetVertices(vertices); mesh.SetNormals(ns); mesh.SetUVs(0,uv); mesh.SetTriangles(indices,0);
        mesh.RecalculateTangents(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
    }
    private static Material Material(string name, Shader shader, Color color, float metallic, float gloss)
    {
        var material = new Material(shader) { name = name, color = color };
        material.SetFloat("_Metallic", metallic); material.SetFloat("_Glossiness", gloss);
        AssetDatabase.CreateAsset(material, Generated + "/" + name + ".mat"); return material;
    }
    private static void Child(GameObject parent, string name, Mesh mesh, Material[] materials)
    {
        var child = new GameObject(name); child.transform.SetParent(parent.transform, false);
        child.AddComponent<MeshFilter>().sharedMesh = mesh; child.AddComponent<MeshRenderer>().sharedMaterials = materials;
    }
}
