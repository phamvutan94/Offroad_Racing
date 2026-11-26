using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>Garage player card: name, rank title and level from the saved profile.</summary>
    public sealed class PlayerCardView : MonoBehaviour
    {
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text rankLabel;
        [SerializeField] private Text levelLabel;

        private void OnEnable()
        {
            GameSave.ProfileChanged += Refresh;
            Refresh();
        }

        private void OnDisable() => GameSave.ProfileChanged -= Refresh;

        private void Refresh()
        {
            int level = GameSave.PlayerLevel;
            nameLabel.text = GameSave.PlayerName;
            rankLabel.text = GameSave.RankTitle(level);
            levelLabel.text = level.ToString();
        }
    }
}
