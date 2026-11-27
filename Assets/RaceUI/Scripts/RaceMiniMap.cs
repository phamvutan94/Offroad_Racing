using ALIyerEdon;
using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// North-up mini map of the whole circuit: draws the track outline from the scene's checkpoints
    /// (smoothed) and moves one dot per racer. The player's dot is the bigger one and turns with the car.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RaceMiniMap : MaskableGraphic
    {
        [SerializeField] private RaceSceneLinks links;
        [SerializeField] private float lineWidth = 7f;
        [SerializeField] private float outlineWidth = 3f;
        [SerializeField] private Color outlineColor = new Color(0.05f, 0.09f, 0.15f, 0.9f);
        [SerializeField] private float padding = 12f;
        [SerializeField] private int samplesPerSegment = 6;

        [Header("Markers (in the prefab)")]
        [SerializeField] private RectTransform playerDot;
        [SerializeField] private RectTransform[] racerDots;
        [SerializeField] private RectTransform startMarker;

        private Vector2[] track;          // world xz of the smoothed outline
        private Rect worldRect;
        private float scale;
        private Vector2 offset;
        private bool built;

        protected override void Start()
        {
            base.Start();
            if (Application.isPlaying) Build();
        }

        private void Build()
        {
            if (links == null || links.CheckpointManager == null) return;
            var cps = links.CheckpointManager.checkpoints;
            if (cps == null || cps.Count < 3) return;
            int n = cps.Count;
            var ctrl = new Vector2[n];
            for (int i = 0; i < n; i++) ctrl[i] = new Vector2(cps[i].position.x, cps[i].position.z);
            int k = Mathf.Max(1, samplesPerSegment);
            track = new Vector2[n * k];
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = ctrl[(i - 1 + n) % n], p1 = ctrl[i], p2 = ctrl[(i + 1) % n], p3 = ctrl[(i + 2) % n];
                for (int j = 0; j < k; j++)
                {
                    float t = j / (float)k;
                    track[i * k + j] = 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
                }
            }
            Vector2 min = track[0], max = track[0];
            foreach (var p in track) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            worldRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            built = true;
            Fit();
            if (startMarker != null) startMarker.anchoredPosition = ToLocal(ctrl[0]);
            if (playerDot != null) playerDot.SetAsLastSibling();
            SetVerticesDirty();
        }

        private void Fit()
        {
            Rect r = rectTransform.rect;
            float w = Mathf.Max(1f, r.width - padding * 2f), h = Mathf.Max(1f, r.height - padding * 2f);
            scale = Mathf.Min(w / Mathf.Max(1f, worldRect.width), h / Mathf.Max(1f, worldRect.height));
            offset = r.center - worldRect.center * scale;
        }

        private Vector2 ToLocal(Vector2 worldXZ) => worldXZ * scale + offset;

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (built) { Fit(); SetVerticesDirty(); }
        }

        private void LateUpdate()
        {
            if (!built || links == null || links.RaceManager == null) return;
            Race_Manager rm = links.RaceManager;
            Car_Position[] cars = rm.CarPositions;
            Car_Position player = rm.PlayerPosition;
            int dot = 0;
            if (cars != null)
                foreach (Car_Position c in cars)
                {
                    if (c == null) continue;
                    Vector3 p = c.transform.position;
                    Vector2 local = ToLocal(new Vector2(p.x, p.z));
                    if (c == player)
                    {
                        if (playerDot == null) continue;
                        playerDot.anchoredPosition = local;
                        Vector3 f = c.transform.forward;
                        playerDot.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg);
                    }
                    else if (dot < racerDots.Length)
                    {
                        racerDots[dot].gameObject.SetActive(true);
                        racerDots[dot++].anchoredPosition = local;
                    }
                }
            for (int i = dot; i < racerDots.Length; i++) racerDots[i].gameObject.SetActive(false);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!built || track == null) return;
            Strip(vh, lineWidth + outlineWidth * 2f, outlineColor);
            Strip(vh, lineWidth, color);
        }

        private void Strip(VertexHelper vh, float width, Color col)
        {
            int n = track.Length;
            int start = vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = ToLocal(track[(i - 1 + n) % n]), b = ToLocal(track[i]), c = ToLocal(track[(i + 1) % n]);
                Vector2 dir = (c - a); if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
                Vector2 nrm = new Vector2(-dir.y, dir.x).normalized * width * 0.5f;
                vh.AddVert(b - nrm, col, Vector2.zero);
                vh.AddVert(b + nrm, col, Vector2.zero);
            }
            for (int i = 0; i < n; i++)
            {
                int i0 = start + i * 2, i1 = start + ((i + 1) % n) * 2;
                vh.AddTriangle(i0, i0 + 1, i1 + 1);
                vh.AddTriangle(i0, i1 + 1, i1);
            }
        }
    }
}
