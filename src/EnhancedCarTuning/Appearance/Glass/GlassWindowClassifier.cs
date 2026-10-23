using System;
using UnityEngine;

namespace EnhancedCarTuning.Appearance.Glass
{
    internal static class GlassWindowClassifier
    {
        internal static bool TryLampGroup(string name, out GlassGroup group)
        {
            group = GlassGroup.LampsFront;
            string text = name ?? "";
            int start = text.IndexOf("_Win_Light_", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return false;
            string suffix = text.Substring(start + 11);
            if (suffix.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0 || suffix.IndexOf("Rear", StringComparison.OrdinalIgnoreCase) >= 0) group = GlassGroup.LampsBack;
            return suffix.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0 || suffix.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        internal static bool IsLampName(string name)
        {
            var text = (name ?? "").ToLowerInvariant();
            return text.Contains("light") || text.Contains("ligth") || text.Contains("lamp") || text.Contains("indicator") ||
                text.Contains("turnsignal") || text.Contains("blink") || text.Contains("oreol") || text.Contains("mirror");
        }
        internal static bool IsCabinWindow(CarWindow window, Transform view)
        {
            var loader = window.GetComponent<MeshLoadFromResources>();
            if (loader && IsLampName(loader.mesh_name)) return false;
            for (var node = window.transform; node && node != view; node = node.parent)
                if (node.GetComponent<CarLight>() || IsLampName(node.name)) return false;
            return true;
        }
        internal static GlassGroup Classify(string name, Vector3 center)
        {
            var text = (name ?? "").Trim().ToLowerInvariant();
            if(text.Contains("_win_top"))return GlassGroup.Sunroof;
            if (text.Contains("windshield") || text.Contains("windscreen")) return GlassGroup.Front;
            if (text.Contains("door") || text.Contains("mini") || text.Contains("side") || text.Contains("left") || text.Contains("right") ||
                text.Contains("_win_l_") || text.Contains("_win_r_") || text.EndsWith("_win_l", StringComparison.Ordinal) || text.EndsWith("_win_r", StringComparison.Ordinal)) return GlassGroup.Middle;
            if (text.Contains("front") || text.Contains("forward")) return GlassGroup.Front;
            if (text.Contains("back") || text.Contains("rear")) return GlassGroup.Back;
            return Math.Abs(center.x) > .25f ? GlassGroup.Middle : center.z < 0 ? GlassGroup.Back : GlassGroup.Front;
        }
        internal static GlassGroup Classify(CarWindow window, MeshRenderer renderer, Transform view)
        {
            var loader = window.GetComponent<MeshLoadFromResources>();
            return Classify(window.name + " " + (loader ? loader.mesh_name : ""), view.InverseTransformPoint(renderer.bounds.center));
        }
    }
}
