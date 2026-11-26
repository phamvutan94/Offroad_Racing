using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>Read-only bar (stats, loading, progress) backed by a Slider.</summary>
    public sealed class StatBar : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Text label;
        [SerializeField] private Text valueText;
        [SerializeField] private string valueFormat = "{0:0}%";
        [Tooltip("Time AnimateTo takes to reach most of the way to the new value")]
        [SerializeField] private float smoothTime = 0.25f;

        private float shown;
        private float target;
        private float velocity;
        private bool animating;

        private void Reset() => slider = GetComponent<Slider>();

        /// Jumps straight to the value (loading bars, first fill).
        public void SetValue(float normalized)
        {
            animating = false;
            velocity = 0f;
            target = normalized;
            Show(normalized);
        }

        /// Slides from the current value to the new one (stat bars when the car changes).
        public void AnimateTo(float normalized)
        {
            target = normalized;
            animating = !Mathf.Approximately(shown, target);
        }

        public void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }

        private void Update()
        {
            if (!animating) return;
            float next = Mathf.SmoothDamp(shown, target, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            if (Mathf.Abs(next - target) < 0.001f) { next = target; animating = false; velocity = 0f; }
            Show(next);
        }

        private void Show(float normalized)
        {
            shown = normalized;
            slider.normalizedValue = normalized;
            if (valueText != null) valueText.text = string.Format(valueFormat, normalized * 100f);
        }
    }
}
