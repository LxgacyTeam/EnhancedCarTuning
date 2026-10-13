using System;
using System.Collections.Generic;
using EnhancedCarTuning.Configuration;
using EnhancedCarTuning.Resources;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Appearance.Vinyls
{
    // Opt-in replacement for the known stock primitive assets. Missing embedded PNGs
    // deliberately fall back to vanilla, so adding assets needs no code changes.
    internal static class PrimitiveVinylTextures
    {
        private static readonly HashSet<string> supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Primitive_1", "Primitive_2", "Primitive_3", "Primitive_4", "Primitive_5",
            "Primitive_7", "Primitive_8", "Primitive_10", "Primitive_11", "Primitive_12",
            "Primitive_14", "Primitive_15", "Primitive_17", "Primitive_21", "Primitive_22"
        };
        private static readonly Dictionary<string, Texture2D> loaded = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static string Key(CarPaintSprites.SpriteInfo info)
        {
            if (info == null) return null;
            string name = !string.IsNullOrEmpty(info.texutreName) ? info.texutreName : info.fullPath;
            if (string.IsNullOrEmpty(name)) return null;
            name = name.Replace('\\', '/');
            int slash = name.LastIndexOf('/');
            if (slash >= 0) name = name.Substring(slash + 1);
            if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 4);
            return supported.Contains(name) ? name : null;
        }

        internal static bool TryTexture(CarPaintSprites.SpriteInfo info, out Texture2D texture)
        {
            texture = null;
            if (NetManager.isServer || !TuningConfig.UsePrimitiveTextures) return false;
            string key = Key(info);
            if (key == null || missing.Contains(key)) return false;
            if (loaded.TryGetValue(key, out texture) && texture) return true;
            byte[] data = ResourceContainer.Read(key + ".png");
            if (data == null) { missing.Add(key); return false; }
            Texture2D created = null;
            try
            {
                created = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!created.LoadImage(data)) throw new InvalidOperationException("LoadImage returned false");
                var pixels = created.GetPixels32();
                FixTransparentRgb(pixels);
                created.SetPixels32(pixels);
                created.Apply(false);
                created.name = "ECT." + key;
                created.wrapMode = TextureWrapMode.Clamp;
                created.filterMode = FilterMode.Bilinear;
                loaded[key] = texture = created;
                return true;
            }
            catch (Exception ex)
            {
                if (created) Object.Destroy(created);
                missing.Add(key);
                Plugin.Warn("Unable to load primitive " + key + ": " + ex.Message);
                return false;
            }
        }

        internal static void FixTransparentRgb(Color32[] pixels)
        {
            // These primitives are white tint masks with straight alpha. Runtime LoadImage
            // bypasses the importer's Alpha Is Transparency color bleed. Black RGB under
            // zero alpha otherwise darkens a bilinear sample across the shape's edge.
            // Change only hidden RGB: preserve the alpha mask and visible/antialiased texels.
            for (int i = 0; i < pixels.Length; i++)
                if (pixels[i].a == 0) pixels[i].r = pixels[i].g = pixels[i].b = 255;
        }

        internal static bool TryIcon(CarPaintSprites.SpriteInfo info, out Sprite icon)
        {
            icon = null;
            if (!TryTexture(info, out Texture2D texture)) return false;
            string key = Key(info);
            if (icons.TryGetValue(key, out icon) && icon) return true;
            icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
            icon.name = "ECT.Icon." + key;
            icons[key] = icon;
            return true;
        }

        internal static void RefreshCatalog()
        {
            if (NetManager.isServer) return;
            // SetSprite reinstates the vanilla preview when disabled and the modified
            // icon when enabled (via PrimitiveCatalogIconPatch postfix).
            foreach (var tile in UnityEngine.Resources.FindObjectsOfTypeAll<CarPaintSprite>())
                if (tile && tile.GetComponentInParent<CarGarageUI>() && tile.spriteInfo != null && Key(tile.spriteInfo) != null)
                    tile.SetSprite(tile.spriteInfo);
            foreach (var paint in Object.FindObjectsOfType<CarPaintItems>())
                if (paint) paint.UpImage();
        }

        internal static void Clear()
        {
            foreach (Sprite icon in icons.Values) if (icon) Object.Destroy(icon);
            icons.Clear();
            foreach (Texture2D texture in loaded.Values) if (texture) Object.Destroy(texture);
            loaded.Clear(); missing.Clear();
        }
    }
}
