using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EnhancedCarTuning.Resources;
using EnhancedCarTuning.Wheels.Rims;
using UnityEngine;

namespace EnhancedCarTuning
{
    internal static partial class EmbeddedAssets
    {
        private static AssetBundle rimsBundle;
        private static bool rimsAttempted;
        private static ReadOnlyCollection<RimModel> rimModels = Array.AsReadOnly(new RimModel[0]);
        private static readonly Dictionary<string, RimModel> rimById = new Dictionary<string, RimModel>(StringComparer.Ordinal);
        private static readonly Dictionary<string, GameObject> rimPrefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);

        internal static IReadOnlyList<RimModel> RimModels { get { EnsureRims(); return rimModels; } }
        internal static bool TryGetRim(string id, out RimModel model)
        { EnsureRims(); model = null; return id != null && rimById.TryGetValue(id,out model); }
        internal static GameObject RimPrefab(string id, bool left)
        {
            if (!TryGetRim(id,out var model) || !rimsBundle) return null;
            string address = left ? model.LeftAsset : model.RightAsset;
            if (rimPrefabs.TryGetValue(address,out var cached) && cached) return cached;
            var prefab = rimsBundle.LoadAsset<GameObject>(address);
            if (!prefab || prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidOperationException("Invalid rim prefab: " + address);
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length == 0) throw new InvalidOperationException("Rim has no geometry: " + address);
            foreach (var filter in filters)
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (!filter.sharedMesh || filter.sharedMesh.vertexCount == 0 || !renderer || renderer.sharedMaterials.Length != filter.sharedMesh.subMeshCount)
                    throw new InvalidOperationException("Invalid rim geometry or material slots: " + address);
            }
            rimPrefabs[address] = prefab; return prefab;
        }
        private static void EnsureRims()
        {
            if (rimsAttempted || NetManager.isServer) return;
            rimsAttempted = true;
            try
            {
                var bytes = ResourceContainer.Read("ect-rims.bundle");
                if (bytes == null) throw new InvalidOperationException("Embedded rim bundle is missing.");
                rimsBundle = AssetBundle.LoadFromMemory(bytes);
                if (!rimsBundle) throw new InvalidOperationException("Embedded rim bundle could not be loaded.");
                var json = rimsBundle.LoadAsset<TextAsset>("rims/catalog");
                if (!json) throw new InvalidOperationException("Embedded rims/catalog TextAsset is missing.");
                var data = RimCatalogJson.Read(json.text);
                if (data == null || data.version != 1 || data.models == null || data.models.Length == 0 || data.models.Length > 128)
                    throw new InvalidOperationException("Invalid rim bundle catalog: version=" + (data == null ? "null" : data.version.ToString()) +
                        ", models=" + (data == null || data.models == null ? "null" : data.models.Length.ToString()));
                var list = new List<RimModel>();
                foreach (var entry in data.models)
                {
                    string number = entry.sourceModel.ToString("00",System.Globalization.CultureInfo.InvariantCulture);
                    if (entry.sourceModel < 1 || entry.sourceModel > 99 || entry.id != "rim_" + number ||
                        entry.left != "rims/" + number + "/left" || entry.right != "rims/" + number + "/right" ||
                        string.IsNullOrEmpty(entry.name) || entry.name.Length > 128 || rimById.ContainsKey(entry.id) ||
                        !ValidRadius(entry.leftRadius) || !ValidRadius(entry.rightRadius) || !rimsBundle.Contains(entry.left) || !rimsBundle.Contains(entry.right))
                        throw new InvalidOperationException("Invalid rim catalog entry.");
                    var model = new RimModel(entry.id,entry.name,entry.left,entry.right,entry.leftRadius,entry.rightRadius);
                    rimById.Add(model.Id,model); list.Add(model);
                }
                rimModels = list.AsReadOnly();
            }
            catch (Exception error)
            {
                rimModels = Array.AsReadOnly(new RimModel[0]); rimById.Clear(); rimPrefabs.Clear();
                if (rimsBundle) rimsBundle.Unload(true); rimsBundle = null;
                Plugin.Warn("Cannot load embedded rims: " + error.Message);
            }
        }
        private static bool ValidRadius(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= .01f && value <= 5;
        private static void ClearRims()
        {
            NativeRimVisual.ClearAll(); RimVisual.ClearAll(); rimPrefabs.Clear(); rimById.Clear(); rimModels = Array.AsReadOnly(new RimModel[0]);
            if (rimsBundle) rimsBundle.Unload(true); rimsBundle = null; rimsAttempted = false;
            EnhancedCarTuning.Wheels.Models.WheelModelCatalog.Reset();
        }
    }
}
