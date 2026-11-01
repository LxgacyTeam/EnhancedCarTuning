using UnityEngine;

namespace EnhancedCarTuning.Appearance.Glass
{
    internal sealed class GlassVinylPreview : MonoBehaviour
    {
        private Renderer preview;
        private bool wasEnabled;

        internal void Refresh(Renderer renderer, bool hide)
        {
            if (!hide || preview != renderer) Restore();
            if (!hide || !renderer) return;
            if (!preview) { preview = renderer; wasEnabled = renderer.enabled; }
            renderer.enabled = false;
        }

        private void Restore()
        {
            if (preview) preview.enabled = wasEnabled;
            preview = null;
        }

        private void OnDisable() { Restore(); }
        private void OnDestroy() { Restore(); }
    }
}
