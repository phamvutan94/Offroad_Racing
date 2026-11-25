using Core;
using UnityEngine;

namespace RaceUI
{
    /// <summary>Gameplay haptics routed through the Core AudioAndHapticManager (respects the Vibration setting).</summary>
    public static class GameHaptics
    {
        /// Minimum time between two collision vibrations, so scraping along a wall does not buzz continuously.
        public const float CollisionCooldown = 0.3f;
        /// Impacts this much above the car's collision threshold use the stronger vibration.
        public const float HeavyImpactMargin = 6f;

        private static float lastCollisionTime = -999f;

        public static void OnCarCollision(float impactVelocity, float threshold)
        {
            AudioAndHapticManager manager = GameAudioBridge.Manager;
            if (manager == null || !manager.EnableVibrate) return;
            if (Time.unscaledTime - lastCollisionTime < CollisionCooldown) return;
            lastCollisionTime = Time.unscaledTime;
            manager.Vibrate(impactVelocity >= threshold + HeavyImpactMargin ? HapticType.Medium : HapticType.Light);
        }
    }
}
