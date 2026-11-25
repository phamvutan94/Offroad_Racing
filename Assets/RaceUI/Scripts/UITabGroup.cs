using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>Tabs that switch pages. Each tab Button calls Select(index) via its OnClick list.</summary>
    public sealed class UITabGroup : MonoBehaviour
    {
        [SerializeField] private Image[] tabBackgrounds;
        [SerializeField] private Text[] tabLabels;
        [SerializeField] private GameObject[] pages;
        [SerializeField] private int selected;
        [SerializeField] private Color selectedColor = RaceUITheme.Accent;
        [SerializeField] private Color normalColor = RaceUITheme.Mid;
        [SerializeField] private Color selectedLabel = Color.white;
        [SerializeField] private Color normalLabel = RaceUITheme.Muted;

        private void Start() => Select(selected);

        public void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, Mathf.Max(0, tabBackgrounds.Length - 1));
            for (int i = 0; i < tabBackgrounds.Length; i++)
            {
                bool on = i == selected;
                if (tabBackgrounds[i] != null) tabBackgrounds[i].color = on ? selectedColor : normalColor;
                if (i < tabLabels.Length && tabLabels[i] != null) tabLabels[i].color = on ? selectedLabel : normalLabel;
                if (i < pages.Length && pages[i] != null) pages[i].SetActive(on);
            }
        }
    }
}
