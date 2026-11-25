using System;
using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>Modal confirm / info popup. Lives on an always-active overlay; the panel root is toggled.</summary>
    public sealed class UIDialog : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text title;
        [SerializeField] private Text message;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text confirmLabel;
        [SerializeField] private Button cancelButton;

        private Action onConfirm;
        private bool listening;

        // The panel is saved inactive, so Awake only runs on first activation: never hide here.
        private void Awake() => Listen();

        private void Listen()
        {
            if (listening) return;
            listening = true;
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Close);
        }

        public void Show(string heading, string body, string confirmText, Action confirmAction, bool showCancel = true)
        {
            Listen();
            title.text = heading;
            message.text = body;
            confirmLabel.text = confirmText;
            onConfirm = confirmAction;
            cancelButton.gameObject.SetActive(showCancel);
            root.SetActive(true);
        }

        public void ShowInfo(string heading, string body) => Show(heading, body, "OK", null, false);

        public void Close()
        {
            onConfirm = null;
            root.SetActive(false);
        }

        private void Confirm()
        {
            Action action = onConfirm;
            Close();
            action?.Invoke();
        }
    }
}
