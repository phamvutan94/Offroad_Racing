using System.Collections.Generic;
using ALIyerEdon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RaceUI.EditorTools
{
    /// Temporary editor tool: tall invisible guard walls along both sides of every race track, plus the track data
    /// for PlayerTrackGuard (RaceSceneLinks.trackLanes / trackLimit). Delete when done; the scenes keep the result.
    public static class TrackWallBuilder
    {
        const float Step = 6f, Height = 40f, Thick = 0.8f, Below = 3f;

        public static string Build(string scenePath, float defaultOffset, float maxOffset, string roadMeshPath = null, float extraLimit = 0f)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            var old = GameObject.Find("TrackGuardWalls"); if (old != null) Object.DestroyImmediate(old);
            Physics.SyncTransforms();

            var lanes = new[] { GameObject.Find("Waypoints/Path_2").GetComponent<Waypoint_System>(), GameObject.Find("Waypoints/Path_3").GetComponent<Waypoint_System>() };
            var line = string.IsNullOrEmpty(roadMeshPath) ? Centre(lanes) : MeshCentre(roadMeshPath);
            int n = line.Count;
            var dist = new float[n]; for (int i = 1; i < n; i++) dist[i] = dist[i - 1] + Vector3.Distance(line[i - 1], line[i]);
            float total = dist[n - 1] + Vector3.Distance(line[n - 1], line[0]);
            int obstacle = LayerMask.NameToLayer("Obstacle");

            // barrier distance per sample and side
            var off = new float[2, n];
            var ground = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 f = line[(i + 1) % n] - line[i]; f.y = 0; f.Normalize();
                Vector3 r = Vector3.Cross(Vector3.up, f);
                ground[i] = GroundY(line[i]);
                for (int s = 0; s < 2; s++)
                {
                    Vector3 dir = s == 0 ? -r : r;
                    float best = float.MaxValue;
                    foreach (float h in new[] { 0.6f, 1.5f })
                        foreach (var hit in Physics.RaycastAll(new Vector3(line[i].x, ground[i] + h, line[i].z), dir, maxOffset, ~0, QueryTriggerInteraction.Ignore))
                        {
                            if (hit.collider.CompareTag("Ground") || hit.collider is TerrainCollider) continue;
                            if (hit.collider.attachedRigidbody != null && !hit.collider.attachedRigidbody.isKinematic) continue;
                            if (hit.distance < 4f) continue; // something on the road itself, not a barrier
                            best = Mathf.Min(best, hit.distance);
                        }
                    off[s, i] = best < float.MaxValue ? Mathf.Min(best + 0.8f, maxOffset) : defaultOffset;
                }
            }
            // smooth (moving max over +-3 samples so the wall never cuts inside a barrier gap)
            var sm = new float[2, n];
            for (int s = 0; s < 2; s++)
                for (int i = 0; i < n; i++)
                {
                    float m = 0; for (int k = -3; k <= 3; k++) m = Mathf.Max(m, off[s, ((i + k) % n + n) % n]);
                    sm[s, i] = m;
                }

            var root = new GameObject("TrackGuardWalls");
            SceneManager_Move(root, scene);
            int made = 0, skipped = 0;
            int stride = Mathf.Max(1, Mathf.RoundToInt(Step / 2f)); // centreline is ~2 m
            for (int i = 0; i < n; i += stride)
            {
                int j = (i + stride) % n;
                for (int s = 0; s < 2; s++)
                {
                    Vector3 a = Edge(line, i, s, sm[s, i]), b = Edge(line, j, s, sm[s, j]);
                    Vector3 mid = (a + b) * 0.5f;
                    if (NearOtherRoad(line, dist, total, i, mid, Mathf.Max(sm[s, i], sm[s, j]) + 6f)) { skipped++; continue; }
                    float gy = Mathf.Min(ground[i], ground[j]);
                    var go = new GameObject("Wall");
                    go.layer = obstacle;
                    go.transform.SetParent(root.transform, false);
                    Vector3 along = b - a; along.y = 0;
                    go.transform.position = new Vector3(mid.x, gy - Below + Height * 0.5f, mid.z);
                    go.transform.rotation = Quaternion.LookRotation(along.normalized, Vector3.up);
                    var bc = go.AddComponent<BoxCollider>();
                    bc.size = new Vector3(Thick, Height, along.magnitude + 0.6f);
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                    made++;
                }
            }

            // data for PlayerTrackGuard
            float maxOff = 0; for (int s = 0; s < 2; s++) for (int i = 0; i < n; i++) maxOff = Mathf.Max(maxOff, sm[s, i]);
            var links = Object.FindFirstObjectByType<RaceSceneLinks>(FindObjectsInactive.Include);
            var so = new SerializedObject(links);
            var tl = so.FindProperty("trackLanes"); tl.arraySize = 2;
            tl.GetArrayElementAtIndex(0).objectReferenceValue = lanes[0]; tl.GetArrayElementAtIndex(1).objectReferenceValue = lanes[1];
            so.FindProperty("trackLimit").floatValue = Mathf.Ceil(maxOff + 6f + extraLimit);
            so.ApplyModifiedProperties();
            if (PrefabUtility.IsPartOfPrefabInstance(links)) PrefabUtility.RecordPrefabInstancePropertyModifications(links);

            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return $"{System.IO.Path.GetFileNameWithoutExtension(scenePath)}: walls={made} skipped={skipped} trackLimit={Mathf.Ceil(maxOff + 6f)} len={total:0}";
        }

        static void SceneManager_Move(GameObject go, UnityEngine.SceneManagement.Scene scene) => UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);

        static Vector3 Edge(List<Vector3> line, int i, int side, float offset)
        {
            int n = line.Count;
            Vector3 f = line[(i + 1) % n] - line[(i - 1 + n) % n]; f.y = 0; f.Normalize();
            Vector3 r = Vector3.Cross(Vector3.up, f);
            return line[i] + (side == 0 ? -r : r) * offset;
        }

        /// A different part of the circuit (not the neighbouring stretch) passes near this wall piece: skip it.
        static bool NearOtherRoad(List<Vector3> line, float[] dist, float total, int i, Vector3 p, float radius)
        {
            int n = line.Count; float r2 = radius * radius;
            for (int k = 0; k < n; k += 2)
            {
                float d = Mathf.Abs(dist[k] - dist[i]); d = Mathf.Min(d, total - d);
                if (d < 80f) continue;
                Vector3 q = line[k] - p; q.y = 0;
                if (q.sqrMagnitude < r2) return true;
            }
            return false;
        }

        static float GroundY(Vector3 p)
        {
            float best = float.NaN;
            foreach (var h in Physics.RaycastAll(p + Vector3.up * 20f, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore))
                if ((h.collider.CompareTag("Ground") || h.collider is TerrainCollider) && (float.IsNaN(best) || Mathf.Abs(h.point.y - p.y) < Mathf.Abs(best - p.y))) best = h.point.y;
            return float.IsNaN(best) ? p.y - 1f : best;
        }

        static List<Vector3> MeshCentre(string path)
        {
            var mf = GameObject.Find(path).GetComponent<MeshFilter>();
            var v = mf.sharedMesh.vertices; var uv = mf.sharedMesh.uv;
            var rows = new SortedDictionary<int, Vector3[]>();
            for (int i = 0; i < v.Length; i++)
            {
                int key = Mathf.RoundToInt(uv[i].y * 1000f);
                if (!rows.TryGetValue(key, out var r)) rows[key] = r = new Vector3[2];
                if (uv[i].x < 0.5f) r[0] = mf.transform.TransformPoint(v[i]); else r[1] = mf.transform.TransformPoint(v[i]);
            }
            var res = new List<Vector3>(); Vector3 last = Vector3.positiveInfinity;
            foreach (var r in rows.Values)
            {
                if (r[0] == Vector3.zero || r[1] == Vector3.zero) continue;
                Vector3 c = (r[0] + r[1]) * 0.5f;
                if (res.Count == 0 || (c - last).sqrMagnitude >= 4f) { res.Add(c); last = c; }
            }
            return res;
        }

        static List<Vector3> Centre(Waypoint_System[] lanes)
        {
            int count = Mathf.Min(lanes[0].waypoints.Count, lanes[1].waypoints.Count);
            var ctrl = new List<Vector3>();
            for (int i = 0; i < count; i++) ctrl.Add((lanes[0].waypoints[i].position + lanes[1].waypoints[i].position) * 0.5f);
            var res = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                Vector3 p0 = ctrl[(i - 1 + count) % count], p1 = ctrl[i], p2 = ctrl[(i + 1) % count], p3 = ctrl[(i + 2) % count];
                int k = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(p1, p2) / 2f));
                for (int j = 0; j < k; j++)
                {
                    float t = j / (float)k;
                    res.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t));
                }
            }
            return res;
        }
    }
}
