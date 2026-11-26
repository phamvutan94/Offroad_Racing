using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// Single-line auto size for a legacy Text: picks the largest font size between min and max at which
    /// the whole string fits the rect on ONE line (Unity's Best Fit breaks long words into lines instead).
    /// Re-fits when the text or the rect size changes.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Text))]
    [DisallowMultipleComponent]
    public sealed class TextAutoFit : UIBehaviour
    {
        [SerializeField] private Text text;
        [SerializeField, Min(1)] private int maxSize = 32;
        [SerializeField, Min(1)] private int minSize = 14;

        private bool fitting;
        private string lastText;
        private Vector2 lastSize;

        public int MaxSize { get => maxSize; set { maxSize = value; Fit(true); } }

        protected override void Reset()
        {
            text = GetComponent<Text>();
            maxSize = text.fontSize;
            minSize = Mathf.Max(10, Mathf.RoundToInt(maxSize * 0.45f));
        }

        protected override void OnEnable()
        {
            if (text == null) text = GetComponent<Text>();
            text.RegisterDirtyVerticesCallback(OnTextDirty);
            Fit(true);
        }

        protected override void OnDisable()
        {
            if (text != null) text.UnregisterDirtyVerticesCallback(OnTextDirty);
        }

        protected override void OnRectTransformDimensionsChange() => Fit(false);

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (text == null) text = GetComponent<Text>();
            if (minSize > maxSize) minSize = maxSize;
            lastText = null;
        }
#endif

        private void OnTextDirty() => Fit(false);

        private void Fit(bool force)
        {
            if (fitting || text == null || !isActiveAndEnabled) return;
            Rect r = text.rectTransform.rect;
            if (r.width <= 0f) return;
            if (!force && text.text == lastText && r.size == lastSize) return;
            lastText = text.text;
            lastSize = r.size;

            fitting = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;

            TextGenerationSettings s = text.GetGenerationSettings(r.size);
            s.horizontalOverflow = HorizontalWrapMode.Overflow;
            s.verticalOverflow = VerticalWrapMode.Overflow;
            s.resizeTextForBestFit = false;
            s.scaleFactor = 1f;
            TextGenerator gen = text.cachedTextGeneratorForLayout;

            // binary search the largest size that fits on one line
            int lo = minSize, hi = Mathf.Max(minSize, maxSize), best = lo;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                s.fontSize = mid;
                float w = gen.GetPreferredWidth(text.text, s);
                float h = gen.GetPreferredHeight(text.text, s);
                if (w <= r.width + 0.5f && h <= r.height * 1.25f + 2f) { best = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            if (text.fontSize != best) text.fontSize = best;
            fitting = false;
        }
    }
}
