using Core;
using UnityEngine;
using UnityEngine.Audio;

namespace RaceUI
{
    /// <summary>
    /// Bridges the Core AudioAndHapticManager settings (music / sfx on-off, master volume) to the game's
    /// AudioMixer, so they also affect the scene music and the car sounds (routed to the Music / SFX groups).
    /// </summary>
    public static class GameAudio
    {
        public const string MusicParam = "MusicVolume";
        public const string SfxParam = "SfxVolume";
        private const float Off = -80f;

        public static void ApplyToMixer(AudioMixer mixer, AudioAndHapticManager manager)
        {
            if (mixer == null || manager == null) return;
            mixer.SetFloat(MusicParam, manager.EnableMusic ? 0f : Off);
            mixer.SetFloat(SfxParam, manager.EnableSfx ? 0f : Off);
        }

        /// Restores the listener after the game muted it (pause / scene change). Uses the Core master volume.
        public static void RestoreListenerVolume()
        {
            // Use the bridge's reference: Core's Instance getter logs an error when the manager is not loaded yet.
            AudioAndHapticManager manager = GameAudioBridge.Manager;
            AudioListener.volume = manager != null ? manager.MasterVolume : 1f;
        }
    }
}
