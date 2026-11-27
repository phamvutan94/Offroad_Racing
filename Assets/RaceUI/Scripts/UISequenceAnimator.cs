using System.Collections;
using UnityEngine;

namespace RaceUI
{
    /// <summary>
    /// Plays an entrance sequence every time the object is enabled: each step slides / scales / fades one element
    /// in after its delay. Steps are set up in the Inspector; uses real time so it also runs while paused.
    /// </summary>
    public sealed class UISequenceAnimator : MonoBehaviour
    {
        public enum Ease { OutCubic, OutBack, OutElastic }

        [System.Serializable]
        public sealed class Step
        {
            public RectTransform target;
            [Tooltip("Optional: faded from 0 to 1")] public CanvasGroup group;
            public float delay;
            public float duration = 0.35f;
            [Tooltip("Start offset in canvas units, relative to the element's laid-out position")] public Vector2 fromOffset;
            [Tooltip("Start scale (1 = no scale animation)")] public float fromScale = 1f;
            public Ease ease = Ease.OutCubic;

            [System.NonSerialized] public Vector2 restPosition;
            [System.NonSerialized] public Vector3 restScale;
        }

        [SerializeField] private Step[] steps;
        [SerializeField] private bool playOnEnable = true;
        [Tooltip("Added to every step's delay (e.g. let a finish celebration play first)")]
        [SerializeField] private float startDelay;

        public float Duration
        {
            get { float d = 0; foreach (var s in steps) if (s.target != null) d = Mathf.Max(d, s.delay + s.duration); return startDelay + d; }
        }

        private bool captured;

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            Restore();
        }

        public void Play()
        {
            Capture();
            StopAllCoroutines();
            foreach (var s in steps)
            {
                if (s.target == null) continue;
                Apply(s, 0f);
                StartCoroutine(Run(s));
            }
        }

        private void Capture()
        {
            if (captured) return;
            captured = true;
            foreach (var s in steps)
            {
                if (s.target == null) continue;
                s.restPosition = s.target.anchoredPosition;
                s.restScale = s.target.localScale;
            }
        }

        private void Restore()
        {
            if (!captured) return;
            foreach (var s in steps) if (s.target != null) Apply(s, 1f);
        }

        private IEnumerator Run(Step s)
        {
            // wall-clock time: independent of timeScale and of editor frame hitches
            float start = Time.realtimeSinceStartup + startDelay + s.delay;
            while (Time.realtimeSinceStartup < start) yield return null;
            for (float t = 0; t < s.duration; t = Time.realtimeSinceStartup - start)
            {
                Apply(s, t / s.duration);
                yield return null;
            }
            Apply(s, 1f);
        }

        private static void Apply(Step s, float t)
        {
            float k = Evaluate(s.ease, Mathf.Clamp01(t));
            float lin = Mathf.Clamp01(t);
            s.target.anchoredPosition = s.restPosition + s.fromOffset * (1f - k);
            float sc = Mathf.LerpUnclamped(s.fromScale, 1f, k);
            s.target.localScale = s.restScale * sc;
            if (s.group != null) s.group.alpha = Mathf.Clamp01(lin * 2.2f);
        }

        private static float Evaluate(Ease e, float t)
        {
            switch (e)
            {
                case Ease.OutBack:
                    { const float c1 = 1.70158f, c3 = c1 + 1f; float u = t - 1f; return 1f + c3 * u * u * u + c1 * u * u; }
                case Ease.OutElastic:
                    if (t <= 0f || t >= 1f) return t;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
                default:
                    { float u = 1f - t; return 1f - u * u * u; }
            }
        }
    }
}
