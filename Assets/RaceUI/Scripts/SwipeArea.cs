using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// Transparent full-screen touch/mouse area placed BELOW the buttons of a screen.
    /// One finger drags (look/orbit), two fingers pinch. Buttons on top keep their own touches.
    /// Deltas are normalised by screen height so sensitivity is the same on every device.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class SwipeArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private readonly Dictionary<int, Vector2> pointers = new Dictionary<int, Vector2>();
        private Vector2 dragDelta;
        private float pinchDelta;

        public bool IsDragging => pointers.Count > 0;
        public float LastInputTime { get; private set; } = -999f;

        /// Accumulated one-finger drag since last call, in screen heights (1 = full screen height).
        public Vector2 ConsumeDrag() { Vector2 d = dragDelta; dragDelta = Vector2.zero; return d; }

        /// Accumulated pinch since last call, in screen heights (positive = fingers apart).
        public float ConsumePinch() { float p = pinchDelta; pinchDelta = 0f; return p; }

        public void OnPointerDown(PointerEventData e)
        {
            pointers[e.pointerId] = e.position;
            LastInputTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData e) => pointers.Remove(e.pointerId);

        public void OnDrag(PointerEventData e)
        {
            if (!pointers.ContainsKey(e.pointerId)) return;
            float scale = 1f / Mathf.Max(1, Screen.height);
            if (pointers.Count == 2)
            {
                Vector2 other = Vector2.zero;
                foreach (var kv in pointers) if (kv.Key != e.pointerId) other = kv.Value;
                float before = Vector2.Distance(pointers[e.pointerId], other);
                float after = Vector2.Distance(e.position, other);
                pinchDelta += (after - before) * scale;
            }
            else if (pointers.Count == 1)
            {
                dragDelta += e.delta * scale;
            }
            pointers[e.pointerId] = e.position;
            LastInputTime = Time.unscaledTime;
        }

        private void OnDisable()
        {
            pointers.Clear();
            dragDelta = Vector2.zero;
            pinchDelta = 0f;
        }
    }
}
