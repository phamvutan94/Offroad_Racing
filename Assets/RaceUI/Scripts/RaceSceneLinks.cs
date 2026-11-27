using ALIyerEdon;
using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// The only place in the shared race-UI prefab that references the race scene's systems.
    /// Each scene overrides these fields once; every button / EventTrigger in the prefab calls the proxy methods
    /// below, so the prefab itself never stores scene references and can be edited in one place.
    /// </summary>
    public sealed class RaceSceneLinks : MonoBehaviour
    {
        [Header("Scene systems (set per scene)")]
        [SerializeField] private Race_Manager raceManager;
        [SerializeField] private Pause_Menu pauseMenu;
        [SerializeField] private InputSystem inputSystem;
        [SerializeField] private Nitro nitro;
        [SerializeField] private Load_Settings loadSettings;
        [SerializeField] private Checkpoint_Manager checkpointManager;
        [SerializeField] private Behaviour sunShaftEffect;
        [SerializeField] private string trackTitle = "TRACK 1";
        [Tooltip("Camera used after the finish line (CameraSwitch slot 0, the Main Camera); finish FX are attached to it")]
        [SerializeField] private Transform finishCamera;
        [Tooltip("Two AI lanes either side of the centre (Path_2, Path_3); their average is the track centreline")]
        [SerializeField] private Waypoint_System[] trackLanes;
        [Tooltip("Horizontal distance from the centreline beyond which the player's car counts as off the track")]
        [SerializeField] private float trackLimit = 30f;

        [Header("Inside the prefab")]
        [SerializeField] private Text[] trackTitleLabels;
        [SerializeField] private Text trackSubtitle;

        [Header("Start")]
        [Tooltip("Start the race as soon as the game would show the pre-race menu (no START button).")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private GameObject raceMenus;   // Race_Manager.startUI / raceFinishUI
        [SerializeField] private GameObject resultScreen;

        private bool autoStarted;

        public Race_Manager RaceManager => raceManager;
        public Pause_Menu PauseMenu => pauseMenu;
        public InputSystem InputSystem => inputSystem;
        public Load_Settings LoadSettings => loadSettings;
        public Checkpoint_Manager CheckpointManager => checkpointManager;
        public Behaviour SunShaftEffect => sunShaftEffect;
        public string TrackTitle => trackTitle;
        public Transform FinishCamera => finishCamera;
        public Waypoint_System[] TrackLanes => trackLanes;
        public float TrackLimit => trackLimit;

        private void Start()
        {
            foreach (Text t in trackTitleLabels) if (t != null) t.text = trackTitle;
            if (trackSubtitle != null && raceManager != null)
                trackSubtitle.text = "CIRCUIT  /  " + raceManager.totalLaps + (raceManager.totalLaps == 1 ? " LAP" : " LAPS");
        }

        private void Update()
        {
            // Race_Manager activates the menus root when it is ready to start; the result screen is still hidden then.
            if (!autoStart || autoStarted || raceManager == null) return;
            if (raceMenus.activeInHierarchy && !resultScreen.activeSelf)
            {
                autoStarted = true;
                raceManager.StartRace_Button();
            }
        }

        // ---- menu proxies (buttons) ----
        public void StartRace() => raceManager.StartRace_Button();
        public void Pause() => pauseMenu.Pause();
        public void Resume() => pauseMenu.Resume();
        public void Restart() => SceneNavigation.ReloadActive(null, trackTitle);
        public void ExitToGarage() => SceneNavigation.Load(SceneNavigation.GarageScene, null, "GARAGE");
        public void SwitchCamera() => inputSystem.Switch_Camera();

        // ---- driving proxies (EventTriggers on the touch controls) ----
        public void Throttle() => inputSystem.Throttle();
        public void ThrottleRelease() => inputSystem.ThrottleRelease();
        public void Brake(bool pressed) => inputSystem.Brake(pressed);
        public void HandBrake(bool pressed) => inputSystem.Hand_Brake(pressed);
        public void Steer(bool right) => inputSystem.Steer(right);
        public void SteerRelease() => inputSystem.SteerRelease();
        public void NitroPress() => nitro.Apply_Nitro();
        public void NitroRelease() => nitro.Release_Nitro();
    }
}
