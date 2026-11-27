using UnityEngine;

namespace RaceUI
{
    /// <summary>
    /// Per-car chase camera framing. SmoothFollow2 (the race camera) uses these values instead of the scene's
    /// when the player's car has this component, so tall cars get a higher, longer camera that keeps the road visible.
    /// </summary>
    public sealed class CarCameraFraming : MonoBehaviour
    {
        [SerializeField] private float distance = 6.2f;
        [SerializeField] private float height = 1.75f;
        [SerializeField] private float angle = 8f;

        public float Distance => distance;
        public float Height => height;
        public float Angle => angle;
    }
}
