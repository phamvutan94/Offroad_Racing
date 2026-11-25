using Core.DesignPatterns.Singleton;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

//thêm quyền try cập vào Assets/Plugins/Android/AndroidManifest.xml
//<uses-permission android:name="android.permission.VIBRATE" />
namespace Core
{
    public class AudioAndHapticManager : Singleton<AudioAndHapticManager>
    {
        [Serializable]
        class SoundIDClipPair
        {
            public SoundID m_SoundID;
            public AudioClip m_AudioClip;
        }
        [SerializeField] List<AudioClip> playlist;
        [SerializeField]
        AudioSource m_MusicSource;
        [SerializeField]
        AudioSource m_EffectSource;
        [SerializeField, Min(0f)]
        float m_MinSoundInterval = 0.1f;
        [SerializeField]
        SoundIDClipPair[] m_Sounds;
        float m_LastSoundPlayTime;
        readonly Dictionary<SoundID, AudioClip> m_Clips = new();
        const string k_AudioSettings = "AudioSettings";
        AudioSettings m_AudioSettings = new();
        private Coroutine playlistCoroutine;

        public bool EnableMusic
        {
            get => m_AudioSettings.EnableMusic;
            set
            {
                m_AudioSettings.EnableMusic = value;
                m_MusicSource.mute = !value;
            }
        }
        public bool EnableSfx
        {
            get => m_AudioSettings.EnableSfx;
            set
            {
                m_AudioSettings.EnableSfx = value;
                m_EffectSource.mute = !value;
            }
        }
        public bool EnableVibrate
        {
            get => m_AudioSettings.EnableVibrate;
            set
            {
                m_AudioSettings.EnableVibrate = value;
            }
        }
        public float MasterVolume
        {
            get => m_AudioSettings.MasterVolume;
            set
            {
                m_AudioSettings.MasterVolume = value;
                AudioListener.volume = value;
            }
        }
        public int GraphicQuality
        {
            get => m_AudioSettings.GraphicQuality;
            set
            {
                m_AudioSettings.GraphicQuality = value;
            }
        }
        protected override void Awake()
        {
            base.Awake();
            foreach (var sound in m_Sounds)
            {
                m_Clips.Add(sound.m_SoundID, sound.m_AudioClip);
            }
            var audioSettings = LoadAudioSetting();
            EnableMusic = audioSettings.EnableMusic;
            EnableSfx = audioSettings.EnableSfx;
            MasterVolume = audioSettings.MasterVolume;
            GraphicQuality = audioSettings.GraphicQuality;
            EnableVibrate = audioSettings.EnableVibrate;

            StartCoroutine(PreloadAudioRoutine());
        }
        private IEnumerator PreloadAudioRoutine()
        {
            foreach (var pair in m_Sounds)
            {
                if (pair == null || pair.m_AudioClip == null)
                    continue;

                var clip = pair.m_AudioClip;

                if (clip.loadState == AudioDataLoadState.Unloaded)
                    clip.LoadAudioData();
            }

            if (playlist != null)
            {
                foreach (var clip in playlist)
                {
                    if (clip == null)
                        continue;

                    if (clip.loadState == AudioDataLoadState.Unloaded)
                        clip.LoadAudioData();
                }
            }

            bool loading;

            do
            {
                loading = false;

                foreach (var pair in m_Sounds)
                {
                    if (pair?.m_AudioClip == null)
                        continue;

                    if (pair.m_AudioClip.loadState == AudioDataLoadState.Loading)
                    {
                        loading = true;
                        break;
                    }
                }

                yield return null;

            } while (loading);
        }
        void PlayMusic(AudioClip audioClip, bool looping = true)
        {
            if (m_MusicSource.isPlaying)
                return;

            m_MusicSource.clip = audioClip;
            m_MusicSource.loop = looping;
            m_MusicSource.Play();
        }

        /// <summary>
        /// Play a music based on its sound ID
        /// </summary>
        /// <param name="soundID">The ID of the music</param>
        /// <param name="looping">Is music looping?</param>
        public void PlayMusic(SoundID soundID, bool looping = true)
        {
            PlayMusic(m_Clips[soundID], looping);
        }

        /// <summary>
        /// Stop the current music
        /// </summary>
        public void StopMusic()
        {
            m_MusicSource.Stop();
        }

        void PlayEffect(AudioClip audioClip)
        {
            if (Time.unscaledTime - m_LastSoundPlayTime >= m_MinSoundInterval && audioClip)
            {
                m_EffectSource.PlayOneShot(audioClip);
                m_LastSoundPlayTime = Time.unscaledTime;
            }
        }

        /// <summary>
        /// Play a sound effect based on its sound ID
        /// </summary>
        /// <param name="soundID">The ID of the sound effect</param>
        public void PlayEffect(SoundID soundID)
        {
            if (soundID == SoundID.None)
                return;

            if (!m_Clips.TryGetValue(soundID, out var clip) || clip == null)
                return;

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning(
                    $"AudioClip not ready: {soundID}, state = {clip.loadState}");
#endif
                return;
            }

            PlayEffect(clip);
        }
        public void PlayEffectAndHapticButton()
        {
            PlayEffect(SoundID.Button);
            if (EnableVibrate)
            {
                Vibrate(HapticType.Light);
            }
        }
        public void PlayEffectAndHaptic(SoundID soundID)
        {
            PlayEffect(soundID);
            if (EnableVibrate)
            {
                Vibrate(HapticType.Light);
            }
        }
        /// <summary>
        /// Bắt đầu phát danh sách nhạc
        /// </summary>
        public void PlayList()
        {
            if (playlist == null || playlist.Count == 0)
            {
                Debug.LogWarning("Playlist đang trống! Vui lòng kéo thả AudioClip vào inspector.");
                return;
            }

            // Dừng coroutine cũ nếu đang chạy để tránh phát chồng chéo nhiều luồng
            if (playlistCoroutine != null)
            {
                StopCoroutine(playlistCoroutine);
            }

            playlistCoroutine = StartCoroutine(PlayPlaylistRoutine());
        }

        /// <summary>
        /// Dừng hoàn toàn việc phát nhạc
        /// </summary>
        public void StopList()
        {
            if (playlistCoroutine != null)
            {
                StopCoroutine(playlistCoroutine);
            }
            m_MusicSource.Stop();
        }

        // Luồng xử lý việc chờ và chuyển bài
        private IEnumerator PlayPlaylistRoutine()
        {
            int currentIndex = 0;

            while (true) // Vòng lặp vô tận để lặp lại list
            {
                // Gán bài hát hiện tại và phát
                AudioClip currentClip = playlist[currentIndex];
                m_MusicSource.clip = currentClip;
                m_MusicSource.Play();

                // Lệnh yield này sẽ bắt Unity chờ đúng bằng thời lượng của bài hát
                // trước khi chạy dòng code tiếp theo.
                yield return new WaitForSeconds(currentClip.length);

                // Chuyển sang bài tiếp theo
                currentIndex++;

                // Nếu đã chạy đến cuối danh sách, quay lại bài đầu tiên (Index 0)
                if (currentIndex >= playlist.Count)
                {
                    currentIndex = 0;
                }
            }
        }
        public void SaveAudioSetting()
        {
            PlayerPrefs.SetString(k_AudioSettings, JsonUtility.ToJson(m_AudioSettings));
        }
        AudioSettings LoadAudioSetting()
        {
            return PlayerPrefs.HasKey(k_AudioSettings)
                    ? JsonUtility.FromJson<AudioSettings>(PlayerPrefs.GetString(k_AudioSettings))
                    : new AudioSettings();
        }
        public void Vibrate(HapticType type)
        {
            if (!EnableVibrate) return;

            switch (type)
            {
                case HapticType.Light:
                    HapticHelper.VibrateLight();
                    break;

                case HapticType.Medium:
                    HapticHelper.VibrateMedium();
                    break;
            }
        }
    }
    public class AudioSettings
    {
        public bool EnableMusic;
        public bool EnableSfx;
        public bool EnableVibrate;
        public float MasterVolume;
        public int GraphicQuality;
        public AudioSettings()
        {
            EnableMusic = true;
            EnableSfx = true;
            EnableVibrate = true;
            MasterVolume = 1f;
            GraphicQuality = 0;
        }
    }
    public enum SoundID
    {
        None = 0, Button = 1, Tick = 2, Magnetic = 3, Check = 4,
        FoodPick = 5, BoxPick = 6, BoosterCollected = 7, MoneyCollected = 8,
        BoosterExtraSlot = 9, BoosterShuffle = 10, BoosterPickUp = 11, BoosterTornado = 12,
        Done = 13, PopupShow = 14,
        Win = 15, Lose = 16, BoxCannotMove = 17,
    }
    public enum HapticType
    {
        Light,
        Medium,
    }
}