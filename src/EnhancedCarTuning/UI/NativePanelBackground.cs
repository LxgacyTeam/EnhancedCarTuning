using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EnhancedCarTuning.UI
{
    internal sealed class NativePanelBackground : MonoBehaviour
    {
        private Graphic source, target;
        private readonly Vector3[] corners = new Vector3[4];

        internal static void Attach(RectTransform rect, CarGarageUI garage)
        {
            var blur = FindBlur(MenuEsc.me);
            if (!blur)
                foreach (var menu in UnityEngine.Resources.FindObjectsOfTypeAll<MenuEsc>())
                { blur = FindBlur(menu); if (blur) break; }
            var main = garage.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Main");
            var panel = main ? main.Find("Panel") : null;
            var style = panel ? panel.GetComponent<Graphic>() : null;
            // Panel already references the game's blur material on some builds.
            var source = blur ? blur : style;
            if (!source)
            {
                var fallback = rect.gameObject.AddComponent<Image>();
                fallback.color = new Color(0.07f, 0.08f, 0.09f, 0.75f); fallback.raycastTarget = false;
                Plugin.Warn("Native BLUR / Main/Panel not found; using translucent suspension background.");
                return;
            }
            Graphic target;
            if (source is RawImage) target = rect.gameObject.AddComponent<RawImage>();
            else target = rect.gameObject.AddComponent<Image>();
            target.color = style ? style.color : source.color;
            target.raycastTarget = false;
            var binding = rect.gameObject.AddComponent<NativePanelBackground>();
            binding.source = source; binding.target = target; binding.Sync();
            // This is the native enable/disable registration; it does not create another blur camera.
            rect.gameObject.AddComponent<BlurBehindRequared>();
        }

        private static Graphic FindBlur(MenuEsc menu)
        {
            if (!menu) return null;
            var node = menu.transform.Find("Root/Back ground BLUR/BLUR");
            return node ? node.GetComponent<Graphic>() : null;
        }

        private void LateUpdate() { Sync(); }
        private void Sync()
        {
            if (!source || !target) return;
            // The game may replace its blur render texture; keep the copy bound to the live source.
            if (target.material != source.material) target.material = source.material;
            if (source is RawImage raw && target is RawImage output)
            {
                if (output.texture != raw.texture) output.texture = raw.texture;
                // Crop a full-screen raw texture to the panel's area instead of squeezing the whole screen into it.
                var sourceRect = raw.rectTransform.rect;
                if (sourceRect.width > 0.001f && sourceRect.height > 0.001f)
                {
                    output.rectTransform.GetWorldCorners(corners);
                    var low = raw.rectTransform.InverseTransformPoint(corners[0]);
                    var high = raw.rectTransform.InverseTransformPoint(corners[2]);
                    var uv = raw.uvRect;
                    output.uvRect = new Rect(uv.x + (low.x - sourceRect.xMin) / sourceRect.width * uv.width,
                        uv.y + (low.y - sourceRect.yMin) / sourceRect.height * uv.height,
                        (high.x - low.x) / sourceRect.width * uv.width, (high.y - low.y) / sourceRect.height * uv.height);
                }
                else output.uvRect = raw.uvRect;
            }
            else if (source is Image image && target is Image copy)
            { copy.sprite = image.overrideSprite; copy.type = image.type; copy.fillCenter = image.fillCenter; copy.preserveAspect = false; }
        }
    }
}
