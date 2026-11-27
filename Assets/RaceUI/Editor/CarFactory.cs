using System.Text;
using ALIyerEdon;
using UnityEditor;
using UnityEngine;

namespace RaceUI.EditorTools
{
    /// Temporary editor tool: builds player car prefabs from a model and sets real-world based physics.
    /// Delete when done; the prefabs are the source of truth.
    public static class CarFactory
    {
        public const string Template = "Assets/Snow_Racer/Prefabs/Cars/Players/Player_5.prefab";
        public const float SpeedScale = 500f / (260f * 1.315f); // strongest car (260 km/h real) fully tuned = 500 km/h

        public class Spec
        {
            public float mass, t0100, topReal, comH, travel, freq, wheelMass, steer, rearShare = 0.5f;
            public bool rwd;
        }

        public class Look
        {
            public float realLength;      // metres
            public float tailY, tailX;    // tail lights: height above ground, inset from the side (m)
            public float exhaustY, exhaustX; // exhaust: height above ground, half spacing (m); exhaustX 0 = centre
            public float rearInset = 0.05f;   // tail/exhaust in front of the rearmost point (m)
        }

        // ------------------------------------------------------------------ build
        public static string BuildCar(int number, string fbxPath, Look look)
        {
            var sb = new StringBuilder();
            string dst = "Assets/Snow_Racer/Prefabs/Cars/Players/Player_" + number + ".prefab";
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            // measure the body length at scale 1 and set the importer scale so the car has its real length
            imp.globalScale = 1f; imp.SaveAndReimport();
            var probe = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath));
            float len = 0; foreach (var r in probe.GetComponentsInChildren<Renderer>()) len = Mathf.Max(len, r.bounds.size.z);
            Object.DestroyImmediate(probe);
            imp.globalScale = look.realLength / len; imp.SaveAndReimport();
            sb.Append($"scale={imp.globalScale:0.0000} ");

            if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) == null) AssetDatabase.CopyAsset(Template, dst);
            var root = PrefabUtility.LoadPrefabContents(dst); root.name = "Player_" + number; var R = root.transform;
            var bodyT = R.Find("Body");
            for (int c = bodyT.childCount - 1; c >= 0; c--)
            {
                var ch = bodyT.GetChild(c);
                if (ch.name == "Effects" || ch.name == "Lights" || ch.name == "Local_Positions" || ch.name == "SteeringWheel") continue;
                Object.DestroyImmediate(ch.gameObject); // previous car model
            }
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath));
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.transform.SetParent(R, false); model.transform.localRotation = Quaternion.identity; model.transform.localPosition = Vector3.zero;
            // wheels: put the wheel centres at the template's axle height (-0.24 below the root)
            var fl = model.transform.Find("FL");
            float wheelR = fl.GetComponent<Renderer>().bounds.extents.y;
            model.transform.localPosition = new Vector3(0, -0.24f - R.InverseTransformPoint(fl.position).y, 0);
            var mBody = model.transform.Find("Body"); mBody.name = "Car_Body"; mBody.SetParent(bodyT, true);

            string[] entries = { "FR", "FL", "RR", "RL" }; string[] wheels = { "FR", "FL", "BR", "BL" };
            var wtParent = R.Find("Wheel_Transforms"); var wcParent = R.Find("Wheel_Colliders");
            for (int i = 0; i < 4; i++)
            {
                var entry = wtParent.Find(entries[i]); var mw = model.transform.Find(wheels[i]);
                for (int c = entry.childCount - 1; c >= 0; c--) Object.DestroyImmediate(entry.GetChild(c).gameObject);
                entry.position = mw.position; entry.rotation = R.rotation; entry.localScale = Vector3.one;
                mw.SetParent(entry, true); mw.localPosition = Vector3.zero; mw.localRotation = Quaternion.identity;
                var wc = wcParent.Find(entries[i]).GetComponent<WheelCollider>();
                wc.radius = mw.GetComponent<Renderer>().bounds.extents.y;
                wc.transform.position = mw.position + R.up * (wc.suspensionDistance * wc.suspensionSpring.targetPosition);
            }
            Object.DestroyImmediate(model);

            var br = mBody.GetComponent<Renderer>().bounds;
            Vector3 lmin = R.InverseTransformPoint(br.min), lmax = R.InverseTransformPoint(br.max);
            float groundY = -0.24f - wheelR, height = lmax.y - groundY;
            var bc = root.GetComponent<BoxCollider>();
            float bottom = -0.24f + 0.12f, top = groundY + height * 0.85f;
            bc.center = new Vector3(0, (bottom + top) * 0.5f, (lmin.z + lmax.z) * 0.5f);
            bc.size = new Vector3(Mathf.Max(0.8f, (lmax.x - lmin.x) * 0.92f), Mathf.Max(0.4f, top - bottom), (lmax.z - lmin.z) * 0.97f);

            // tail lights: brake on the outside, reverse a bit inboard
            float rearZ = lmin.z + look.rearInset, halfW = (lmax.x - lmin.x) * 0.5f;
            int bi = 0, ri = 0;
            foreach (Transform l in bodyT.Find("Lights"))
            {
                if (l.name == "BrakeLight") { float x = (bi++ == 0 ? -1 : 1) * (halfW - look.tailX); l.position = R.TransformPoint(new Vector3(x, groundY + look.tailY, rearZ - 0.05f)); }
                else if (l.name == "ReverseLight") { float x = (ri++ == 0 ? -1 : 1) * (halfW - look.tailX - 0.18f); l.position = R.TransformPoint(new Vector3(x, groundY + look.tailY - 0.06f, rearZ - 0.05f)); }
            }
            // exhaust: flames + nitro
            var fx = bodyT.Find("Effects");
            float ex = look.exhaustX, ey = groundY + look.exhaustY, ez = rearZ - 0.12f;
            Place(fx, "Flame_Left", R, new Vector3(-ex, ey, ez)); Place(fx, "Flame_Right", R, new Vector3(ex, ey, ez));
            Place(fx, "Nitro_2", R, new Vector3(-ex, ey, ez - 0.03f)); Place(fx, "Nitro_1", R, new Vector3(ex, ey, ez - 0.03f));
            var sh = fx.Find("Shadow"); sh.position = R.TransformPoint(new Vector3(0, groundY + 3.5f, (lmin.z + lmax.z) * 0.5f));
            foreach (Transform s in fx.Find("Wheel Effects"))
            {
                int idx = System.Array.IndexOf(entries, s.name.Split('_')[0]); if (idx < 0) continue;
                s.position = wtParent.Find(entries[idx]).position - R.up * (wheelR - 0.05f) - R.forward * 0.15f;
            }
            foreach (Transform t in bodyT.Find("Local_Positions")) t.position = R.TransformPoint(new Vector3(0, groundY + height + 0.8f, (lmin.z + lmax.z) * 0.5f));
            // the AI ray origin sits at the front bumper
            var ray = R.Find("RayCast"); if (ray != null) ray.localPosition = new Vector3(0, -0.24f + 0.25f, lmax.z - 0.3f);

            // chase camera for tall cars
            var framing = root.GetComponent<CarCameraFraming>();
            if (height > 1.7f)
            {
                if (framing == null) framing = root.AddComponent<CarCameraFraming>();
                var so = new SerializedObject(framing);
                so.FindProperty("distance").floatValue = 4.2f + (height - 1.4f) * 3f;
                so.FindProperty("height").floatValue = 0.83f + (height - 1.4f) * 1.4f;
                so.FindProperty("angle").floatValue = 7.5f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (framing != null) Object.DestroyImmediate(framing);

            var tso = new SerializedObject(root.GetComponent<CarTuningApplier>()); tso.FindProperty("carIndex").intValue = number - 1; tso.ApplyModifiedPropertiesWithoutUndo();
            bodyT.GetComponent<Suspension_Loader>().carID = number - 1;
            root.GetComponent<Car_Position>().RacerName = "Player " + number;
            root.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            PrefabUtility.SaveAsPrefabAsset(root, dst); PrefabUtility.UnloadPrefabContents(root);
            sb.Append($"len={lmax.z - lmin.z:0.00} w={lmax.x - lmin.x:0.00} h={height:0.00} wheelR={wheelR:0.00} framing={(height > 1.7f)}");
            return sb.ToString();
        }

        static void Place(Transform fx, string n, Transform R, Vector3 local) { var t = fx.Find(n); if (t != null) t.position = R.TransformPoint(local); }

        // ------------------------------------------------------------------ physics
        public static string ApplySpecs(int number, Spec s, float kLoss = 1.3f)
        {
            const float g = 9.81f, comFactor = 0.6f, zeta = 0.4f, eff = 0.75f;
            string path = "Assets/Snow_Racer/Prefabs/Cars/Players/Player_" + number + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path); var R = root.transform;
            var e = root.GetComponent<EasyCarController>(); var rb = root.GetComponent<Rigidbody>(); var wcs = e.Wheel_Colliders;
            float rF = wcs[0].radius, rR = wcs[2].radius;
            var restY = new float[4];
            for (int i = 0; i < 4; i++)
            {
                var w = wcs[i]; var ss = w.suspensionSpring; float ms0 = rb.mass / 4f - w.mass;
                float sag0 = Mathf.Min(ms0 * g / ss.spring, w.suspensionDistance * ss.targetPosition);
                restY[i] = R.InverseTransformPoint(w.transform.position).y - w.suspensionDistance * (1f - ss.targetPosition) + sag0;
            }
            float groundY = (restY[0] - rF + restY[2] - rR) * 0.5f;
            int driven = s.rwd ? 2 : 4; float rDrive = s.rwd ? rR : (rF + rR) * 0.5f;
            float engine = s.mass * (27.78f / s.t0100 * kLoss) * rDrive / driven;
            // traction limit (full-throttle button on mobile): per wheel <= 1.2 x its load x radius
            float load = s.rwd ? s.mass * g * s.rearShare / 2f : s.mass * g / 4f;
            float cap = 1.2f * load * rDrive;
            engine = Mathf.Min(engine, cap);
            float topGame = s.topReal * SpeedScale;           // km/h
            rb.mass = s.mass;
            e.driveWheels = s.rwd ? DriveType.Rear : DriveType.AllWheels;
            e.enginePower = Mathf.Round(engine);
            e.maxSpeed = Mathf.Round(topGame / 1.609f);        // EasyCarController speeds are mph
            e.brakePower = Mathf.Round(s.mass * g * 0.9f * rF / 3f);
            e.maxSteer = s.steer;
            // drag: let the car reach ~1.1x its top speed (constant drive force, linear drag)
            float a = engine * driven / rDrive / s.mass * eff;
            e.rollingDrag = Mathf.Max(0.004f, a / (1.1f * topGame / 3.6f));
            e.limiterDrag = Mathf.Max(e.rollingDrag * 6f, 0.15f);
            float ms = s.mass / 4f - s.wheelMass; float k = ms * Mathf.Pow(2f * Mathf.PI * s.freq, 2); float c = 2f * zeta * Mathf.Sqrt(k * ms);
            float sx = 0;
            for (int i = 0; i < 4; i++)
            {
                var w = wcs[i]; var ss = w.suspensionSpring; ss.spring = Mathf.Round(k); ss.damper = Mathf.Round(c); ss.targetPosition = 0.5f; w.suspensionSpring = ss;
                w.mass = s.wheelMass; w.suspensionDistance = s.travel;
                float sag = Mathf.Min(ms * g / k, s.travel * 0.5f);
                var lp = R.InverseTransformPoint(w.transform.position);
                w.transform.position = R.TransformPoint(new Vector3(lp.x, restY[i] + s.travel * 0.5f - sag, lp.z));
                sx += lp.x;
            }
            e.centerOfMass.localPosition = new Vector3(sx / 4f, groundY + s.comH * comFactor, e.centerOfMass.localPosition.z);
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            string result = $"P{number}: m={s.mass} {(s.rwd ? "RWD" : "AWD")} engine={e.enginePower} (cap {cap:0}) top={topGame:0}km/h={e.maxSpeed}mph drag={e.rollingDrag:0.000} brake={e.brakePower} spring={k:0} COMy={e.centerOfMass.localPosition.y:0.00}";
            PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
            return result;
        }
    }
}
