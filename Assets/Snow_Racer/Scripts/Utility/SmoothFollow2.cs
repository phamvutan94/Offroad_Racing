//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ALIyerEdon;
namespace ALIyerEdon
{
    public class SmoothFollow2 : MonoBehaviour
    {
        public Transform target;

        public float smooth = 0.33f;
        public float distance = 4.56f;
        public float height = 0.5f;
        public float Angle = 6.39f;
        [HideInInspector] public float daynamicCameraIntensity = 0.5f;

        public LayerMask lineOfSightMask = 0;

        private Rigidbody myRigidbody;
        private float yVelocity = 0.0f;
        private float xVelocity = 0.0f;

        bool started;

        // Extra look-around angles (set by RaceUI.RaceCameraSwipe). Zero = original behaviour.
        [HideInInspector] public float yawOffset;
        [HideInInspector] public float pitchOffset;
        // Pinch zoom (set by RaceUI.RaceCameraSwipe): multiplies the follow distance. 1 = original behaviour.
        [HideInInspector] public float zoom = 1f;
        float baseX, baseY;
        bool baseReady;

        IEnumerator Start()
        {

            yield return new WaitForEndOfFrame();

            target = GameObject.FindGameObjectWithTag("Player").transform;

            myRigidbody = target.GetComponent<Rigidbody>();

            // Tall cars carry their own chase framing (RaceUI.CarCameraFraming); other cars keep the scene values.
            var framing = target.GetComponent<RaceUI.CarCameraFraming>();
            if (framing != null)
            {
                distance = framing.Distance;
                height = framing.Height;
                Angle = framing.Angle;
            }

            //Face the camera to the car forward at start
            transform.position = new Vector3(target.position.x,
                target.position.y + 300f, target.position.z);

            transform.LookAt(GameObject.FindGameObjectWithTag("Player")
                    .GetComponent<Car_AI>().rayPosition);

            started = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!started)
                return;

            // Damp angle from current y-angle towards target y-angle
            // (the damped base is kept separately so the look-around offset does not feed back into it)
            if (!baseReady) { baseX = transform.eulerAngles.x; baseY = transform.eulerAngles.y; baseReady = true; }

            baseX = Mathf.SmoothDampAngle(baseX, target.eulerAngles.x + Angle, ref xVelocity, smooth);
            baseY = Mathf.SmoothDampAngle(baseY, target.eulerAngles.y, ref yVelocity, smooth);

            // Look at the target
            transform.eulerAngles = new Vector3(baseX + pitchOffset, baseY + yawOffset, 0.0f);

            var direction = transform.rotation * -Vector3.forward;
            float zoomedHeight = height * Mathf.Lerp(1f, zoom, 0.6f);
            var targetDistance = AdjustLineOfSight(target.position + new Vector3(0, zoomedHeight, 0), direction);

            transform.position = target.position + new Vector3(0, zoomedHeight, 0) + direction * targetDistance;

        }

        float AdjustLineOfSight(Vector3 target, Vector3 direction)
        {
            RaycastHit hit;

            float zoomedDistance = distance * zoom;
            if (Physics.Raycast(target, direction, out hit, zoomedDistance, lineOfSightMask.value))
                return hit.distance;
            else
                return zoomedDistance;
        }
    }
}