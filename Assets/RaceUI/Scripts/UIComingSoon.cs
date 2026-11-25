using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>Toast for features not in the game yet. Buttons call Show() via OnClick.</summary>
    public sealed class UIComingSoon : MonoBehaviour
    {
        [SerializeField] private CanvasGroup toast;
        [SerializeField] private Text message;
        [SerializeField] private string defaultMessage = "COMING SOON";
        [SerializeField, Min(0f)] private float holdSeconds = 1.4f;

        private Coroutine routine;

        public void Show() => Show(defaultMessage);

        public void Show(string text)
        {
            if (message != null) message.text = text;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Display());
        }

        private IEnumerator Display()
        {
            toast.gameObject.SetActive(true);
            toast.alpha = 1f;
            yield return new WaitForSecondsRealtime(holdSeconds);
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                toast.alpha = 1f - t / 0.25f;
                yield return null;
            }
            toast.gameObject.SetActive(false);
            routine = null;
        }
    }
}
