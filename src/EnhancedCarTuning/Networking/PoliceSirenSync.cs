using System.IO;
using System.Xml;
using EnhancedCarTuning.Appearance.Police;

namespace EnhancedCarTuning.Networking
{
    internal static class PoliceSirenSync
    {
        internal static bool TryUpdate(string original, int mode, out string xml)
        {
            xml = null;
            if (string.IsNullOrEmpty(original) || mode < 0 || mode > 2) return false;
            try
            {
                var document = new XmlDocument { XmlResolver = null };
                var options = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null, MaxCharactersInDocument = 2 * 1024 * 1024 };
                using (var input = new StringReader(original))
                using (var reader = XmlReader.Create(input, options)) document.Load(reader);
                var root = document.DocumentElement;
                if (root == null || root.Name != "CarMat" || !PolicePaintXml.TryRead(root, out bool enabled) || !enabled) return false;
                PolicePaintXml.Write(root, true, mode); xml = document.OuterXml; return true;
            }
            catch (XmlException) { return false; }
        }
    }
}
