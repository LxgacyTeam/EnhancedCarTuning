using UnityEngine;
using UnityEngine.UI;

namespace EnhancedCarTuning.Appearance.UI
{
    internal sealed class VinylStyleCheck : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Vector2 origin = new Vector2(rect.xMin, rect.yMin);
            Vector2 a = origin + new Vector2(rect.width * .12f, rect.height * .47f);
            Vector2 b = origin + new Vector2(rect.width * .38f, rect.height * .22f);
            Vector2 c = origin + new Vector2(rect.width * .89f, rect.height * .78f);
            float thickness = Mathf.Min(rect.width, rect.height) * .14f;
            Segment(mesh, a, b, thickness);
            Segment(mesh, b, c, thickness);
        }

        private void Segment(VertexHelper mesh, Vector2 start, Vector2 end, float thickness)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x) * thickness * .5f;
            start -= direction * thickness * .25f;
            end += direction * thickness * .25f;
            int index = mesh.currentVertCount;
            mesh.AddVert(start - side, color, Vector2.zero);
            mesh.AddVert(start + side, color, Vector2.zero);
            mesh.AddVert(end + side, color, Vector2.zero);
            mesh.AddVert(end - side, color, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index + 2, index + 3, index);
        }
    }
}
