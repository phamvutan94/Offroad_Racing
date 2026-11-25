using ALIyerEdon;
using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>In-race HUD: speed, gear, rev ring, lap, race time, progress, position and standings.</summary>
    public sealed class RaceHUD : MonoBehaviour
    {
        [Header("Game")]
        [SerializeField] private RaceSceneLinks links;
        [SerializeField] private float speedMultiplier = 1.7f; // same factor as the original RPM display

        [Header("Speedometer")]
        [SerializeField] private Text speedText;
        [SerializeField] private Text gearText;
        [SerializeField] private Image revRing;
        [SerializeField, Range(0.1f, 1f)] private float revRingArc = 0.75f;

        [Header("Race info")]
        [SerializeField] private Text lapText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text positionText;
        [SerializeField] private Text[] standingRows;
        [SerializeField] private Color playerRowColor = RaceUITheme.Accent;
        [SerializeField] private Color otherRowColor = Color.white;

        private float smoothedRevs;
        private bool finished;

        public float RaceTime { get; private set; }

        public void MarkFinished() => finished = true;

        public static string FormatTime(float seconds)
        {
            int minutes = (int)(seconds / 60f);
            int secs = (int)(seconds % 60f);
            int millis = (int)((seconds * 1000f) % 1000f);
            return minutes.ToString("00") + ":" + secs.ToString("00") + ":" + millis.ToString("000");
        }

        private void Update()
        {
            Race_Manager raceManager = links.RaceManager;
            Pause_Menu pauseMenu = links.PauseMenu;
            Checkpoint_Manager checkpointManager = links.CheckpointManager;
            if (raceManager == null) return;
            EasyCarController car = raceManager.PlayerCar;
            Car_Position position = raceManager.PlayerPosition;
            if (car == null || position == null) return;

            if (pauseMenu.raceIsStarted && !finished) RaceTime += Time.deltaTime;

            speedText.text = Mathf.RoundToInt(Mathf.Abs(car.currentSpeed) * speedMultiplier).ToString("000");
            gearText.text = car.reversing ? "R" : (car.currentGear + 1).ToString();
            smoothedRevs = Mathf.Lerp(smoothedRevs, Mathf.Clamp01(car.Revs), Time.deltaTime * 5f);
            revRing.fillAmount = Mathf.Max(0.03f, smoothedRevs) * revRingArc;

            int laps = Mathf.Max(1, raceManager.totalLaps);
            int lap = Mathf.Clamp(position.currentLap, 1, laps);
            lapText.text = "LAP " + lap + "/" + laps;
            timeText.text = FormatTime(RaceTime);
            positionText.text = (position.currentPosition + 1) + "/" + Mathf.Max(1, raceManager.RacerCount);

            int checkpoints = checkpointManager != null ? Mathf.Max(1, checkpointManager.checkpoints.Count) : 1;
            float done = (Mathf.Max(0, position.currentLap - 1) * checkpoints + position.currentCheckpoint) / (float)(laps * checkpoints);
            progressText.text = Mathf.RoundToInt(Mathf.Clamp01(done) * 100f) + "%";

            for (int i = 0; i < standingRows.Length; i++)
            {
                string racer = raceManager.GetRacerNameAtPosition(i);
                standingRows[i].gameObject.SetActive(racer.Length > 0);
                standingRows[i].text = (i + 1) + "  " + racer.ToUpperInvariant();
                standingRows[i].color = racer == raceManager.PlayerName ? playerRowColor : otherRowColor;
            }
        }
    }
}
