using UnityEngine;
using UnityEngine.UI;

namespace EnhancedCarTuning.Wheels.UI
{
    internal static class WheelSelectorView
    {
        internal static Text Create(RectTransform frame, Text title, Font font, out WheelSwitchArrow[] arrows)
        {
            Fit(title.rectTransform, .18f, .60f, .82f, .96f); title.alignment = TextAnchor.MiddleCenter;
            var text = new GameObject("Value", typeof(RectTransform)).AddComponent<Text>();
            text.transform.SetParent(frame, false); Fit(text.rectTransform, .16f, .07f, .84f, .58f);
            text.font = font; text.alignment = TextAnchor.MiddleCenter; text.fontSize = text.resizeTextMaxSize = 28;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.color = title.color; text.raycastTarget = false;
            arrows = new WheelSwitchArrow[2];
            for (int i = 0; i < 2; i++)
            {
                bool left = i == 0;
                var rect = (RectTransform)new GameObject(left ? "Previous" : "Next", typeof(RectTransform)).transform;
                rect.SetParent(frame, false); Fit(rect, left ? .035f : .885f, .32f, left ? .115f : .965f, .65f);
                var arrow = rect.gameObject.AddComponent<WheelSwitchArrow>(); arrow.Left = left; arrow.raycastTarget = false;
                arrow.color = new Color(.88f, .9f, .91f, 1); arrows[i] = arrow;
            }
            return text;
        }
        internal static void Fit(RectTransform rect, float x1, float y1, float x2, float y2)
        {
            rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            rect.anchorMin = new Vector2(x1, y1); rect.anchorMax = new Vector2(x2, y2);
            rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
    internal static class WheelControlLabels
    {
        internal static void Format(Text text, Transform parent, Font font, bool value)
        {
            text.transform.SetParent(parent, false);
            WheelSelectorView.Fit(text.rectTransform, value ? .60f : .06f, .45f, value ? .95f : .57f, .96f);
            text.font = font; text.fontSize = 34; text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16; text.resizeTextMaxSize = 34;
            text.alignment = value ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = new Color(.88f, .9f, .91f); text.raycastTarget = false;
        }
    }
}
