using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>Profile: editable player name, race record (races / 1st / 2nd / 3rd) and level with XP progress.</summary>
    public sealed class ProfileScreen : MonoBehaviour
    {
        [SerializeField] private InputField nameInput;
        [SerializeField] private Text racesValue;
        [SerializeField] private Text winsValue;
        [SerializeField] private Text secondsValue;
        [SerializeField] private Text thirdsValue;
        [SerializeField] private Text levelValue;
        [SerializeField] private Text rankLabel;
        [SerializeField] private StatBar xpBar;
        [SerializeField] private Text xpLabel;

        private void Awake()
        {
            nameInput.characterLimit = GameSave.MaxNameLength;
            nameInput.onEndEdit.AddListener(SaveName);
        }

        private void OnEnable()
        {
            GameSave.ProfileChanged += Refresh;
            Refresh();
        }

        private void OnDisable() => GameSave.ProfileChanged -= Refresh;

        private void SaveName(string value) => GameSave.PlayerName = value;

        private void Refresh()
        {
            nameInput.SetTextWithoutNotify(GameSave.PlayerName);
            racesValue.text = GameSave.RacesPlayed.ToString("N0");
            winsValue.text = GameSave.Wins.ToString("N0");
            secondsValue.text = GameSave.Seconds.ToString("N0");
            thirdsValue.text = GameSave.Thirds.ToString("N0");

            int level = GameSave.PlayerLevel;
            levelValue.text = level.ToString();
            rankLabel.text = GameSave.RankTitle(level);
            xpBar.SetValue(GameSave.LevelProgress);
            int into = GameSave.Xp - GameSave.XpForLevel(level);
            int need = GameSave.XpForLevel(level + 1) - GameSave.XpForLevel(level);
            xpLabel.text = into.ToString("N0") + " / " + need.ToString("N0") + " XP";
        }
    }
}
