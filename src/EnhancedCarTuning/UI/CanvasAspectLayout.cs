using UnityEngine;

namespace EnhancedCarTuning.UI
{
    /// <summary>A uniform 1920x1080 design-space transform centered inside the game's UI rect.
    /// Different aspect ratios add letterbox margins instead of stretching buttons and typography.</summary>
    internal static class CanvasAspectLayout
    {
        internal static void Apply(RectTransform root, RectTransform parent, RectTransform viewport, Vector3[] corners)
        {
            if (!root || !parent || !viewport) return;
            viewport.GetWorldCorners(corners);
            Vector3 lower = parent.InverseTransformPoint(corners[0]);
            Vector3 upper = parent.InverseTransformPoint(corners[2]);
            float w = Mathf.Abs(upper.x - lower.x), h = Mathf.Abs(upper.y - lower.y);
            float s = Mathf.Min(w / 1920f, h / 1080f);
            if (s <= 0f) return;
            root.anchorMin = root.anchorMax = root.pivot = Vector2.zero;
            root.localRotation = Quaternion.identity;
            root.sizeDelta = new Vector2(1920, 1080);
            root.localScale = new Vector3(s, s, 1f);
            root.localPosition = new Vector3(
                Mathf.Min(lower.x, upper.x) + (w - 1920f * s) * .5f,
                Mathf.Min(lower.y, upper.y) + (h - 1080f * s) * .5f,
                0f);
        }
    }
}
