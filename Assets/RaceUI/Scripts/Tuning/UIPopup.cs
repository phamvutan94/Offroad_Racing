using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// Reward-style popup (big icon, title, message, optional coin amount, one or two buttons).
    /// Used for "upgrade complete", "not enough coins" and "coins received".
    /// </summary>
    public sealed class UIPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Image glow;
        [SerializeField] private Image icon;
        [SerializeField] private Text title;
        [SerializeField] private Text message;
        [SerializeField] private GameObject amountGroup;
        [SerializeField] private Text amount;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Text primaryLabel;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private Text secondaryLabel;

        [Header("Looks")]
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Color successColor = RaceUITheme.Green;
        [SerializeField] private Color warningColor = RaceUITheme.Accent;
        [SerializeField] private Color rewardColor = RaceUITheme.Gold;

        private Action onPrimary, onSecondary;
        private bool listening;

        private void Awake() => Listen();

        private void Listen()
        {
            if (listening) return;
            listening = true;
            primaryButton.onClick.AddListener(() => Close(onPrimary));
            secondaryButton.onClick.AddListener(() => Close(onSecondary));
        }

        public void ShowUpgradeSuccess(Sprite partSprite, string heading, string body) =>
            Show(partSprite, successColor, heading, body, null, "GREAT", null, null, null);

        public void ShowNotEnoughCoins(int missing, Action onGetCoins = null) =>
            Show(coinSprite, warningColor, "NOT ENOUGH COINS", "You need " + missing.ToString("N0") + " more coins.\nWin races to earn more.",
                 null, "OK", null, onGetCoins != null ? "RACE" : null, onGetCoins);

        public void ShowCoinsReceived(int coins, string body) =>
            Show(coinSprite, rewardColor, "COINS RECEIVED", body, "+" + coins.ToString("N0"), "COLLECT", null, null, null);

        public void Show(Sprite iconSprite, Color accent, string heading, string body, string amountText,
                         string primaryText, Action primaryAction, string secondaryText, Action secondaryAction)
        {
            Listen();
            icon.sprite = iconSprite; icon.enabled = iconSprite != null;
            if (glow != null) glow.color = new Color(accent.r, accent.g, accent.b, glow.color.a);
            title.text = heading; title.color = accent;
            message.text = body;
            amountGroup.SetActive(!string.IsNullOrEmpty(amountText));
            if (amountText != null) amount.text = amountText;
            primaryLabel.text = primaryText; onPrimary = primaryAction;
            secondaryButton.gameObject.SetActive(!string.IsNullOrEmpty(secondaryText));
            if (secondaryText != null) secondaryLabel.text = secondaryText;
            onSecondary = secondaryAction;
            root.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Pop());
        }

        private void Close(Action action)
        {
            root.SetActive(false);
            onPrimary = onSecondary = null;
            action?.Invoke();
        }

        private IEnumerator Pop()
        {
            for (float t = 0f; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.22f;
                float s = 1f + 0.12f * Mathf.Sin(k * Mathf.PI) - 0.25f * (1f - k) * (1f - k);
                panel.localScale = Vector3.one * s;
                yield return null;
            }
            panel.localScale = Vector3.one;
        }
    }
}
