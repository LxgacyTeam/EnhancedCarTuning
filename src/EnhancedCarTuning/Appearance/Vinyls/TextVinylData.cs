using System;
using System.Text;
using System.Globalization;
using EnhancedCarTuning.Resources;

namespace EnhancedCarTuning.Appearance.Vinyls
{
    internal sealed class TextVinylData
    {
        internal const string Prefix = "ect.text.v1:";
        internal const int MaxLength = 80;
        internal static string[] Fonts => EmbeddedAssets.RasterFontNames;
        internal readonly string Text;
        internal readonly int Font, Style;
        internal TextVinylData(string text, int font = 0, int style = 0)
        {
            // Server validation only needs the managed manifest, never GDI/font initialization.
            if (text == null || text.Length > MaxLength || font < 0 || font >= Math.Max(1, ResourceContainer.RasterFontCount) || style < 0 || style > 3)
                throw new ArgumentException("Invalid text vinyl.");
            foreach (char c in text) if (char.IsControl(c)) throw new ArgumentException("Control character in text vinyl.");
            new UTF8Encoding(false, true).GetBytes(text);
            Text = text; Font = font; Style = style;
        }
        internal string Name => Prefix + Font.ToString(CultureInfo.InvariantCulture) + ":" + Style + ":" + Convert.ToBase64String(Encoding.UTF8.GetBytes(Text));
        internal static bool IsText(string name) => name != null && name.StartsWith(Prefix, StringComparison.Ordinal);
        internal static bool TryRead(string name, out TextVinylData value)
        {
            value = null;
            if (!IsText(name) || name.Length > 360) return false;
            var parts = name.Substring(Prefix.Length).Split(':');
            if (parts.Length != 3 || parts[1].Length != 1 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int font)) return false;
            try
            {
                value = new TextVinylData(new UTF8Encoding(false, true).GetString(Convert.FromBase64String(parts[2])),
                    font, int.Parse(parts[1], CultureInfo.InvariantCulture));
                return value.Name == name;
            }
            catch (Exception e) when (e is ArgumentException || e is FormatException || e is OverflowException) { return false; }
        }
    }
}
