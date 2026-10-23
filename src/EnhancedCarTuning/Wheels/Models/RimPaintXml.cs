using System;
using System.Globalization;
using System.Xml;
using EnhancedCarTuning.Appearance.Body;
using EnhancedCarTuning.Wheels.Suspension;
using UnityEngine;

namespace EnhancedCarTuning.Wheels.Models
{
    internal static class RimPaintXml
    {
        internal const string Node = "EnhancedRimPaint";
        internal static bool Read(XmlElement root, WheelSettings original, out WheelSettings settings)
        {
            settings = original; var nodes = root.SelectNodes(Node);
            if (nodes.Count == 0) return true;
            if (nodes.Count != 1 || !(nodes[0] is XmlElement element) || element.Attributes.Count != 1 || element.GetAttribute("version") != "1") return false;
            int seen = 0;
            foreach (XmlNode child in element.ChildNodes)
            {
                if (!(child is XmlElement part) || part.Name != "Part" || part.HasChildNodes || part.Attributes.Count != 5) return false;
                int axle = part.GetAttribute("axle") == "Front" ? 0 : part.GetAttribute("axle") == "Rear" ? 1 : -1;
                int which = part.GetAttribute("name") == "Outer" ? 0 : part.GetAttribute("name") == "Inner" ? 1 : -1;
                if (axle < 0 || which < 0 || (seen & (1 << (axle * 2 + which))) != 0) return false;
                var values = part.GetAttribute("color").Split(' '); if (values.Length != 3) return false;
                float r, g, b, metallic, gloss;
                if (!Unit(values[0], out r) || !Unit(values[1], out g) || !Unit(values[2], out b) || !Unit(part.GetAttribute("metallic"), out metallic) || !Unit(part.GetAttribute("gloss"), out gloss)) return false;
                settings = settings.WithPaint(axle == 0 ? AxleTarget.Front : AxleTarget.Rear, 1 << which, new PaintFinish(new Color(r,g,b,1),metallic,gloss));
                seen |= 1 << (axle * 2 + which);
            }
            return true;
        }
        private static bool Unit(string text, out float value) => float.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out value) && !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;
        private static string Number(float value) => value.ToString("R",CultureInfo.InvariantCulture);
        internal static void Write(XmlElement root, WheelSettings settings)
        {
            for (int i = root.ChildNodes.Count - 1; i >= 0; i--) if (root.ChildNodes[i].Name == Node) root.RemoveChild(root.ChildNodes[i]);
            var node = root.OwnerDocument.CreateElement(Node); node.SetAttribute("version","1");
            for (int axle = 0; axle < 2; axle++) for (int part = 0; part < 2; part++)
            {
                var setting = axle == 0 ? settings.Front : settings.Rear; var paint = part == 0 ? setting.OuterFinish : setting.InnerFinish;
                if (paint == null) continue;
                var item = root.OwnerDocument.CreateElement("Part"); item.SetAttribute("axle",axle == 0 ? "Front" : "Rear"); item.SetAttribute("name",part == 0 ? "Outer" : "Inner");
                item.SetAttribute("color",Number(paint.Color.r)+" "+Number(paint.Color.g)+" "+Number(paint.Color.b)); item.SetAttribute("metallic",Number(paint.Metallic)); item.SetAttribute("gloss",Number(paint.Gloss)); node.AppendChild(item);
            }
            if (node.HasChildNodes) root.AppendChild(node);
        }
    }
}
