using System.Xml;

namespace EnhancedCarTuning.Appearance.Vinyls
{
    internal static class VinylXml
    {
        internal const string Symmetry = "ect.symmetry";
        internal const string TextSprite = "ECT.Text";
        internal static void WriteText(XmlElement xml, TextVinylData data)
        {
            if (data == null)
            { xml.RemoveAttribute("ect.text"); xml.RemoveAttribute("ect.font"); xml.RemoveAttribute("ect.style"); return; }
            xml.SetAttribute("sprite.name", TextSprite);
            xml.SetAttribute("ect.text", data.Text);
            xml.SetAttribute("ect.font", data.Font.ToString(System.Globalization.CultureInfo.InvariantCulture));
            xml.SetAttribute("ect.style", data.Style.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        internal static bool TryReadText(XmlElement xml, out TextVinylData data)
        {
            data = null;
            if (xml.GetAttribute("sprite.name") != TextSprite) return TextVinylData.TryRead(xml.GetAttribute("sprite.name"), out data);
            if (!xml.HasAttribute("ect.text") || !int.TryParse(xml.GetAttribute("ect.font"), out var font) ||
                !int.TryParse(xml.GetAttribute("ect.style"), out var style)) return false;
            try { data = new TextVinylData(xml.GetAttribute("ect.text"), font, style); return true; }
            catch (System.ArgumentException) { return false; }
        }
        internal static bool ReadSymmetry(XmlElement xml) => xml.GetAttribute(Symmetry) == "1";
        internal static void WriteSymmetry(XmlElement xml, bool value)
        { if (value) xml.SetAttribute(Symmetry, "1"); else xml.RemoveAttribute(Symmetry); }
        internal static bool Validate(XmlElement root)
        {
            if (root == null) return true;
            var nodes = root.SelectNodes("CarPaint/Items/Item");
            if (nodes.Count > 256) return false;
            foreach (XmlElement item in nodes)
            {
                var symmetry = item.GetAttribute(Symmetry);
                if (symmetry.Length != 0 && symmetry != "0" && symmetry != "1") return false;
                string name = item.GetAttribute("sprite.name");
                if (name == TextSprite && !TryReadText(item, out _)) return false;
                if (name.StartsWith("ect.text.", System.StringComparison.Ordinal) && !TextVinylData.TryRead(name, out _)) return false;
            }
            return true;
        }
    }
}
