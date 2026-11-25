using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>Row of options with an underline under the selected one. Option Buttons call Select(index).</summary>
    public sealed class UISegmentedOption : MonoBehaviour
    {
        [System.Serializable] public sealed class IntEvent : UnityEvent<int> { }

        [SerializeField] private Image[] underlines;
        [SerializeField] private Text[] labels;
        [SerializeField] private int selected;
        [SerializeField] private Color selectedBar = RaceUITheme.Green;
        [SerializeField] private Color normalBar = new Color(1f, 1f, 1f, 0.18f);
        [SerializeField] private Color selectedLabel = Color.white;
        [SerializeField] private Color normalLabel = RaceUITheme.Muted;
        [SerializeField] private IntEvent onChanged = new IntEvent();

        public int Selected => selected;
        public IntEvent OnChanged => onChanged;

        private void Start() => Apply();

        /// Called by the option buttons (persistent listeners).
        public void Select(int index)
        {
            SetWithoutNotify(index);
            onChanged.Invoke(selected);
        }

        public void SetWithoutNotify(int index)
        {
            selected = Mathf.Clamp(index, 0, Mathf.Max(0, underlines.Length - 1));
            Apply();
        }

        private void Apply()
        {
            for (int i = 0; i < underlines.Length; i++)
            {
                bool on = i == selected;
                if (underlines[i] != null) underlines[i].color = on ? selectedBar : normalBar;
                if (i < labels.Length && labels[i] != null) labels[i].color = on ? selectedLabel : normalLabel;
            }
        }
    }
}
