using System;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;

namespace EnhancedCarTuning.Appearance.Body
{
    internal static class BodyPaintXml
    {
        internal const string Node = "EnhancedBodyPaint";
        internal static void Write(XmlElement root, BodyPaintSettings settings)
        {
            if (root == null) return;
            for (int i = root.ChildNodes.Count - 1; i >= 0; i--) if (root.ChildNodes[i].Name == Node) root.RemoveChild(root.ChildNodes[i]);
            if (settings.IsEmpty) return;
            var node = root.OwnerDocument.CreateElement(Node); node.SetAttribute("version", "1");
            for (int i = 0; i < BodyPaintSettings.Count; i++)
            {
                var finish = settings[(BodyPart)i]; if (finish == null) continue;
                var part = root.OwnerDocument.CreateElement("Part");
                part.SetAttribute("id", ((BodyPart)i).ToString());
                part.SetAttribute("color", Number(finish.Color.r) + " " + Number(finish.Color.g) + " " + Number(finish.Color.b));
                part.SetAttribute("metallic", Number(finish.Metallic)); part.SetAttribute("gloss", Number(finish.Gloss));
                node.AppendChild(part);
            }
            root.AppendChild(node);
        }
        private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        internal static bool TryRead(string xml, out BodyPaintSettings settings)
        {
            settings = BodyPaintSettings.Empty;
            if (string.IsNullOrEmpty(xml)) return true;
            try
            {
                var document = new XmlDocument { XmlResolver = null };
                var options = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
                    MaxCharactersInDocument = 2 * 1024 * 1024 };
                using (var input = new StringReader(xml))
                using (var reader = XmlReader.Create(input, options)) document.Load(reader);
                return TryRead(document.DocumentElement, out settings);
            }
            catch (XmlException) { return false; }
        }
        internal static bool TryRead(XmlElement root, out BodyPaintSettings settings)
        {
            settings = BodyPaintSettings.Empty;
            if (root == null) return true;
            XmlElement node = null;
            foreach (XmlNode child in root.ChildNodes) if (child.Name == Node)
            { if (node != null || !(child is XmlElement element)) return false; node = element; }
            if (node == null) return true;
            if (node.GetAttribute("version") != "1" || node.ChildNodes.Count > BodyPaintSettings.Count) return false;
            int seen = 0;
            var result = BodyPaintSettings.Empty;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (!(child is XmlElement element) || element.Name != "Part") return false;
                BodyPart part;
                if (!Enum.TryParse(element.GetAttribute("id"), out part) || !Enum.IsDefined(typeof(BodyPart), part) ||
                    part.ToString() != element.GetAttribute("id") || (seen & (1 << (int)part)) != 0) return false;
                var color = element.GetAttribute("color").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (color.Length != 3) return false;
                try
                {
                    result = result.With(part, new PaintFinish(new Color(Parse(color[0]), Parse(color[1]), Parse(color[2]), 1),
                        Parse(element.GetAttribute("metallic")), Parse(element.GetAttribute("gloss"))));
                }
                catch (Exception e) when (e is ArgumentException || e is FormatException || e is OverflowException) { return false; }
                seen |= 1 << (int)part;
            }
            settings = result; return true;
        }
        private static float Parse(string value)
        {
            if (value.Length > 32) throw new FormatException();
            return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
