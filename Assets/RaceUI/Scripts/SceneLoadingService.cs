using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// One persistent loading screen (lives in the Boot scene, DontDestroyOnLoad) used for every scene change.
    /// Call SceneLoadingService.Instance.Load(...). All view references are assigned in the Inspector.
    /// </summary>
    public sealed class SceneLoadingService : MonoBehaviour
    {
        public static SceneLoadingService Instance { get; private set; }

        [Header("View")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private StatBar progressBar;
        [SerializeField] private GameObject thumbnailFrame;
        [SerializeField] private Image thumbnail;
        [SerializeField] private Text caption;
        [Tooltip("Track title logo shown over the caption bar (hidden when the track has none)")]
        [SerializeField] private Image titleArt;
        [SerializeField] private Text tip;
        [SerializeField, TextArea] private string[] tips =
        {
            "Brake before the turn.\nCarry your speed through the exit.",
            "Save your nitro for the long straights.",
            "Finish on the podium to earn coins and unlock new tracks.",
            "Swipe the screen to look around your car.",
        };

        [Header("Timing")]
        [SerializeField, Min(0f)] private float fadeTime = 0.25f;
        [Tooltip("The loading screen stays at least this long; the bar fills smoothly over this time.")]
        [SerializeField, Min(0f)] private float minimumShowTime = 3f;
        [SerializeField, Min(0.01f)] private float progressSmoothTime = 0.12f;

        public bool IsLoading { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetVisible(0f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <param name="preview">Track thumbnail; null hides the thumbnail frame.</param>
        /// <param name="title">Track title logo; null shows the plain caption text instead.</param>
        public void Load(string sceneName, Sprite preview = null, string label = null, Sprite title = null)
        {
            if (IsLoading || string.IsNullOrEmpty(sceneName)) return;
            StartCoroutine(LoadRoutine(sceneName, preview, label, title));
        }

        private IEnumerator LoadRoutine(string sceneName, Sprite preview, string label, Sprite title)
        {
            IsLoading = true;
            thumbnailFrame.SetActive(preview != null);
            if (preview != null) thumbnail.sprite = preview;
            caption.text = label ?? string.Empty;
            bool hasTitle = title != null && preview != null;
            titleArt.gameObject.SetActive(hasTitle);
            if (hasTitle) titleArt.sprite = title;
            caption.gameObject.SetActive(!hasTitle);
            if (tips.Length > 0) tip.text = tips[Random.Range(0, tips.Length)];
            progressBar.SetValue(0f);

            group.blocksRaycasts = true;
            yield return Fade(0f, 1f);
            float shownAt = Time.unscaledTime;

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                // Scene missing from the build profile: hide the screen again instead of staying stuck in IsLoading.
                Debug.LogError("[SceneLoadingService] Cannot load scene '" + sceneName + "' (not in the build profile?)");
                yield return Fade(1f, 0f);
                group.blocksRaycasts = false;
                IsLoading = false;
                yield break;
            }
            op.allowSceneActivation = false;
            // Fake progress: eases towards 95% over minimumShowTime, never ahead of the real load,
            // then glides to 100% once the scene is ready and the minimum time has passed.
            float shown = 0f, velocity = 0f;
            while (true)
            {
                float elapsed = Time.unscaledTime - shownAt;
                float real = Mathf.Clamp01(op.progress / 0.9f);
                bool ready = op.progress >= 0.9f && elapsed >= minimumShowTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, minimumShowTime));
                float fake = 1f - (1f - t) * (1f - t); // ease-out
                float target = ready ? 1f : 0.95f * Mathf.Min(fake, real);
                shown = Mathf.SmoothDamp(shown, target, ref velocity, progressSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                progressBar.SetValue(shown);
                if (ready && shown >= 0.995f) break;
                yield return null;
            }
            progressBar.SetValue(1f);
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
            yield return null; // let the new scene run its first frame behind the loading screen

            yield return Fade(1f, 0f);
            group.blocksRaycasts = false;
            IsLoading = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                SetVisible(Mathf.Lerp(from, to, t / fadeTime));
                yield return null;
            }
            SetVisible(to);
        }

        private void SetVisible(float alpha)
        {
            group.alpha = alpha;
            group.interactable = false;
            group.blocksRaycasts = alpha > 0f;
        }
    }
}
