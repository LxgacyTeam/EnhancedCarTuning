using UnityEngine;
using UnityEngine.UI;

namespace EnhancedCarTuning.Appearance.UI
{
    internal sealed class PolicePaintArrow : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            mesh.AddVert(new Vector2(rect.xMin, rect.center.y), color, Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax, rect.yMax), color, Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax, rect.yMin), color, Vector2.zero);
            mesh.AddTriangle(0, 1, 2);
        }
    }
}
