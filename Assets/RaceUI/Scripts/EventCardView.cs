using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>View for one track card in the World Tour list.</summary>
    public sealed class EventCardView : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private Text subtitle;
        [SerializeField] private Text number;
        [SerializeField] private Image thumbnail;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject selectedOutline;
        [SerializeField] private Text footer;
        [SerializeField] private Image footerIcon;
        [SerializeField] private Sprite lockIcon;
        [SerializeField] private Sprite trophyIcon;
        [SerializeField] private Color lockedColor = RaceUITheme.Gold;
        [SerializeField] private Color[] finishColors = { RaceUITheme.Gold, new Color(0.8f, 0.84f, 0.9f), RaceUITheme.ClassB, RaceUITheme.Muted };

        public void Setup(int index, string heading, string sub, Sprite preview)
        {
            title.text = heading;
            subtitle.text = sub;
            number.text = (index + 1).ToString();
            if (preview != null) thumbnail.sprite = preview;
        }

        public void SetState(bool unlocked, int price, int bestFinish)
        {
            lockedOverlay.SetActive(!unlocked);
            if (!unlocked)
            {
                footer.text = price.ToString("N0") + " COINS";
                footer.color = lockedColor;
                footerIcon.sprite = lockIcon;
                footerIcon.color = lockedColor;
                return;
            }
            int i = Mathf.Clamp(bestFinish, 0, 3);
            footer.text = i < 3 ? "BEST  " + GameSave.Ordinal(i) : "NO PODIUM YET";
            footer.color = finishColors[i];
            footerIcon.sprite = trophyIcon;
            footerIcon.color = finishColors[i];
        }

        public void SetSelected(bool selected) => selectedOutline.SetActive(selected);
    }
}
