using UnityEngine;

namespace EnhancedCarTuning.Appearance.Body
{
    internal sealed class BodyPaintState : MonoBehaviour
    {
        internal CarMaterial CarMaterial { get; private set; }
        internal BodyPaintSettings Committed { get; private set; } = BodyPaintSettings.Empty;
        internal BodyPaintSettings Active { get; private set; } = BodyPaintSettings.Empty;
        internal bool SerializePreview { get; set; }
        internal BodyPaintRenderer Renderer { get; private set; }
        private bool needDraw;
        internal static BodyPaintState Ensure(CarMaterial material)
        {
            if (!material) return null;
            var state = material.GetComponent<BodyPaintState>() ?? material.gameObject.AddComponent<BodyPaintState>();
            if (!state.CarMaterial)
            { state.CarMaterial = material; if (!NetManager.isServer) state.Renderer = new BodyPaintRenderer(state); }
            return state;
        }
        internal void Preview(BodyPaintSettings settings)
        {
            ApplyBase(settings);
            if (settings.Equals(Active)) return;
            Active = settings; Renderer?.Sync(); Invalidate();
        }
        internal void Commit(BodyPaintSettings settings) { Preview(settings); Committed = settings; }
        internal void ApplyBase(BodyPaintSettings settings)
        { if (CarMaterial) settings[BodyPart.Body]?.Apply(CarMaterial.body); }
        internal void Invalidate() { needDraw = true; }
        private void LateUpdate()
        {
            if (!CarMaterial || Renderer == null) return;
            Renderer.Sync();
            if (!needDraw || !CarMaterial.paintItems || !CarMaterial.car || !CarMaterial.car.gameObject.activeInHierarchy) return;
            needDraw = false; CarMaterial.paintItems.UpImage();
        }
        internal void ResetForPool()
        { Active = Committed = BodyPaintSettings.Empty; SerializePreview = false; needDraw = false; Renderer?.Reset(); }
        private void OnDisable() { ResetForPool(); }
        private void OnDestroy() { Renderer?.Dispose(); Renderer = null; }
    }
}
