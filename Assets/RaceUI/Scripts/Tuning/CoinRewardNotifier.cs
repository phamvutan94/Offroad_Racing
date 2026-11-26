using UnityEngine;

namespace RaceUI
{
    /// <summary>In the garage: shows the "coins received" popup for coins won since the last visit.</summary>
    public sealed class CoinRewardNotifier : MonoBehaviour
    {
        [SerializeField] private UIPopup popup;
        [SerializeField, Min(0f)] private float delay = 0.6f;

        private void Start()
        {
            if (GameSave.PendingReward > 0) Invoke(nameof(ShowPending), delay);
        }

        private void ShowPending()
        {
            int coins = GameSave.PendingReward;
            if (coins <= 0) return;
            string source = GameSave.PendingRewardSource;
            GameSave.ClearPendingReward();
            popup.ShowCoinsReceived(coins, string.IsNullOrEmpty(source) ? "Added to your wallet." : source);
        }
    }
}
