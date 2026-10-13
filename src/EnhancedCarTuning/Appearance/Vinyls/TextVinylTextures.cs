using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedCarTuning.Appearance.Vinyls
{
    internal static class TextVinylTextures
    {
        private sealed class Entry
        {
            internal CarPaintSprites.SpriteInfo Info;
            internal Texture2D Texture;
            internal float Used;
            internal float Retry;
            internal bool Warned;
        }
        private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

        internal static CarPaintSprites.SpriteInfo Info(string name)
        {
            if (!TextVinylData.TryRead(name, out _)) return null;
            if (!entries.TryGetValue(name, out var entry))
            {
                entry = new Entry { Info = new CarPaintSprites.SpriteInfo {
                    texutreName = name, fullPath = name, texW = TextVinylRaster.Width, texH = TextVinylRaster.Height,
                    type = CarPaintSprites.TypeVinil.Vinil, price = 0, isOwn = true, useCoins = true } };
                entries.Add(name, entry);
            }
            entry.Used = Time.unscaledTime;
            return entry.Info;
        }
        internal static Texture Texture(string name)
        {
            if (Info(name) == null || NetManager.isServer) return null;
            var entry = entries[name];
            if (!entry.Texture)
            {
                if (Time.unscaledTime < entry.Retry) return null;
                TextVinylData.TryRead(name, out var data);
                try { entry.Texture = Rasterize(data); }
                catch (Exception error)
                {
                    entry.Retry = Time.unscaledTime + 5;
                    if (!entry.Warned) { entry.Warned = true; Plugin.Warn("Cannot rasterize text vinyl: " + error); }
                }
            }
            return entry.Texture;
        }
        private static Texture2D Rasterize(TextVinylData data)
        {
            var texture = new Texture2D(TextVinylRaster.Width, TextVinylRaster.Height, TextureFormat.RGBA32, false);
            texture.name = "ECT.TextVinyl";
            texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
            try
            {
                texture.LoadRawTextureData(TextVinylRaster.Render(data));
                texture.Apply(false, true); return texture;
            }
            catch { UnityEngine.Object.Destroy(texture); throw; }
        }
        internal static void Trim()
        {
            foreach (var entry in entries.Values)
                if (entry.Texture && Time.unscaledTime - entry.Used > 30)
                { UnityEngine.Object.Destroy(entry.Texture); entry.Texture = null; }
        }
        internal static void Clear()
        {
            foreach (var entry in entries.Values) if (entry.Texture) UnityEngine.Object.Destroy(entry.Texture);

            entries.Clear();
        }
    }
}

