using System;
using System.IO;
using System.Reflection;
using System.Linq;

namespace EnhancedCarTuning.Resources
{
    // Only managed bytes leave this assembly. The runtime consumes and owns Unity textures/sprites.
    public static class ResourceContainer
    {
        private const string Prefix = "EnhancedCarTuning.Resources.";
        private static readonly string[] fontResources = DiscoverFonts();
        public static int RasterFontCount => fontResources.Length;
        public static string[] GetRasterFontResources() => (string[])fontResources.Clone();
        private static string[] DiscoverFonts()
        {
            return typeof(ResourceContainer).Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(Prefix + "assets.fonts.", StringComparison.Ordinal) &&
                    (name.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(name => name, StringComparer.Ordinal)
                .Select(name => name.Substring(Prefix.Length)).ToArray();
        }
        public static byte[] Read(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(new[] { '/', '\\' }) >= 0)
                throw new ArgumentException("Expected an embedded resource name.", "name");
            using (Stream stream = typeof(ResourceContainer).Assembly.GetManifestResourceStream(Prefix + name))
            {
                if (stream == null) return null;
                using (var ms = new MemoryStream()) { stream.CopyTo(ms); return ms.ToArray(); }
            }
        }
    }
}
