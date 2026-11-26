using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// Hexagon radar for the tuning stats. Mode Fill draws the stat polygon (with outline);
    /// mode Grid draws the background rings and spokes.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RadarChart : MaskableGraphic
    {
        public enum Mode { Fill, Grid }

        [SerializeField] private Mode mode = Mode.Fill;
        [SerializeField] private float maxValue = 10f;
        [SerializeField] private float[] values = { 5, 5, 5, 5, 5, 5 };
        [SerializeField] private float outlineWidth = 4f;
        [SerializeField] private Color outlineColor = new Color(0.55f, 0.95f, 0.45f, 1f);
        [SerializeField] private int rings = 4;
        [SerializeField] private float lineWidth = 2f;
        [Tooltip("Degrees of the first axis (90 = straight up)")]
        [SerializeField] private float startAngle = 90f;

        public void SetValues(float[] v)
        {
            values = (float[])v.Clone();
            SetVerticesDirty();
        }

        Vector2 Axis(int i, int n)
        {
            float a = (startAngle - 360f / n * i) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int n = Mathf.Max(3, values.Length);
            Rect r = GetPixelAdjustedRect();
            Vector2 c = r.center; float rad = Mathf.Min(r.width, r.height) * 0.5f;
            if (mode == Mode.Grid)
            {
                for (int k = 1; k <= rings; k++)
                {
                    float rr = rad * k / rings;
                    for (int i = 0; i < n; i++) Line(vh, c + Axis(i, n) * rr, c + Axis((i + 1) % n, n) * rr, lineWidth, color);
                }
                for (int i = 0; i < n; i++) Line(vh, c, c + Axis(i, n) * rad, lineWidth, color);
                return;
            }
            var pts = new Vector2[n];
            for (int i = 0; i < n; i++) pts[i] = c + Axis(i, n) * rad * Mathf.Clamp01((i < values.Length ? values[i] : 0f) / maxValue);
            int centre = vh.currentVertCount; vh.AddVert(c, color, Vector2.zero);
            for (int i = 0; i < n; i++) vh.AddVert(pts[i], color, Vector2.zero);
            for (int i = 0; i < n; i++) vh.AddTriangle(centre, centre + 1 + i, centre + 1 + (i + 1) % n);
            if (outlineWidth > 0) for (int i = 0; i < n; i++) Line(vh, pts[i], pts[(i + 1) % n], outlineWidth, outlineColor);
        }

        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float w, Color col)
        {
            Vector2 d = (b - a); if (d.sqrMagnitude < 1e-4f) return;
            Vector2 nrm = new Vector2(-d.y, d.x).normalized * w * 0.5f;
            int i = vh.currentVertCount;
            vh.AddVert(a - nrm, col, Vector2.zero); vh.AddVert(a + nrm, col, Vector2.zero);
            vh.AddVert(b + nrm, col, Vector2.zero); vh.AddVert(b - nrm, col, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
