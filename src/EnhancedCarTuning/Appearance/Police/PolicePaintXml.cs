using System.IO;
using System.Xml;

namespace EnhancedCarTuning.Appearance.Police
{
    internal static class PolicePaintXml
    {
        internal const string Node = "EnhancedPolicePaint";
        internal static void Write(XmlElement root, bool enabled, int siren = 2)
        {
            if (root == null) return;
            for (int i = root.ChildNodes.Count - 1; i >= 0; i--)
                if (root.ChildNodes[i].Name == Node) root.RemoveChild(root.ChildNodes[i]);
            if (!enabled) return;
            var element = root.OwnerDocument.CreateElement(Node);
            element.SetAttribute("version", "1"); element.SetAttribute("enabled", "true");
            element.SetAttribute("siren", siren.ToString(System.Globalization.CultureInfo.InvariantCulture));
            root.AppendChild(element);
        }
        internal static bool TryRead(XmlElement root, out bool enabled)
            => TryRead(root, out enabled, out _);
        internal static bool TryRead(XmlElement root, out bool enabled, out int siren)
        {
            enabled = false; siren = 0;
            if (root == null) return true;
            XmlElement element = null;
            foreach (XmlNode child in root.ChildNodes)
                if (child.Name == Node)
                {
                    if (element != null || !(child is XmlElement value)) return false;
                    element = value;
                }
            if (element == null) return true;
            string flag = element.GetAttribute("enabled");
            if (element.GetAttribute("version") != "1" || element.HasChildNodes ||
                (flag != "true" && flag != "false")) return false;
            string mode = element.GetAttribute("siren");
            if (mode.Length > 0 && mode != "0" && mode != "1" && mode != "2") return false;
            if (flag == "false" && mode.Length > 0 && mode != "0") return false;
            enabled = flag == "true";
            siren = enabled ? mode.Length == 0 ? 2 : mode[0] - '0' : 0;
            return true;
        }
        internal static bool TryRead(string xml, out bool enabled)
            => TryRead(xml, out enabled, out _);
        internal static bool TryRead(string xml, out bool enabled, out int siren)
        {
            enabled = false; siren = 0;
            if (string.IsNullOrEmpty(xml)) return true;
            try
            {
                var document = new XmlDocument { XmlResolver = null };
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null, MaxCharactersInDocument = 2 * 1024 * 1024 };
                using (var input = new StringReader(xml))
                using (var reader = XmlReader.Create(input, settings)) document.Load(reader);
                return TryRead(document.DocumentElement, out enabled, out siren);
            }
            catch (XmlException) { return false; }
        }
    }
}
