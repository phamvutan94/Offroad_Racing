using System.Collections.Generic;
using ALIyerEdon;
using UnityEngine;

namespace RaceUI
{
    /// <summary>
    /// Safety net that keeps the player's car on the circuit: when the car is outside the track limit (or far below
    /// the road) for a moment, it is put back on the centreline at the nearest point, facing the race direction.
    /// The invisible guard walls along the track stop most cars; this catches the rest (e.g. huge jumps at 500 km/h).
    /// Lives in the shared race UI prefab; the scene data comes from RaceSceneLinks.
    /// </summary>
    public sealed class PlayerTrackGuard : MonoBehaviour
    {
        [SerializeField] private RaceSceneLinks links;
        [Tooltip("Seconds outside the limit before the car is put back")]
        [SerializeField] private float delay = 0.75f;
        [Tooltip("Car this many metres below the road counts as off the track (fell off a cliff / through the map)")]
        [SerializeField] private float maxDrop = 12f;
        [Tooltip("Speed kept after the reset, as a fraction (capped)")]
        [SerializeField] private float keepSpeed = 0.35f;
        [SerializeField] private float maxResetSpeed = 25f;
        [SerializeField] private float lift = 1.2f;
        [Tooltip("Optional message shown briefly when the car is put back")]
        [SerializeField] private GameObject notice;
        [SerializeField] private float noticeTime = 1.2f;

        private readonly List<Vector3> line = new List<Vector3>();
        private int nearest;
        private float outFor, noticeUntil, fullSearchAt;

        private void Start() => BuildCentreline();

        private void BuildCentreline()
        {
            line.Clear();
            if (links == null || links.TrackLanes == null || links.TrackLanes.Length == 0) return;
            var lanes = links.TrackLanes;
            int count = int.MaxValue;
            foreach (var l in lanes) if (l != null) count = Mathf.Min(count, l.waypoints.Count);
            if (count == int.MaxValue || count < 3) return;
            var ctrl = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                Vector3 sum = Vector3.zero; int n = 0;
                foreach (var l in lanes) if (l != null && l.waypoints[i] != null) { sum += l.waypoints[i].position; n++; }
                ctrl.Add(sum / Mathf.Max(1, n));
            }
            for (int i = 0; i < count; i++)
            {
                Vector3 p0 = ctrl[(i - 1 + count) % count], p1 = ctrl[i], p2 = ctrl[(i + 1) % count], p3 = ctrl[(i + 2) % count];
                int k = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(p1, p2) / 4f));
                for (int j = 0; j < k; j++)
                {
                    float t = j / (float)k;
                    line.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t));
                }
            }
        }

        private void Update()
        {
            if (notice != null && notice.activeSelf && Time.unscaledTime > noticeUntil) notice.SetActive(false);
            Race_Manager rm = links != null ? links.RaceManager : null;
            if (line.Count == 0 || rm == null || rm.raceUI == null || !rm.raceUI.activeInHierarchy) { outFor = 0f; return; }
            EasyCarController car = rm.PlayerCar;
            if (car == null) { outFor = 0f; return; }

            Vector3 p = car.transform.position;
            FindNearest(p);
            Vector3 c = line[nearest];
            float horizontal = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z));
            bool off = horizontal > links.TrackLimit || c.y - p.y > maxDrop;
            outFor = off ? outFor + Time.deltaTime : 0f;
            if (outFor >= delay) { outFor = 0f; PutBack(car); }
        }

        /// Nearest centreline sample; a local search around the last one, a full search now and then.
        private void FindNearest(Vector3 p)
        {
            int n = line.Count;
            bool full = Time.time >= fullSearchAt;
            int from = full ? 0 : nearest - 60, to = full ? n : nearest + 60;
            if (full) fullSearchAt = Time.time + 1.5f;
            float best = float.MaxValue; int bestI = nearest;
            for (int i = from; i < to; i++)
            {
                int k = ((i % n) + n) % n;
                Vector3 d = line[k] - p; d.y *= 0.5f; // prefer the road at the car's level (bridges, tunnels)
                float sq = d.sqrMagnitude;
                if (sq < best) { best = sq; bestI = k; }
            }
            nearest = bestI;
        }

        private void PutBack(EasyCarController car)
        {
            int n = line.Count;
            Vector3 c = line[nearest], next = line[(nearest + 3) % n];
            Vector3 forward = Vector3.ProjectOnPlane(next - c, Vector3.up).normalized;
            if (forward.sqrMagnitude < 1e-4f) forward = car.transform.forward;
            Vector3 pos = c;
            foreach (var hit in Physics.RaycastAll(c + Vector3.up * 8f, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.transform.root != car.transform.root && (pos == c || hit.point.y > pos.y)) pos = hit.point;
            pos += Vector3.up * lift;

            Rigidbody body = car.GetComponent<Rigidbody>();
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
            float speed = body != null ? Mathf.Min(body.linearVelocity.magnitude * keepSpeed, maxResetSpeed) : 0f;
            if (body != null)
            {
                body.position = pos; body.rotation = rot;
                body.linearVelocity = forward * speed; body.angularVelocity = Vector3.zero;
            }
            car.transform.SetPositionAndRotation(pos, rot);
            if (notice != null) { notice.SetActive(true); noticeUntil = Time.unscaledTime + noticeTime; }
        }
    }
}
