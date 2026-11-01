using System;
using System.Xml;
using EnhancedCarTuning.Wheels.Suspension;

namespace EnhancedCarTuning.Wheels.Tires
{
    internal static class TireXml
    {
        internal const string Node = "EnhancedTireModels";
        internal static bool ValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 128) return false;
            foreach (char c in id)
                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_' || c == '-' || c == '.' )) return false;
            return true;
        }
        internal static bool Read(XmlElement root, WheelSettings original, out WheelSettings settings)
        {
            settings = original;
            XmlElement element = null;
            if (root == null) return true;
            foreach (XmlNode node in root.ChildNodes)
                if (node is XmlElement child && child.Name == Node)
                { if (element != null) return false; element = child; }
            if (element == null) return true;
            if (element.GetAttribute("version") != "1" || element.Attributes.Count != 1) return false;
            bool front = false, rear = false;
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement axle)) continue;
                if (axle.Name != "Front" && axle.Name != "Rear" || axle.HasChildNodes || axle.Attributes.Count != 1) return false;
                bool isRear = axle.Name == "Rear";
                if (isRear ? rear : front) return false;
                string id = axle.GetAttribute("model");
                if (!ValidId(id)) return false;
                settings = settings.WithTire(isRear ? AxleTarget.Rear : AxleTarget.Front, id);
                if (isRear) rear = true; else front = true;
            }
            return true;
        }
        internal static void Write(XmlElement root, WheelSettings settings)
        {
            if (root == null) return;
            for (int i = root.ChildNodes.Count - 1; i >= 0; i--)
                if (root.ChildNodes[i].Name == Node) root.RemoveChild(root.ChildNodes[i]);
            if (settings.Front.TireId == "stock" && settings.Rear.TireId == "stock") return;
            var element = root.OwnerDocument.CreateElement(Node);
            element.SetAttribute("version", "1"); root.AppendChild(element);
            WriteAxle(element, "Front", settings.Front.TireId); WriteAxle(element, "Rear", settings.Rear.TireId);
        }
        private static void WriteAxle(XmlElement root, string name, string id)
        {
            if (id == "stock") return;
            var element = root.OwnerDocument.CreateElement(name); element.SetAttribute("model", id); root.AppendChild(element);
        }
    }
}
