using System.Collections;
using UnityEngine;

namespace RaceUI
{
    /// <summary>Root of one UI screen. Shown/hidden by UIScreenManager.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform content;
        [SerializeField, Min(0f)] private float fadeDuration = 0.18f;

        private Coroutine transition;

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            content = (RectTransform)transform;
        }

        public void Show(bool animate)
        {
            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            if (transition != null) StopCoroutine(transition);
            if (animate && fadeDuration > 0f && isActiveAndEnabled) transition = StartCoroutine(FadeIn());
            else SetVisible();
        }

        public void Hide()
        {
            if (transition != null) StopCoroutine(transition);
            transition = null;
            SetVisible();
            gameObject.SetActive(false);
        }

        private IEnumerator FadeIn()
        {
            canvasGroup.interactable = false;
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / fadeDuration);
                canvasGroup.alpha = k;
                if (content != null) content.localScale = Vector3.one * Mathf.Lerp(0.975f, 1f, k);
                yield return null;
            }
            SetVisible();
            transition = null;
        }

        private void SetVisible()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            if (content != null) content.localScale = Vector3.one;
        }
    }
}
