using UnityEngine;
using UnityEngine.UI;

namespace LockedIn
{
    /// <summary>A small, untextured instrument face. Rebuilds only when the value changes.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FocusDialGraphic : MaskableGraphic
    {
        private float amount;
        public float Amount
        {
            get => amount;
            set { value = Mathf.Clamp01(value); if (Mathf.Abs(value - amount) < 0.001f) return; amount = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.43f;
            for (int i = 0; i <= 40; i++)
            {
                float t = i / 40f;
                float angle = (210 - t * 240) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 normal = new Vector2(-direction.y, direction.x) * 1.2f;
                float length = i % 5 == 0 ? 12 : 6;
                Color ink = t <= amount ? color : new Color(color.r, color.g, color.b, 0.16f);
                int start = mesh.currentVertCount;
                mesh.AddVert(direction * radius - normal, ink, Vector2.zero);
                mesh.AddVert(direction * radius + normal, ink, Vector2.zero);
                mesh.AddVert(direction * (radius - length) + normal, ink, Vector2.zero);
                mesh.AddVert(direction * (radius - length) - normal, ink, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
