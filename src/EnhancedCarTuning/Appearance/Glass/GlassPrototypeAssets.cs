using System;
using EnhancedCarTuning.Resources;
using UnityEngine;

namespace EnhancedCarTuning.Appearance.Glass
{
    internal static class GlassPrototypeAssets
    {
        private static AssetBundle bundle;
        private static Shader shader;
        private static Shader finishShader;
        private static bool attempted;
        internal static Shader Shader
        {
            get
            {
                if (attempted) return shader;
                attempted = true;
                try
                {
                    var bytes = ResourceContainer.Read("glass-prototype.bundle");
                    if (bytes != null) bundle = AssetBundle.LoadFromMemory(bytes);
                    if (bundle) shader = bundle.LoadAsset<Shader>("Assets/GlassOverlay.shader");
                    if (bundle) finishShader = bundle.LoadAsset<Shader>("Assets/GlassFinish.shader");
                    if (!finishShader || !finishShader.isSupported) { finishShader = null; Plugin.Warn("Glass finish shader is unavailable on this graphics device."); }
                    if (!shader || !shader.isSupported) { shader = null; Plugin.Warn("Glass prototype shader is unavailable on this graphics device."); }
                }
                catch (Exception error) { Plugin.Warn("Cannot load glass prototype shader: " + error); }
                return shader;
            }
        }
        internal static Shader FinishShader { get { var loaded = Shader; return finishShader; } }
        internal static void Clear() { GlassOverlayRenderer.Clear(); shader = finishShader = null; if (bundle) bundle.Unload(true); bundle = null; attempted = false; }
    }
}
