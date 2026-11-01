using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using EnhancedCarTuning.Resources;
using UnityEngine;
using FontStyle = System.Drawing.FontStyle;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning
{
    internal static partial class EmbeddedAssets
    {
        private static Texture2D iconTexture;
        private static Sprite icon;
        private static PrivateFontCollection[] fontCollections;
        private static GCHandle[] handles;
        private static FontFamily[] fontFamilies;
        private static string[] fontNames;
        private static bool fontsInitialized;
        private static AssetBundle uiFontsBundle;
        private static bool bundleAttempted;
        private static UnityEngine.Font cachedUiFont;

        internal static Sprite TextVinylIcon
        {
            get
            {
                if (icon) return icon;
                var bytes = ResourceContainer.Read("text-vinyl-icon.png");
                if (bytes == null) return null;
                iconTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                iconTexture.name = "ECT.TextVinylIcon";
                if (!iconTexture.LoadImage(bytes)) { Object.Destroy(iconTexture); iconTexture = null; return null; }
                // The PNG contains uneven transparent padding. Center the visible glyph,
                // not only the 128x128 file bounds (the latter looks off-center in the
                // native CarPaintSprite tile even after RectTransform alignment).
                Rect visible = GetVisibleIconBounds(iconTexture);
                icon = Sprite.Create(iconTexture, visible, new Vector2(.5f, .5f));
                icon.name = "ECT.TextVinylIcon";
                return icon;
            }
        }

        private static Rect GetVisibleIconBounds(Texture2D texture)
        {
            try
            {
                var pixels = texture.GetPixels32();
                int width = texture.width, height = texture.height;
                int minX = width, minY = height, maxX = -1, maxY = -1;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        if (pixels[y * width + x].a > 12)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                if (maxX < minX || maxY < minY) return new Rect(0, 0, width, height);
                const int padding = 6;
                int left = Math.Max(0, minX - padding), bottom = Math.Max(0, minY - padding);
                int right = Math.Min(width, maxX + padding + 1), top = Math.Min(height, maxY + padding + 1);
                return new Rect(left, bottom, right - left, top - bottom);
            }
            catch (Exception) { return new Rect(0, 0, texture.width, texture.height); }
        }

        // UnityEngine.Font cannot be created from standalone OTF bytes. A Unity-version-compatible
        // AssetBundle is required for UI fonts; the OTF data can be used by System.Drawing's rasterizer.
        internal static UnityEngine.Font UiFont(string assetName)
        {
            if (!uiFontsBundle && !bundleAttempted)
            {
                bundleAttempted = true;
                var bytes = ResourceContainer.Read("ui-fonts.bundle");
                if (bytes != null) try { uiFontsBundle = AssetBundle.LoadFromMemory(bytes); }
                catch (Exception e) { Plugin.Warn("UI font bundle could not be loaded: " + e.Message); }
            }
            if (!uiFontsBundle) return null;
            if (!cachedUiFont) cachedUiFont = uiFontsBundle.LoadAsset<UnityEngine.Font>(assetName);
            return cachedUiFont;
        }

        internal static System.Drawing.Font RasterFont(int index, float pixels, System.Drawing.FontStyle style)
        {
            InitializeRasterFonts();
            if (fontFamilies != null && index >= 0 && index < fontFamilies.Length && fontFamilies[index] != null)
                try { return new System.Drawing.Font(fontFamilies[index], pixels, style, GraphicsUnit.Pixel); }
                catch (Exception) { /* Font files need not support every requested weight/style. */ }
            if (fontFamilies != null && index >= 0 && index < fontFamilies.Length && fontFamilies[index] != null)
                foreach (var available in new[] { FontStyle.Regular, FontStyle.Bold, FontStyle.Italic, FontStyle.Bold | FontStyle.Italic })
                    if (fontFamilies[index].IsStyleAvailable(available))
                        return new System.Drawing.Font(fontFamilies[index], pixels, available, GraphicsUnit.Pixel);
            return new System.Drawing.Font(FontFamily.GenericSansSerif, pixels, style, GraphicsUnit.Pixel);
        }

        internal static string[] RasterFontNames { get { InitializeRasterFonts(); return fontNames; } }

        private static void InitializeRasterFonts()
        {
            if (fontsInitialized) return;
            fontsInitialized = true;
            string[] names = ResourceContainer.GetRasterFontResources();
            fontCollections = new PrivateFontCollection[names.Length];
            handles = new GCHandle[names.Length];
            fontFamilies = new FontFamily[names.Length];
            fontNames = names.Length == 0 ? new[] { "Arial" } : new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                fontNames[i] = Path.GetFileNameWithoutExtension(names[i].Substring("assets.fonts.".Length));
                byte[] bytes = ResourceContainer.Read(names[i]);
                if (bytes == null) continue;
                try
                {
                    handles[i] = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    // Isolate each file: font collections can reorder/deduplicate families.
                    var collection = fontCollections[i] = new PrivateFontCollection();
                    collection.AddMemoryFont(handles[i].AddrOfPinnedObject(), bytes.Length);
                    var families = collection.Families;
                    if (families.Length == 0) throw new InvalidOperationException("Font contains no usable family.");
                    fontFamilies[i] = families[0]; fontNames[i] = families[0].Name;
                    for (int family = 1; family < families.Length; family++) families[family].Dispose();
                }
                catch (Exception e) { Plugin.Warn("Could not register font " + names[i] + ": " + e.Message); }
            }
            // Separate files for one family must remain distinguishable in the picker.
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in fontNames) { counts.TryGetValue(name, out int count); counts[name] = count + 1; }
            for (int i = 0; i < names.Length; i++)
                if (counts[fontNames[i]] > 1)
                    fontNames[i] += " (" + Path.GetFileNameWithoutExtension(names[i].Substring("assets.fonts.".Length)) + ")";
        }

        internal static void Clear()
        {
            ClearRims();
            ClearTires();
            if (icon) Object.Destroy(icon); icon = null;
            if (iconTexture) Object.Destroy(iconTexture); iconTexture = null;
            if (uiFontsBundle) { uiFontsBundle.Unload(false); uiFontsBundle = null; }
            bundleAttempted = false; cachedUiFont = null;
            if (fontFamilies != null) foreach (var family in fontFamilies) family?.Dispose();
            fontFamilies = null; fontNames = null;
            if (fontCollections != null) foreach (var collection in fontCollections) collection?.Dispose();
            fontCollections = null;
            if (handles != null)
                foreach (var handle in handles) if (handle.IsAllocated) handle.Free();
            handles = null; fontsInitialized = false;
        }
    }
}
