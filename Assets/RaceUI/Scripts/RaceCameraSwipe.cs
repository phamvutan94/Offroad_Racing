using ALIyerEdon;
using UnityEngine;

namespace RaceUI
{
    /// <summary>
    /// "Look around" while driving: swiping adds a yaw/pitch offset to the chase camera (SmoothFollow2).
    /// After the finger is released the camera springs back behind the car. Only active on the main camera.
    /// </summary>
    public sealed class RaceCameraSwipe : MonoBehaviour
    {
        [SerializeField] private SmoothFollow2 follow;
        [SerializeField] private GameObject mainCamera;   // CameraSwitch deactivates it when another view is selected
        [SerializeField] private SwipeArea swipeArea;

        [SerializeField] private float yawPerScreen = 300f;
        [SerializeField] private float pitchPerScreen = 90f;
        [SerializeField] private float maxYaw = 180f;
        [SerializeField] private float minPitch = -10f;
        [SerializeField] private float maxPitch = 25f;
        [SerializeField] private float returnDelay = 0.8f;
        [SerializeField] private float returnSpeed = 4f;

        private float yaw, pitch;

        private void Update()
        {
            bool usable = swipeArea != null && swipeArea.isActiveAndEnabled && mainCamera.activeInHierarchy;
            if (usable)
            {
                Vector2 drag = swipeArea.ConsumeDrag();
                yaw = Mathf.Clamp(yaw + drag.x * yawPerScreen, -maxYaw, maxYaw);
                pitch = Mathf.Clamp(pitch - drag.y * pitchPerScreen, minPitch, maxPitch);
            }

            bool holding = usable && (swipeArea.IsDragging || Time.unscaledTime - swipeArea.LastInputTime < returnDelay);
            if (!holding)
            {
                float k = 1f - Mathf.Exp(-returnSpeed * Time.unscaledDeltaTime);
                yaw = Mathf.Lerp(yaw, 0f, k);
                pitch = Mathf.Lerp(pitch, 0f, k);
            }

            follow.yawOffset = yaw;
            follow.pitchOffset = pitch;
        }
    }
}
