using System.Collections;
using ALIyerEdon;
using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// Finish screen. Race_Manager.Finish_Race activates it and Start_Finish_UI grants the award coins;
    /// this view then shows the final position, reward and standings.
    /// </summary>
    public sealed class RaceResultView : MonoBehaviour
    {
        [SerializeField] private RaceSceneLinks links;
        [SerializeField] private Start_Finish_UI startFinish;
        [SerializeField] private RaceHUD hud;

        [SerializeField] private Text positionText;
        [SerializeField] private Text captionText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Text[] rowPositions;
        [SerializeField] private Text[] rowNames;
        [SerializeField] private Text[] rowTimes;
        [SerializeField] private Image[] rowBackgrounds;
        [SerializeField] private Color playerRowColor = RaceUITheme.Accent;
        [SerializeField] private Color otherRowColor = RaceUITheme.Row;

        [Header("Entrance")]
        [SerializeField] private UISequenceAnimator entrance;
        [Tooltip("Seconds after the screen appears before the player's car brakes to a stop")]
        [SerializeField] private float stopCarDelay = 0.15f;
        [SerializeField] private float stopCarDamping = 2.5f;

        [Header("Finish FX (Epic Toon FX confetti, spawned around the player's car)")]
        [SerializeField] private GameObject confettiCannon;   // directional, fired from both sides of the car
        [SerializeField] private GameObject confettiShower;   // falls over the car
        [SerializeField] private GameObject confettiBlast;    // one burst in the middle of the screen
        [SerializeField] private float cannonSide = 4.5f;
        [SerializeField] private float showerHeight = 7f;
        [SerializeField] private float confettiScale = 2f;

        private void OnEnable()
        {
            if (hud != null) hud.MarkFinished();
            StartCoroutine(FillNextFrame());
            PlayConfetti();
            StartCoroutine(StopPlayerCar());
        }

        private IEnumerator FillNextFrame()
        {
            yield return null; // Start_Finish_UI.Update_Award runs right after this screen is activated
            Race_Manager raceManager = links.RaceManager;
            Car_Position player = raceManager.PlayerPosition;
            if (player == null) yield break;

            int place = player.currentPosition;
            int reward = place == 0 ? startFinish.goldAward : place == 1 ? startFinish.bronzeAward : place == 2 ? startFinish.silverAward : 0;
            positionText.text = GameSave.Ordinal(place);
            captionText.text = place == 0 ? "TRAIL CONQUERED" : place < 3 ? "ON THE PODIUM" : "KEEP PUSHING";
            if (reward > 0) StartCoroutine(CountUp(reward)); else rewardText.text = "NO REWARD";
            GameSave.NotifyCoinsChanged();
            // the coins are already added; the garage shows the "coins received" popup for them
            GameSave.AddPendingReward(reward, GameSave.Ordinal(place) + " place in " + links.TrackTitle);
            GameSave.RecordRaceResult(place);

            for (int i = 0; i < rowNames.Length; i++)
            {
                string racer = raceManager.GetRacerNameAtPosition(i);
                bool used = racer.Length > 0;
                rowBackgrounds[i].gameObject.SetActive(used);
                if (!used) continue;
                bool isPlayer = racer == raceManager.PlayerName;
                rowPositions[i].text = (i + 1).ToString("00");
                rowNames[i].text = racer.ToUpperInvariant();
                rowTimes[i].text = isPlayer && hud != null ? RaceHUD.FormatTime(hud.RaceTime) : "RACING";
                rowBackgrounds[i].color = isPlayer ? playerRowColor : otherRowColor;
            }
        }

        private IEnumerator CountUp(int reward)
        {
            rewardText.text = "+0 COINS";
            float wait = entrance != null ? entrance.Duration - 0.5f : 0f;
            float start = Time.realtimeSinceStartup + wait;
            while (Time.realtimeSinceStartup < start) yield return null;
            const float duration = 0.9f;
            for (float t = 0; t < duration; t = Time.realtimeSinceStartup - start)
            {
                float k = 1f - Mathf.Pow(1f - t / duration, 3f);
                rewardText.text = "+" + Mathf.RoundToInt(reward * k).ToString("N0") + " COINS";
                yield return null;
            }
            rewardText.text = "+" + reward.ToString("N0") + " COINS";
        }

        /// The original game hands the player's car to its AI after the finish line; brake it to a stop instead.
        private IEnumerator StopPlayerCar()
        {
            for (float t = 0; t < stopCarDelay; t += Time.unscaledDeltaTime) yield return null;
            EasyCarController car = links.RaceManager != null ? links.RaceManager.PlayerCar : null;
            if (car == null) yield break;
            Car_AI ai = car.GetComponent<Car_AI>();
            if (ai != null) ai.enabled = false;
            Rigidbody body = car.GetComponent<Rigidbody>();
            while (car != null && isActiveAndEnabled)
            {
                car.throttleInput = 0f;
                car.steerInput = 0f;
                if (body != null)
                {
                    // EasyCarController resets the rigidbody drag every frame, so damp the horizontal velocity here
                    float k = Mathf.Exp(-stopCarDamping * Time.fixedDeltaTime);
                    Vector3 v = body.linearVelocity;
                    body.linearVelocity = new Vector3(v.x * k, v.y, v.z * k);
                    body.angularVelocity *= k;
                    if (new Vector2(v.x, v.z).sqrMagnitude < 0.25f) body.linearVelocity = new Vector3(0f, Mathf.Min(0f, v.y), 0f);
                }
                yield return new WaitForFixedUpdate();
            }
        }

        private void PlayConfetti()
        {
            Transform cam = links.FinishCamera;
            if (cam != null)
            {
                // attached to the finish camera so the celebration is always on screen
                // spread over the whole view (about +-9 m wide at 8 m in front of the camera on a 19.5:9 screen)
                if (confettiBlast != null)
                    foreach (var p in BlastSpots)
                        Spawn(confettiBlast, cam, p, Quaternion.LookRotation(Vector3.up), 1.8f, 7f);
                if (confettiCannon != null)
                    foreach (float side in new[] { -1f, 1f })
                        Spawn(confettiCannon, cam, new Vector3(side * 7.5f, -4f, 8f),
                              Quaternion.LookRotation(new Vector3(-side * 0.4f, 1f, 0.1f)), 1.8f, 7f);
                if (confettiShower != null)
                {
                    GameObject fx = Spawn(confettiShower, cam, new Vector3(0f, 5f, 7f), Quaternion.Euler(270f, 0f, 0f), 1.6f, 9f);
                    WidenShower(fx, 22f, 6f, 1.6f);
                }
                return;
            }
            // fallback: around the player's car
            EasyCarController car = links.RaceManager != null ? links.RaceManager.PlayerCar : null;
            if (car == null) return;
            Transform t = car.transform;
            if (confettiCannon != null)
                foreach (float side in new[] { -1f, 1f })
                {
                    GameObject fx = Instantiate(confettiCannon, t.position + t.right * side * cannonSide, Quaternion.LookRotation(Vector3.up * 1.6f - t.right * side));
                    fx.transform.localScale = Vector3.one * confettiScale;
                    Destroy(fx, 8f);
                }
            if (confettiShower != null)
            {
                GameObject fx = Instantiate(confettiShower, t.position + Vector3.up * showerHeight, confettiShower.transform.rotation);
                fx.transform.localScale = Vector3.one * confettiScale;
                Destroy(fx, 10f);
            }
        }

        private static readonly Vector3[] BlastSpots =
        {
            new Vector3(-6f, 1.5f, 8f), new Vector3(6f, 1.5f, 8f), new Vector3(-3f, -1.5f, 7f), new Vector3(3f, -1.5f, 7f), new Vector3(0f, 2.5f, 9f),
        };

        /// Stretches the shower's emitter box to width x depth metres (keeping the density) so it covers the screen.
        private static void WidenShower(GameObject fx, float width, float depth, float scale)
        {
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>())
            {
                var shape = ps.shape; Vector3 box = shape.scale;
                float kx = width / Mathf.Max(0.01f, box.x * scale), ky = depth / Mathf.Max(0.01f, box.y * scale);
                shape.scale = new Vector3(box.x * kx, box.y * ky, box.z);
                var emission = ps.emission; emission.rateOverTimeMultiplier *= kx * ky;
                var main = ps.main; main.maxParticles = Mathf.Max(main.maxParticles, 3000);
            }
        }

        private static GameObject Spawn(GameObject prefab, Transform parent, Vector3 localPos, Quaternion localRot, float scale, float life)
        {
            GameObject fx = Instantiate(prefab, parent);
            fx.transform.localPosition = localPos;
            fx.transform.localRotation = localRot;
            fx.transform.localScale = Vector3.one * scale;
            Destroy(fx, life);
            return fx;
        }
    }
}
