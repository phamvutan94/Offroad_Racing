using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// World Tour track list. Replaces the old LevelSelect UI logic and keeps its PlayerPrefs keys
    /// ("LevelID", "Level{i}", "Award_Level_{i}", "TotalScores").
    /// </summary>
    public sealed class LevelSelector : MonoBehaviour
    {
        [Header("Tracks")]
        [SerializeField] private string[] sceneNames;
        [SerializeField] private int[] prices;
        [SerializeField] private string[] titles;
        [SerializeField] private string[] subtitles;
        [SerializeField] private Sprite[] thumbnails;
        [Tooltip("Track title logos for the loading screen (optional, same order as the tracks)")]
        [SerializeField] private Sprite[] titleArts;
        [SerializeField] private int[] podiumRewards = { 3000, 2000, 1000 };

        [Header("UI")]
        [SerializeField] private EventCardView[] cards;
        [SerializeField] private Text detailNumber;
        [SerializeField] private Text detailTitle;
        [SerializeField] private Text detailSubtitle;
        [SerializeField] private Text[] rewardLabels;
        [SerializeField] private Text startLabel;

        [Header("Flow")]
        [SerializeField] private UIDialog dialog;
        [SerializeField] private UIScreenManager manager;
        [SerializeField] private UIScreen carSelectScreen;

        private int selected;

        public string SelectedSceneName => sceneNames[selected];
        public Sprite SelectedThumbnail => thumbnails[selected];
        public string SelectedTitle => titles[selected];
        public Sprite SelectedTitleArt => titleArts != null && selected < titleArts.Length ? titleArts[selected] : null;

        private void Start()
        {
            selected = Mathf.Clamp(GameSave.SelectedLevel, 0, sceneNames.Length - 1);
            for (int i = 0; i < cards.Length; i++) cards[i].Setup(i, titles[i], subtitles[i], thumbnails[i]);
            for (int i = 0; i < rewardLabels.Length && i < podiumRewards.Length; i++) rewardLabels[i].text = podiumRewards[i].ToString("N0");
            Refresh();
        }

        private void OnEnable()
        {
            if (cards != null && cards.Length > 0 && cards[0] != null) Refresh();
        }

        /// Called by each card's Button (persistent int listener).
        public int TrackCount => sceneNames.Length;
        public string SceneNameAt(int index) => sceneNames[Mathf.Clamp(index, 0, sceneNames.Length - 1)];
        public string TitleAt(int index) => titles[Mathf.Clamp(index, 0, titles.Length - 1)];
        /// Re-reads unlocks after save data changed outside the World Tour UI (test menu).
        public void RefreshView() { if (cards != null && cards.Length > 0 && cards[0] != null) Refresh(); }

        public void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, sceneNames.Length - 1);
            Refresh();
        }

        /// START button: unlock the track if needed, otherwise continue to car selection.
        public void Continue()
        {
            if (GameSave.IsLevelUnlocked(selected))
            {
                GameSave.SelectedLevel = selected;
                manager.Show(carSelectScreen);
                return;
            }
            int price = prices[selected];
            dialog.Show("UNLOCK " + titles[selected], "Unlock this track for " + price.ToString("N0") + " coins?", "UNLOCK", () =>
            {
                if (GameSave.TrySpend(price)) { GameSave.SetLevelUnlocked(selected); Refresh(); }
                else dialog.ShowInfo("NOT ENOUGH COINS", "You need " + (price - GameSave.Coins).ToString("N0") + " more coins. Win races to earn more.");
            });
        }

        private void Refresh()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].SetState(GameSave.IsLevelUnlocked(i), prices[i], GameSave.GetBestFinish(i));
                cards[i].SetSelected(i == selected);
            }
            detailNumber.text = (selected + 1).ToString();
            detailTitle.text = titles[selected];
            detailSubtitle.text = subtitles[selected];
            startLabel.text = GameSave.IsLevelUnlocked(selected) ? "SELECT CAR" : "UNLOCK";
        }
    }
}
