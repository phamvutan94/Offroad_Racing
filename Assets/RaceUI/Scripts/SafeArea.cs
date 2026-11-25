using UnityEngine;

namespace RaceUI
{
    /// <summary>Fits this RectTransform to Screen.safeArea. Children anchor to its corners/edges.</summary>
    [ExecuteAlways, RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;

        private Rect lastSafeArea;
        private Vector2Int lastScreen;

        private void Reset() => rectTransform = (RectTransform)transform;
        private void OnEnable() => Fit();

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height) Fit();
        }

        private void Fit()
        {
            if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0) return;
            lastSafeArea = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            Vector2 size = new Vector2(Screen.width, Screen.height);
            rectTransform.anchorMin = lastSafeArea.min / size;
            rectTransform.anchorMax = lastSafeArea.max / size;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
