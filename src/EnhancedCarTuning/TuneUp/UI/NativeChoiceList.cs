using UnityEngine;

namespace EnhancedCarTuning.TuneUp.UI
{
    // Native MenuButtonGroup hit testing + MenuMaskArea. No additional EventSystem/Canvas.
    internal sealed class NativeChoiceList : MonoBehaviour
    {
        internal RectTransform Content;
        internal float ContentHeight;
        private RectTransform viewport;

        internal bool ContainsPointer()
        {
            if (!gameObject.activeInHierarchy) return false;
            if (!viewport) viewport = (RectTransform)transform;
            return viewport.isMouseIntersect(Input.mousePosition);
        }

        private void Update()
        {
            if (!Content || !ContainsPointer()) return;
            float delta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(delta) < 0.001f) return;
            Content.anchoredPosition = new Vector2(Content.anchoredPosition.x,
                Mathf.Clamp(Content.anchoredPosition.y - delta * 55f, 0, Mathf.Max(0, ContentHeight - viewport.rect.height)));
        }
    }
}
