using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>Shows the player's coins (PlayerPrefs "TotalScores") and updates when they change.</summary>
    public sealed class CoinPill : MonoBehaviour
    {
        [SerializeField] private Text amountText;
        [SerializeField] private bool bindToSave = true;
        [SerializeField] private int previewAmount = 385;

        private void OnEnable()
        {
            if (!bindToSave) { SetAmount(previewAmount); return; }
            GameSave.CoinsChanged += SetAmount;
            SetAmount(GameSave.Coins);
        }

        // MainUtility.Awake may write the first-run coins after our OnEnable; re-read once everything is awake.
        private void Start()
        {
            if (bindToSave) SetAmount(GameSave.Coins);
        }

        private void OnDisable()
        {
            if (bindToSave) GameSave.CoinsChanged -= SetAmount;
        }

        public void SetAmount(int value)
        {
            if (amountText != null) amountText.text = value.ToString("N0");
        }
    }
}
