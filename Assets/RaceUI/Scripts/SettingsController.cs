using ALIyerEdon;
using Core;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// Settings screen. Writes the same PlayerPrefs keys as the old SettingsMenu and applies them live
    /// through the scene systems assigned in the Inspector (all optional: Garage has no InputSystem/Race_Manager).
    /// </summary>
    public sealed class SettingsController : MonoBehaviour
    {
        [Header("Rows")]
        [SerializeField] private UISegmentedOption quality;
        [SerializeField] private UISegmentedOption sunShaft;
        [SerializeField] private UISegmentedOption wheelSmoke;
        [SerializeField] private UISegmentedOption balloons;
        [SerializeField] private UISegmentedOption controlType;
        [SerializeField] private UISegmentedOption dynamicCamera;
        [SerializeField] private UISegmentedOption positionUI;
        [SerializeField] private UISegmentedOption localPosition;
        [SerializeField] private UISegmentedOption displayFps;
        [SerializeField] private Slider tiltSensitivity;
        [SerializeField] private Text tiltValue;
        [SerializeField] private Slider musicVolume;
        [SerializeField] private Text musicValue;

        [Header("Sound (Core AudioAndHapticManager)")]
        [SerializeField] private Slider masterVolume;
        [SerializeField] private Text masterValue;
        [SerializeField] private UISegmentedOption musicEnabled;
        [SerializeField] private UISegmentedOption sfxEnabled;
        [SerializeField] private UISegmentedOption vibration;
        [SerializeField] private AudioMixer mixer;

        [Header("Race scenes: take the systems from the shared prefab's links (optional)")]
        [SerializeField] private RaceSceneLinks raceLinks;

        [Header("Scene systems (optional)")]
        [SerializeField] private Load_Settings loadSettings;
        [SerializeField] private InputSystem inputSystem;
        [SerializeField] private FPSCounter fpsCounter;
        [SerializeField] private Race_Manager raceManager;
        [SerializeField] private Behaviour sunShaftEffect;

        private const int MaxQualityLevel = 3; // High (Ultra/Max are hidden in the prefab)

        private bool listening;

        private void Awake()
        {
            if (raceLinks != null)
            {
                loadSettings = raceLinks.LoadSettings;
                inputSystem = raceLinks.InputSystem;
                raceManager = raceLinks.RaceManager;
                sunShaftEffect = raceLinks.SunShaftEffect;
            }
            quality.OnChanged.AddListener(SetQuality);
            sunShaft.OnChanged.AddListener(SetSunShaft);
            wheelSmoke.OnChanged.AddListener(SetWheelSmoke);
            balloons.OnChanged.AddListener(SetBalloons);
            controlType.OnChanged.AddListener(SetControlType);
            dynamicCamera.OnChanged.AddListener(v => PlayerPrefs.SetInt("Dynamic Camera", v));
            positionUI.OnChanged.AddListener(SetPositionUI);
            localPosition.OnChanged.AddListener(SetLocalPosition);
            displayFps.OnChanged.AddListener(SetDisplayFps);
            tiltSensitivity.onValueChanged.AddListener(SetTilt);
            musicVolume.onValueChanged.AddListener(SetMusic);
            masterVolume.onValueChanged.AddListener(SetMaster);
            musicEnabled.OnChanged.AddListener(v => SetAudio(m => m.EnableMusic = v == 1));
            sfxEnabled.OnChanged.AddListener(v => SetAudio(m => m.EnableSfx = v == 1));
            vibration.OnChanged.AddListener(SetVibration);
        }

        private static AudioAndHapticManager Audio => AudioAndHapticManager.Instance;

        private void SetAudio(System.Action<AudioAndHapticManager> change)
        {
            if (Audio == null) return;
            change(Audio);
            GameAudio.ApplyToMixer(mixer, Audio);
            Audio.SaveAudioSetting();
        }

        private void SetVibration(int v)
        {
            SetAudio(m => m.EnableVibrate = v == 1);
            if (v == 1 && Audio != null) Audio.Vibrate(HapticType.Light); // feedback
        }

        private void SetMaster(float v)
        {
            if (!listening) return;
            SetAudio(m => m.MasterVolume = v);
            UpdateValueLabels();
        }

        private void OnEnable()
        {
            listening = false;
            quality.SetWithoutNotify(Mathf.Min(PlayerPrefs.GetInt("QualityLevel"), MaxQualityLevel));
            sunShaft.SetWithoutNotify(PlayerPrefs.GetInt("Sun Shaft"));
            wheelSmoke.SetWithoutNotify(PlayerPrefs.GetInt("Wheel Smoke"));
            balloons.SetWithoutNotify(PlayerPrefs.GetInt("Balloons"));
            controlType.SetWithoutNotify(PlayerPrefs.GetInt("ControlType"));
            dynamicCamera.SetWithoutNotify(PlayerPrefs.GetInt("Dynamic Camera"));
            positionUI.SetWithoutNotify(PlayerPrefs.GetInt("ShowPositionUI") == 3 ? 1 : 0);
            localPosition.SetWithoutNotify(PlayerPrefs.GetInt("ShowLocalPosition") == 3 ? 1 : 0);
            displayFps.SetWithoutNotify(PlayerPrefs.GetInt("Display FPS"));
            tiltSensitivity.SetValueWithoutNotify(PlayerPrefs.GetFloat("accelSensibility", 100f));
            musicVolume.SetValueWithoutNotify(PlayerPrefs.GetFloat("Music", 0.7f));
            if (Audio != null)
            {
                masterVolume.SetValueWithoutNotify(Audio.MasterVolume);
                musicEnabled.SetWithoutNotify(Audio.EnableMusic ? 1 : 0);
                sfxEnabled.SetWithoutNotify(Audio.EnableSfx ? 1 : 0);
                vibration.SetWithoutNotify(Audio.EnableVibrate ? 1 : 0);
            }
            UpdateValueLabels();
            listening = true;
        }

        private void SetQuality(int v)
        {
            PlayerPrefs.SetInt("QualityLevel", v);
            if (loadSettings != null) loadSettings.Set_QualityLevel(v);
        }

        private void SetSunShaft(int v)
        {
            PlayerPrefs.SetInt("Sun Shaft", v);
            if (sunShaftEffect != null) sunShaftEffect.enabled = v == 1;
        }

        private void SetWheelSmoke(int v)
        {
            PlayerPrefs.SetInt("Wheel Smoke", v);
            if (loadSettings != null) loadSettings.Update_SmokeEffect();
            if (raceManager != null && raceManager.PlayerCar != null) raceManager.PlayerCar.enableWheelEffects = v == 1;
        }

        private void SetBalloons(int v)
        {
            PlayerPrefs.SetInt("Balloons", v);
            if (loadSettings != null) loadSettings.Update_Balloons();
        }

        private void SetControlType(int v)
        {
            PlayerPrefs.SetInt("ControlType", v);
            if (inputSystem != null) inputSystem.Update_InputType();
        }

        private void SetPositionUI(int v)
        {
            PlayerPrefs.SetInt("ShowPositionUI", v == 1 ? 3 : 0);
            if (raceManager != null && raceManager.positionUI != null) raceManager.positionUI.SetActive(v == 1);
        }

        private void SetLocalPosition(int v)
        {
            PlayerPrefs.SetInt("ShowLocalPosition", v == 1 ? 3 : 0);
            if (raceManager != null) raceManager.showLocalPosition = v == 1;
        }

        private void SetDisplayFps(int v)
        {
            PlayerPrefs.SetInt("Display FPS", v);
            if (fpsCounter != null) fpsCounter.panel.SetActive(v == 1);
        }

        private void SetTilt(float v)
        {
            if (!listening) return;
            PlayerPrefs.SetFloat("accelSensibility", v);
            if (inputSystem != null) inputSystem.Update_InputType();
            UpdateValueLabels();
        }

        private void SetMusic(float v)
        {
            if (!listening) return;
            PlayerPrefs.SetFloat("Music", v);
            if (loadSettings != null) loadSettings.Update_MusicVolume(v);
            UpdateValueLabels();
        }

        private void UpdateValueLabels()
        {
            if (tiltValue != null) tiltValue.text = Mathf.RoundToInt(tiltSensitivity.value).ToString();
            if (musicValue != null) musicValue.text = Mathf.RoundToInt(musicVolume.value * 100f).ToString();
            if (masterValue != null) masterValue.text = Mathf.RoundToInt(masterVolume.value * 100f).ToString();
        }
    }
}
