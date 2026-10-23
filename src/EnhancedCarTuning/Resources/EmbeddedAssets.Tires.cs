using System;
using EnhancedCarTuning.Resources;
using UnityEngine;

namespace EnhancedCarTuning
{
    internal static partial class EmbeddedAssets
    {
        private static AssetBundle tiresBundle;
        private static bool tiresAttempted;
        private static readonly GameObject[] tirePrefabs = new GameObject[2];
        internal static GameObject TirePrefab(bool left)
        {
            if (NetManager.isServer) return null;
            int side = left ? 0 : 1;
            if (tirePrefabs[side]) return tirePrefabs[side];
            try
            {
                if (!tiresAttempted)
                {
                    tiresAttempted = true;
                    var bytes = ResourceContainer.Read("ect-tires.bundle");
                    if (bytes != null) tiresBundle = AssetBundle.LoadFromMemory(bytes);
                    if (!tiresBundle) throw new InvalidOperationException("Shared tire bundle could not be loaded (" + ResourceContainer.Platform + ").");
                }
                if (!tiresBundle) return null;
                var prefab = tiresBundle.LoadAsset<GameObject>(left ? "tires/shared/left" : "tires/shared/right");
                if (!prefab || prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0) throw new InvalidOperationException("Invalid shared tire prefab.");
                var filter = prefab.GetComponent<MeshFilter>(); var renderer = prefab.GetComponent<MeshRenderer>();
                if (!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable || filter.sharedMesh.vertexCount < 12 || !renderer || renderer.sharedMaterials.Length != filter.sharedMesh.subMeshCount)
                    throw new InvalidOperationException("Shared tire must have readable geometry and matching material slots.");
                var material = renderer.sharedMaterial;
                if (!material || !material.shader || material.shader.name != "ECT/TireRubberTexture" || !material.shader.isSupported ||
                    !material.mainTexture || material.mainTexture.name != "TireRubber_BaseColor-small")
                    throw new InvalidOperationException("Shared tire requires the textured rubber shader and TireRubber_BaseColor-small. Install the matching resource DLL for this platform.");
                return tirePrefabs[side] = prefab;
            }
            catch (Exception error) { Plugin.Warn("Cannot load shared tire: " + error.Message); return null; }
        }
        private static void ClearTires()
        {
            tirePrefabs[0] = tirePrefabs[1] = null;
            if (tiresBundle) tiresBundle.Unload(true); tiresBundle = null; tiresAttempted = false;
        }
    }
}
