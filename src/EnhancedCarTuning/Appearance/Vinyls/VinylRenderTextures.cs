using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedCarTuning.Appearance.Vinyls
{
    internal static class VinylRenderTextures
    {
        private sealed class Entry
        {
            internal RenderTexture Texture;
            internal float Retry;
            internal bool Warned;
        }
        private static readonly Dictionary<CarPaintItems, Entry> entries = new Dictionary<CarPaintItems, Entry>();
        internal static bool Ensure(CarPaintItems paint)
        {
            if (NetManager.isServer) return false;
            int resolution = Math.Min(EnhancedCarTuning.Configuration.TuningConfig.VinylTextureResolution, SystemInfo.maxTextureSize);
            bool text = false;
            foreach (var item in paint.items)
                if (TextVinylData.IsText(item.spriteName)) { text = true; break; }
            if (!text || resolution <= 512)
            {
                Release(paint);
                return paint.TryGetTextureRt();
            }
            entries.TryGetValue(paint, out var entry);
            if (entry != null && entry.Texture && paint.textureRt == entry.Texture &&
                entry.Texture.width == resolution && entry.Texture.IsCreated()) return true;
            if (entry != null && Time.unscaledTime < entry.Retry) return paint.TryGetTextureRt();
            entry = entry ?? new Entry();
            paint.UnloadRt();
            RenderTexture texture = null;
            try
            {
                texture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                {
                    name = "ECT.VinylBody." + resolution,
                    filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat,
                    antiAliasing = 1, useMipMap = false
                };
                if (!texture.Create()) throw new InvalidOperationException("RenderTexture.Create returned false.");
                entry.Texture = texture; entry.Retry = 0;
                entries[paint] = entry; paint.textureRt = texture;
                Apply(paint);
                return true;
            }
            catch (Exception error)
            {
                if (texture)
                {
                    if (paint.textureRt == texture) paint.textureRt = null;
                    texture.Release(); UnityEngine.Object.Destroy(texture);
                }
                entry.Texture = null; entry.Retry = Time.unscaledTime + 5;
                entries[paint] = entry;
                if (!entry.Warned) { entry.Warned = true; Plugin.Warn("High resolution vinyl texture unavailable; using native texture. " + error); }
                return paint.TryGetTextureRt();
            }
        }
        internal static bool Release(CarPaintItems paint)
        {
            if (!entries.TryGetValue(paint, out var entry)) return false;
            entries.Remove(paint);
            if (!entry.Texture) return false;
            bool attached = paint && paint.textureRt == entry.Texture;
            if (attached) paint.textureRt = null;
            entry.Texture.Release(); UnityEngine.Object.Destroy(entry.Texture);
            if (attached) Apply(paint);
            return attached;
        }
        private static void Apply(CarPaintItems paint)
        { if (paint && paint.car && paint.car.carMaterial) paint.car.carMaterial.Apply(); }
        internal static void Trim()
        {
            foreach (var paint in new List<CarPaintItems>(entries.Keys))
            {
                bool text = false;
                if (paint) foreach (var item in paint.items)
                    if (TextVinylData.IsText(item.spriteName)) { text = true; break; }
                if (!text) Release(paint);
            }
        }
        internal static void Clear()
        { foreach (var paint in new List<CarPaintItems>(entries.Keys)) Release(paint); }
    }
}
