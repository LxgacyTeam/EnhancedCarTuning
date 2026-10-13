using UnityEngine.Networking;

namespace EnhancedCarTuning.Networking
{
    internal static class VehicleXmlSync
    {
        internal static void Publish(NetControl net, string xml)
        {
            if (net.xmlData == xml) return;
            net.NetworkxmlData = xml;
            if (NetworkServer.localClientActive || !net.control) return;
            var loader = net.control.GetComponent<CarSaveLoad>();
            if (loader) loader.LoadXml(xml);
        }
    }
}
