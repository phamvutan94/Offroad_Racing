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

        private void Reset() => slider = GetComponent<Slider>();

        public void SetValue(float normalized)
        {
            slider.normalizedValue = normalized;
            if (valueText != null) valueText.text = string.Format(valueFormat, normalized * 100f);
        }

        public void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }
    }
}
