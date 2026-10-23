using System;
using System.Globalization;
using System.Xml;
using EnhancedCarTuning.Wheels.Suspension;

namespace EnhancedCarTuning.Wheels.Models
{
    internal static class RimSizeXml
    {
        internal const string Node = "EnhancedRimSizes";
        internal static bool Read(XmlElement root, WheelSettings original, out WheelSettings settings)
        {
            settings = original;
            var nodes = root.SelectNodes(Node);
            if (nodes.Count == 0) return true;
            if (nodes.Count != 1 || !(nodes[0] is XmlElement element) || element.Attributes.Count != 1 || element.GetAttribute("version") != "1") return false;
            bool front = false, rear = false;
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement axle)) continue;
                if (axle.Name != "Front" && axle.Name != "Rear" || axle.HasChildNodes || axle.Attributes.Count == 0) return false;
                bool back = axle.Name == "Rear";
                if (back ? rear : front) return false;
                foreach (XmlAttribute attr in axle.Attributes)
                {
                    WheelParameter p;
                    if (attr.Name == "diameter") p = WheelParameter.RimDiameter;
                    else if (attr.Name == "width") p = WheelParameter.RimWidth;
                    else return false;
                    if (!float.TryParse(attr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) || !WheelLimits.Valid(p, v)) return false;
                    settings = settings.With(back ? AxleTarget.Rear : AxleTarget.Front, p, v);
                }
                if (back) rear = true; else front = true;
            }
            return true;
        }
        internal static void Write(XmlElement root, WheelSettings settings)
        {
            for (int i = root.ChildNodes.Count - 1; i >= 0; i--) if (root.ChildNodes[i].Name == Node) root.RemoveChild(root.ChildNodes[i]);
            var element = root.OwnerDocument.CreateElement(Node); element.SetAttribute("version", "1");
            WriteAxle(element, "Front", settings.Front); WriteAxle(element, "Rear", settings.Rear);
            if (element.HasChildNodes) root.AppendChild(element);
        }
        private static void WriteAxle(XmlElement root, string name, AxleSettings settings)
        {
            var axle = root.OwnerDocument.CreateElement(name);
            if (settings.Get(WheelParameter.RimDiameter).HasValue) axle.SetAttribute("diameter", settings.Get(WheelParameter.RimDiameter).Value.ToString("R", CultureInfo.InvariantCulture));
            if (settings.Get(WheelParameter.RimWidth).HasValue) axle.SetAttribute("width", settings.Get(WheelParameter.RimWidth).Value.ToString("R", CultureInfo.InvariantCulture));
            if (axle.Attributes.Count != 0) root.AppendChild(axle);
        }
    }
}
