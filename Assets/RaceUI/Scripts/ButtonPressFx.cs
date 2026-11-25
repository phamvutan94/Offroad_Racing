using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RaceUI
{
    /// <summary>Scale punch on press. Target defaults to this RectTransform.</summary>
    public sealed class ButtonPressFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.95f;
        [SerializeField, Min(0.01f)] private float duration = 0.08f;

        private Coroutine routine;

        private void Reset() => target = (RectTransform)transform;

        public void OnPointerDown(PointerEventData eventData) => Animate(pressedScale);
        public void OnPointerUp(PointerEventData eventData) => Animate(1f);
        public void OnPointerExit(PointerEventData eventData) => Animate(1f);

        private void Animate(float scale)
        {
            if (target == null || !isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Tween(scale));
        }

        private IEnumerator Tween(float scale)
        {
            Vector3 from = target.localScale, to = Vector3.one * scale;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.Lerp(from, to, t / duration);
                yield return null;
            }
            target.localScale = to;
            routine = null;
        }

        private void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (target != null) target.localScale = Vector3.one;
        }
    }
}
