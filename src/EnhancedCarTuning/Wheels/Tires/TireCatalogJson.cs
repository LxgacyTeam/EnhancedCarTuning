using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace EnhancedCarTuning.Wheels.Tires
{
    [DataContract] internal sealed class TireCatalogData
    {
        [DataMember(IsRequired = true)] public int version;
        [DataMember(IsRequired = true)] public TireData[] models;
    }
    [DataContract] internal sealed class TireData
    {
        [DataMember(IsRequired = true)] public string id;
        [DataMember(IsRequired = true)] public string name;
        [DataMember(IsRequired = true)] public string left;
        [DataMember(IsRequired = true)] public string right;
    }
    internal static class TireCatalogJson
    {
        internal static TireCatalogData Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 256 * 1024) throw new SerializationException("Invalid tire catalog JSON.");
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json.TrimStart('\uFEFF'))))
                return (TireCatalogData)new DataContractJsonSerializer(typeof(TireCatalogData)).ReadObject(stream);
        }
    }
}
