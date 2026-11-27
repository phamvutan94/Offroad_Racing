using ALIyerEdon;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// Test menu for the editor and Development Builds (as in the other games): a small "TEST" button opens
    /// a window of quick test actions. Disabled in release builds. Garage actions show when the garage
    /// references are set, race actions when the race links are set; every button is assigned in the prefab.
    /// </summary>
    public sealed class DebugPanel : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField] private Button openButton;
        [SerializeField] private GameObject window;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text info;

        [Header("Context (set per scene / prefab; leave empty where not used)")]
        [SerializeField] private RaceSceneLinks raceLinks;
        [SerializeField] private GarageCarSelector cars;
        [SerializeField] private LevelSelector levels;
        [SerializeField] private TuningConfig tuning;

        [Header("Shared")]
        [SerializeField] private InputField trackInput;
        [SerializeField] private Button loadTrack;
        [SerializeField] private Button speed;
        [SerializeField] private Text speedLabel;

        [Header("Garage")]
        [SerializeField] private GameObject garageGroup;
        [SerializeField] private Button addCoins;
        [SerializeField] private Button unlockCars;
        [SerializeField] private Button unlockTracks;
        [SerializeField] private Button maxTuning;
        [SerializeField] private Button resetTuning;
        [SerializeField] private Button addWins;
        [SerializeField] private Button resetProgress;

        [Header("Race")]
        [SerializeField] private GameObject raceGroup;
        [SerializeField] private Button win;
        [SerializeField] private Button finish;
        [SerializeField] private Button lose;
        [SerializeField] private Button autopilot;
        [SerializeField] private Text autopilotLabel;
        [SerializeField] private Button nextCheckpoint;
        [SerializeField] private Button restart;
        [SerializeField] private Button prevTrack;
        [SerializeField] private Button nextTrack;
        [SerializeField] private Button toGarage;

        private static readonly float[] Speeds = { 1f, 2f, 4f, 0.5f };
        private int speedIndex;
        private float baseTimeScale = -1f;
        private bool autopilotOn;
        private float fps;

        private bool InRace => raceLinks != null && raceLinks.RaceManager != null;

        private void Start()
        {
            // Only in the editor and Development Builds, never in a release build.
            if (!Debug.isDebugBuild) { gameObject.SetActive(false); return; }

            window.SetActive(false);
            openButton.onClick.AddListener(() => window.SetActive(!window.activeSelf));
            closeButton.onClick.AddListener(Close);
            garageGroup.SetActive(cars != null);
            raceGroup.SetActive(InRace);

            loadTrack.onClick.AddListener(LoadTypedTrack);
            speed.onClick.AddListener(CycleSpeed);

            addCoins.onClick.AddListener(() => GameSave.Coins += 10000);
            unlockCars.onClick.AddListener(UnlockCars);
            unlockTracks.onClick.AddListener(UnlockTracks);
            maxTuning.onClick.AddListener(() => SetTuning(true));
            resetTuning.onClick.AddListener(() => SetTuning(false));
            addWins.onClick.AddListener(AddWins);
            resetProgress.onClick.AddListener(ResetProgress);

            win.onClick.AddListener(() => FinishRace(+1));
            finish.onClick.AddListener(() => FinishRace(0));
            lose.onClick.AddListener(() => FinishRace(-1));
            autopilot.onClick.AddListener(ToggleAutopilot);
            nextCheckpoint.onClick.AddListener(TeleportToNextCheckpoint);
            restart.onClick.AddListener(() => { Close(); SceneNavigation.ReloadActive(); });
            prevTrack.onClick.AddListener(() => LoadTrackOffset(-1));
            nextTrack.onClick.AddListener(() => LoadTrackOffset(+1));
            toGarage.onClick.AddListener(() => { Close(); SceneNavigation.Load(SceneNavigation.GarageScene, null, "GARAGE"); });

            int current = CurrentTrackNumber();
            if (trackInput != null) trackInput.text = (current > 0 ? current : 1).ToString();
            RefreshLabels();
        }

        private void OnDestroy()
        {
            if (baseTimeScale > 0f && Debug.isDebugBuild) Time.timeScale = baseTimeScale;
        }

        private void Close() => window.SetActive(false);

        // ---------------------------------------------------------------- shared

        private static int TrackCount => Mathf.Max(0, SceneManager.sceneCountInBuildSettings - 2); // Boot, Garage, tracks...

        private static int CurrentTrackNumber()
        {
            string n = SceneManager.GetActiveScene().name;
            const string prefix = "Race_Track_";
            return n.StartsWith(prefix) && int.TryParse(n.Substring(prefix.Length), out int t) ? t : 0;
        }

        private void LoadTrack(int number)
        {
            if (TrackCount == 0) return;
            number = (number - 1 + TrackCount) % TrackCount + 1;
            Close();
            SceneNavigation.Load("Race_Track_" + number, null, "TRACK " + number);
        }

        private void LoadTypedTrack()
        {
            if (trackInput != null && int.TryParse(trackInput.text, out int n)) LoadTrack(n);
        }

        private void LoadTrackOffset(int offset) => LoadTrack(Mathf.Max(1, CurrentTrackNumber()) + offset);

        private void CycleSpeed()
        {
            if (baseTimeScale < 0f) baseTimeScale = Time.timeScale;
            speedIndex = (speedIndex + 1) % Speeds.Length;
            Time.timeScale = baseTimeScale * Speeds[speedIndex];
            RefreshLabels();
        }

        // ---------------------------------------------------------------- garage

        private void UnlockCars()
        {
            for (int i = 0; i < cars.CarCount; i++) GameSave.SetCarOwned(i);
            PlayerPrefs.Save();
            cars.RefreshView();
        }

        private void UnlockTracks()
        {
            int count = levels != null ? levels.TrackCount : TrackCount;
            for (int i = 0; i < count; i++) GameSave.SetLevelUnlocked(i);
            PlayerPrefs.Save();
            if (levels != null) levels.RefreshView();
        }

        private void SetTuning(bool max)
        {
            if (tuning == null) return;
            int car = cars.Index;
            foreach (var part in tuning.parts) CarTuning.SetLevel(car, part.id, max ? tuning.MaxLevel : 0);
        }

        private void AddWins()
        {
            for (int i = 0; i < 5; i++) GameSave.RecordRaceResult(0);
        }

        private void ResetProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Close();
            SceneManager.LoadScene(0); // Boot sets the first-run defaults again
        }

        // ---------------------------------------------------------------- race

        /// result: +1 = first place, 0 = current place, -1 = last place.
        private void FinishRace(int result)
        {
            Race_Manager rm = raceLinks.RaceManager;
            Car_Position player = rm.PlayerPosition;
            if (player == null) return;
            if (result != 0) rm.Update_Position(player.RacerID, result > 0 ? "999999999" : "0");
            Close();
            rm.Finish_Race();
        }

        private void ToggleAutopilot()
        {
            EasyCarController car = raceLinks.RaceManager.PlayerCar;
            if (car == null) return;
            Car_AI ai = car.GetComponent<Car_AI>();
            if (ai == null) return;
            autopilotOn = !autopilotOn;
            ai.enabled = autopilotOn;
            ai.raceStarted = autopilotOn;
            // the player's InputSystem writes the throttle every frame and would override the AI
            if (raceLinks.InputSystem != null) raceLinks.InputSystem.canControl = !autopilotOn;
            RefreshLabels();
        }

        private void TeleportToNextCheckpoint()
        {
            Race_Manager rm = raceLinks.RaceManager;
            Car_Position player = rm.PlayerPosition;
            Checkpoint_Manager cm = raceLinks.CheckpointManager;
            if (player == null || cm == null || cm.checkpoints.Count == 0) return;
            int n = cm.checkpoints.Count;
            Transform target = cm.checkpoints[(player.currentCheckpoint + 1) % n];
            Transform after = cm.checkpoints[(player.currentCheckpoint + 2) % n];
            Vector3 dir = Vector3.ProjectOnPlane(after.position - target.position, Vector3.up).normalized;
            Rigidbody body = player.GetComponent<Rigidbody>();
            Vector3 pos = target.position;
            if (Physics.Raycast(pos + Vector3.up * 30f, Vector3.down, out RaycastHit hit, 80f, ~0, QueryTriggerInteraction.Ignore)) pos = hit.point;
            pos += Vector3.up * 1.2f;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = pos;
                body.rotation = Quaternion.LookRotation(dir);
            }
            player.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
        }

        // ---------------------------------------------------------------- info

        private void RefreshLabels()
        {
            if (speedLabel != null) speedLabel.text = "SPEED x" + Speeds[speedIndex].ToString("0.##");
            if (autopilotLabel != null) autopilotLabel.text = autopilotOn ? "AUTOPILOT: ON" : "AUTOPILOT: OFF";
        }

        private void Update()
        {
            fps = Mathf.Lerp(fps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.1f);
            if (!window.activeSelf || info == null) return;
            string text = SceneManager.GetActiveScene().name + "   Coins " + GameSave.Coins.ToString("N0") + "   Car " + GameSave.SelectedCar
                          + "   Level " + GameSave.PlayerLevel + "   FPS " + Mathf.RoundToInt(fps) + "   Time x" + Time.timeScale.ToString("0.##");
            if (InRace && raceLinks.RaceManager.PlayerPosition != null)
            {
                Race_Manager rm = raceLinks.RaceManager;
                Car_Position p = rm.PlayerPosition;
                EasyCarController car = rm.PlayerCar;
                float kmh = car != null ? car.GetComponent<Rigidbody>().linearVelocity.magnitude * 3.6f : 0f;
                text += "\nLap " + Mathf.Max(1, p.currentLap) + "/" + rm.totalLaps + "   Checkpoint " + p.currentCheckpoint
                        + "   Position " + (p.currentPosition + 1) + "/" + rm.RacerCount + "   " + Mathf.RoundToInt(kmh) + " km/h";
            }
            info.text = text;
        }
    }
}
