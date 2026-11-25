using Core;
using UnityEngine;
using UnityEngine.Audio;

namespace RaceUI
{
    /// <summary>Lives on the @AudioManager prefab (Boot scene); applies the saved Core audio settings to the mixer.</summary>
    public sealed class GameAudioBridge : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioAndHapticManager manager;

        /// The persistent audio manager, if the Boot scene has run (null before that, e.g. very early in the Editor).
        public static AudioAndHapticManager Manager { get; private set; }

        private void Reset() => manager = GetComponent<AudioAndHapticManager>();

        private void Awake()
        {
            if (Manager == null) Manager = manager; // duplicates are destroyed by the Core singleton
        }

        // AudioMixer.SetFloat is ignored in Awake, so apply in Start.
        private void Start() => GameAudio.ApplyToMixer(mixer, manager);
    }
}
