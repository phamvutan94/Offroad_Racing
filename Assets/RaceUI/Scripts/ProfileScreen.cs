using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>Profile: editable player name, race record (races / 1st / 2nd / 3rd) and level with XP progress.</summary>
    public sealed class ProfileScreen : MonoBehaviour
    {
        [SerializeField] private InputField nameInput;
        [SerializeField] private Text racesValue;
        [SerializeField] private Text winsValue;
        [SerializeField] private Text secondsValue;
        [SerializeField] private Text thirdsValue;
        [SerializeField] private Text levelValue;
        [SerializeField] private Text rankLabel;
        [SerializeField] private StatBar xpBar;
        [SerializeField] private Text xpLabel;

        [Header("Car framing")]
        [SerializeField] private GarageOrbitCamera orbitCamera;
        [Tooltip("Right-most panel of the screen: the car is centred in the free space to its right")]
        [SerializeField] private RectTransform panelsRightEdge;
        [Tooltip("Camera distance while the profile is open, so the whole car fits the free area")]
        [SerializeField] private float framingDistance = 5.6f;

        private readonly Vector3[] corners = new Vector3[4];

        private void Awake()
        {
            nameInput.characterLimit = GameSave.MaxNameLength;
            nameInput.onEndEdit.AddListener(SaveName);
        }

        private void OnEnable()
        {
            GameSave.ProfileChanged += Refresh;
            Refresh();
            StartCoroutine(FrameCarNextFrame());
        }

        private void OnDisable()
        {
            GameSave.ProfileChanged -= Refresh;
            if (orbitCamera != null) orbitCamera.ResetFocus();
        }

        // the screen's layout (safe area, fade-in scale) settles after the first frame
        private IEnumerator FrameCarNextFrame()
        {
            if (orbitCamera == null || panelsRightEdge == null) yield break;
            yield return null;
            panelsRightEdge.GetWorldCorners(corners); // screen pixels on an overlay canvas
            float edge = Mathf.Clamp01(corners[2].x / Mathf.Max(1, Screen.width));
            orbitCamera.SetFocusViewportX((edge + 1f) * 0.5f, framingDistance);
        }

        private void SaveName(string value) => GameSave.PlayerName = value;

        private void Refresh()
        {
            nameInput.SetTextWithoutNotify(GameSave.PlayerName);
            racesValue.text = GameSave.RacesPlayed.ToString("N0");
            winsValue.text = GameSave.Wins.ToString("N0");
            secondsValue.text = GameSave.Seconds.ToString("N0");
            thirdsValue.text = GameSave.Thirds.ToString("N0");

            int level = GameSave.PlayerLevel;
            levelValue.text = level.ToString();
            rankLabel.text = GameSave.RankTitle(level);
            xpBar.SetValue(GameSave.LevelProgress);
            int into = GameSave.Xp - GameSave.XpForLevel(level);
            int need = GameSave.XpForLevel(level + 1) - GameSave.XpForLevel(level);
            xpLabel.text = into.ToString("N0") + " / " + need.ToString("N0") + " XP";
        }
    }
}
