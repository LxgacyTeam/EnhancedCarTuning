using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace EnhancedCarTuning.Wheels.Rims
{
    [DataContract]
    internal sealed class RimCatalogData
    {
        [DataMember(IsRequired = true)] public int version;
        [DataMember] public string unity;
        [DataMember(IsRequired = true)] public RimData[] models;
    }
    [DataContract]
    internal sealed class RimData
    {
        [DataMember(IsRequired = true)] public string id;
        [DataMember(IsRequired = true)] public string name;
        [DataMember(IsRequired = true)] public string left;
        [DataMember(IsRequired = true)] public string right;
        [DataMember(IsRequired = true)] public int sourceModel;
        [DataMember(IsRequired = true)] public float leftRadius;
        [DataMember(IsRequired = true)] public float rightRadius;
    }
    internal static class RimCatalogJson
    {
        internal static RimCatalogData Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 256 * 1024)
                throw new InvalidOperationException("Missing or oversized rim catalog JSON.");
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json.TrimStart('\uFEFF'))))
                return (RimCatalogData)new DataContractJsonSerializer(typeof(RimCatalogData)).ReadObject(stream);
        }
    }
}
