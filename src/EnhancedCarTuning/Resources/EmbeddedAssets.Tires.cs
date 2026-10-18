using System;
using System.Collections.Generic;
using EnhancedCarTuning.Resources;
using EnhancedCarTuning.Wheels.Tires;
using UnityEngine;

namespace EnhancedCarTuning
{
    internal static partial class EmbeddedAssets
    {
        private static AssetBundle tiresBundle;
        private static bool tiresAttempted;
        private static readonly Dictionary<string,TireData> tiresById = new Dictionary<string,TireData>(StringComparer.Ordinal);
        private static readonly Dictionary<string,GameObject> tirePrefabs = new Dictionary<string,GameObject>(StringComparer.Ordinal);
        internal static GameObject TirePrefab(string id, bool left)
        {
            if (NetManager.isServer || !TireCatalog.IsKnown(id) || id == "stock") return null;
            EnsureTires();
            if (!tiresBundle || !tiresById.TryGetValue(id, out var model)) return null;
            string address = left ? model.left : model.right;
            if (tirePrefabs.TryGetValue(address, out var cached) && cached) return cached;
            var prefab = tiresBundle.LoadAsset<GameObject>(address);
            if (!prefab || prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0) throw new InvalidOperationException("Invalid tire prefab: " + address);
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length != 1) throw new InvalidOperationException("Expected a single tire mesh: " + address);
            foreach (var filter in filters)
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (!filter.sharedMesh || !filter.sharedMesh.isReadable || filter.sharedMesh.vertexCount < 12 || !renderer || renderer.sharedMaterials.Length != filter.sharedMesh.subMeshCount)
                    throw new InvalidOperationException("Invalid tire geometry: " + address);
            }
            tirePrefabs[address] = prefab; return prefab;
        }
        private static void EnsureTires()
        {
            if (tiresAttempted || NetManager.isServer) return;
            tiresAttempted = true;
            try
            {
                var bytes = ResourceContainer.Read("ect-tires.bundle");
                if (bytes == null) throw new InvalidOperationException("Embedded tire bundle is missing.");
                tiresBundle = AssetBundle.LoadFromMemory(bytes);
                if (!tiresBundle) throw new InvalidOperationException("Cannot load embedded tire bundle.");
                var json = tiresBundle.LoadAsset<TextAsset>("tires/catalog");
                if (!json) throw new InvalidOperationException("Tire catalog TextAsset is missing.");
                var data = TireCatalogJson.Read(json.text);
                if (data == null || data.version != 1 || data.models == null || data.models.Length != TireCatalog.Ids.Length)
                    throw new InvalidOperationException("Invalid tire catalog version or count.");
                foreach (var entry in data.models)
                {
                    if (entry == null || !TireCatalog.IsKnown(entry.id) || entry.id == "stock" || entry.name != TireCatalog.Label(entry.id) ||
                        entry.left != "tires/" + entry.id + "/left" || entry.right != "tires/" + entry.id + "/right" ||
                        tiresById.ContainsKey(entry.id) || !tiresBundle.Contains(entry.left) || !tiresBundle.Contains(entry.right))
                        throw new InvalidOperationException("Invalid tire catalog entry.");
                    tiresById.Add(entry.id, entry);
                }
            }
            catch (Exception error)
            {
                tiresById.Clear(); tirePrefabs.Clear();
                if (tiresBundle) tiresBundle.Unload(true); tiresBundle = null;
                Plugin.Warn("Cannot load embedded tires: " + error.Message);
            }
        }
        private static void ClearTires()
        {
            tiresById.Clear(); tirePrefabs.Clear();
            if (tiresBundle) tiresBundle.Unload(true); tiresBundle = null; tiresAttempted = false;
        }
    }
}
