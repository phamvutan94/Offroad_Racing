using UnityEngine;
using UnityEngine.EventSystems;

namespace RaceUI
{
    /// <summary>
    /// Put on a button that sits over a SwipeArea (garage hotspots): a tap still clicks the button,
    /// but once the finger moves past the drag threshold the gesture is handed to the swipe area
    /// (camera orbit) and the click is cancelled.
    /// </summary>
    public sealed class SwipePassThrough : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [SerializeField] private SwipeArea swipeArea;

        public void OnPointerDown(PointerEventData e)
        {
            if (swipeArea != null) swipeArea.OnPointerDown(e);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (swipeArea != null) swipeArea.OnPointerUp(e);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            e.eligibleForClick = false; // it is a swipe now, not a tap
            if (swipeArea != null) swipeArea.OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (swipeArea != null) swipeArea.OnDrag(e);
        }

        public void OnEndDrag(PointerEventData e) { }

        public void OnScroll(PointerEventData e)
        {
            if (swipeArea != null) swipeArea.OnScroll(e);
        }
    }
}
