using ALIyerEdon;
using UnityEngine;

namespace RaceUI
{
    /// <summary>On a player car prefab: applies the saved tuning of this car before the controller starts.</summary>
    public sealed class CarTuningApplier : MonoBehaviour
    {
        [SerializeField] private TuningConfig config;
        [Tooltip("Index of this car in the garage car list")]
        [SerializeField] private int carIndex;
        [SerializeField] private EasyCarController controller;

        private void Reset() => controller = GetComponent<EasyCarController>();

        private void Awake()
        {
            if (config != null && controller != null) CarTuning.Apply(config, carIndex, controller);
        }
    }
}
