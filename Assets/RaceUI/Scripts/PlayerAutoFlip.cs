using ALIyerEdon;
using UnityEngine;

namespace RaceUI
{
    /// <summary>
    /// Puts the player's car back on its wheels when it lies on its roof / side and cannot get up by itself:
    /// tilted past the limit and almost not moving for a short time. The car is lifted a little, set upright
    /// facing its next checkpoint and released. Lives in the shared race UI prefab (scene systems via the links).
    /// </summary>
    public sealed class PlayerAutoFlip : MonoBehaviour
    {
        [SerializeField] private RaceSceneLinks links;
        [Tooltip("Car counts as flipped when its up axis is more than this many degrees from vertical")]
        [SerializeField] private float maxTilt = 70f;
        [Tooltip("...and it moves slower than this (km/h)")]
        [SerializeField] private float maxSpeed = 8f;
        [Tooltip("...for this long (seconds) before it is put back")]
        [SerializeField] private float delay = 2f;
        [SerializeField] private float lift = 1.2f;
        [Tooltip("Optional message shown briefly when the car is reset")]
        [SerializeField] private GameObject notice;
        [SerializeField] private float noticeTime = 1.2f;

        private float flippedFor;
        private float noticeUntil;

        private void Update()
        {
            if (notice != null && notice.activeSelf && Time.unscaledTime > noticeUntil) notice.SetActive(false);

            Race_Manager rm = links != null ? links.RaceManager : null;
            // racing = the race HUD is up (Race_Manager.raceStarted is never set by the original scripts)
            if (rm == null || rm.raceUI == null || !rm.raceUI.activeInHierarchy) { flippedFor = 0f; return; }
            EasyCarController car = rm.PlayerCar;
            if (car == null) { flippedFor = 0f; return; }

            Rigidbody body = car.GetComponent<Rigidbody>();
            float tilt = Vector3.Angle(car.transform.up, Vector3.up);
            float kmh = body != null ? body.linearVelocity.magnitude * 3.6f : 0f;
            if (tilt > maxTilt && kmh < maxSpeed) flippedFor += Time.deltaTime;
            else flippedFor = 0f;

            if (flippedFor >= delay)
            {
                flippedFor = 0f;
                Reset(car, body, rm.PlayerPosition);
            }
        }

        private void Reset(EasyCarController car, Rigidbody body, Car_Position position)
        {
            Transform t = car.transform;
            // keep the heading if the car is on its roof/side; on its nose or tail, face the next checkpoint
            Vector3 forward = Vector3.ProjectOnPlane(t.forward, Vector3.up);
            if (position != null && position.nextCheckpoint != null)
            {
                Vector3 toNext = Vector3.ProjectOnPlane(position.nextCheckpoint.position - t.position, Vector3.up);
                if (forward.sqrMagnitude < 0.09f || Vector3.Dot(forward.normalized, toNext.normalized) < 0f) forward = toNext;
            }
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;

            Vector3 pos = t.position;
            if (Physics.Raycast(pos + Vector3.up * 6f, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore)
                && hit.transform.root != t.root)
                pos.y = hit.point.y;
            pos += Vector3.up * lift;

            Quaternion rot = Quaternion.LookRotation(forward.normalized, Vector3.up);
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = pos;
                body.rotation = rot;
            }
            t.SetPositionAndRotation(pos, rot);

            if (notice != null) { notice.SetActive(true); noticeUntil = Time.unscaledTime + noticeTime; }
        }
    }
}
