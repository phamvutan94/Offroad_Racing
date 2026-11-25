using UnityEngine;

namespace RaceUI
{
    /// <summary>
    /// Garage showroom camera: swipe to orbit around the car, pinch to zoom, inertia after release
    /// and a slow auto-orbit when idle. The start pose is taken from where the camera is placed in the scene.
    /// </summary>
    public sealed class GarageOrbitCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.2f, 0f);

        [Header("Input")]
        [SerializeField] private SwipeArea[] swipeAreas;
        [SerializeField] private float yawPerScreen = 240f;   // degrees for a full-screen-height swipe
        [SerializeField] private float pitchPerScreen = 120f;
        [SerializeField] private float zoomPerScreen = 6f;    // metres for a full-screen-height pinch

        [Header("Limits")]
        [SerializeField] private float minPitch = 2f;
        [SerializeField] private float maxPitch = 35f;
        [SerializeField] private float minDistance = 3f;
        [SerializeField] private float maxDistance = 6f;

        [Header("Feel")]
        [SerializeField, Min(0.01f)] private float inertiaDamping = 4f;
        [SerializeField] private float idleDelay = 5f;
        [SerializeField] private float idleOrbitSpeed = 8f;   // degrees per second

        private float yaw, pitch, distance;
        private float yawVelocity, pitchVelocity;

        private Vector3 Pivot => target.position + targetOffset;

        private void Start()
        {
            Vector3 offset = transform.position - Pivot;
            distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
            pitch = Mathf.Clamp(Mathf.Asin(offset.y / Mathf.Max(0.001f, offset.magnitude)) * Mathf.Rad2Deg, minPitch, maxPitch);
            yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            Apply();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            Vector2 drag = Vector2.zero; float pinch = 0f; bool dragging = false; float lastInput = -999f;
            foreach (SwipeArea area in swipeAreas)
            {
                if (area == null || !area.isActiveAndEnabled) continue;
                drag += area.ConsumeDrag();
                pinch += area.ConsumePinch();
                dragging |= area.IsDragging;
                lastInput = Mathf.Max(lastInput, area.LastInputTime);
            }

            if (dragging)
            {
                yawVelocity = drag.x * yawPerScreen / dt;
                pitchVelocity = -drag.y * pitchPerScreen / dt;
            }
            else
            {
                float k = Mathf.Exp(-inertiaDamping * dt);
                yawVelocity *= k;
                pitchVelocity *= k;
                if (Time.unscaledTime - lastInput > idleDelay) yaw += idleOrbitSpeed * dt;
            }

            yaw += yawVelocity * dt;
            pitch = Mathf.Clamp(pitch + pitchVelocity * dt, minPitch, maxPitch);
            distance = Mathf.Clamp(distance - pinch * zoomPerScreen, minDistance, maxDistance);
            Apply();
        }

        private void Apply()
        {
            Vector3 pivot = Pivot;
            transform.position = pivot + Quaternion.Euler(pitch, yaw, 0f) * Vector3.back * distance;
            transform.LookAt(pivot);
        }
    }
}
