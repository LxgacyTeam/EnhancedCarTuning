using UnityEngine;
using UnityEngine.UI;

namespace EnhancedCarTuning.Wheels.UI
{
    internal sealed class WheelSwitchArrow : MaskableGraphic
    {
        internal bool Left;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); var r = rectTransform.rect;
            float tip = Left ? r.xMin : r.xMax, tail = Left ? r.xMax : r.xMin;
            helper.AddVert(new Vector3(tip, r.center.y), color, Vector2.zero);
            helper.AddVert(new Vector3(tail, r.yMax), color, Vector2.zero);
            helper.AddVert(new Vector3(tail, r.yMin), color, Vector2.zero);
            helper.AddTriangle(0, 1, 2);
        }
    }
}
