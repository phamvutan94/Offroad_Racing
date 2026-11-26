using System;
using ALIyerEdon;
using UnityEngine;

namespace RaceUI
{
    /// <summary>Saved tuning levels ("Tune_{car}_{part}") and applying them to a car.</summary>
    public static class CarTuning
    {
        public static event Action<int> Changed; // car index

        static string Key(int car, string part) => "Tune_" + car + "_" + part;

        public static int GetLevel(int car, string part) => PlayerPrefs.GetInt(Key(car, part), 0);

        public static void SetLevel(int car, string part, int level)
        {
            PlayerPrefs.SetInt(Key(car, part), Mathf.Max(0, level));
            PlayerPrefs.Save();
            Changed?.Invoke(car);
        }

        public static int[] Levels(TuningConfig config, int car)
        {
            var res = new int[config.parts.Length];
            for (int p = 0; p < res.Length; p++) res[p] = GetLevel(car, config.parts[p].id);
            return res;
        }

        public enum UpgradeResult { Upgraded, MaxLevel, NotEnoughCoins }

        public static UpgradeResult TryUpgrade(TuningConfig config, int car, int partIndex, out int price)
        {
            var part = config.parts[partIndex];
            int level = GetLevel(car, part.id);
            price = config.PriceForNextLevel(level);
            if (level >= config.MaxLevel) return UpgradeResult.MaxLevel;
            if (!GameSave.TrySpend(price)) return UpgradeResult.NotEnoughCoins;
            SetLevel(car, part.id, level + 1);
            return UpgradeResult.Upgraded;
        }

        /// Multiplies the car's physics values by the saved part levels. Call before the car's Start.
        public static void Apply(TuningConfig config, int car, EasyCarController controller)
        {
            float engine = 1, speed = 1, brake = 1, brakeFric = 1, steerSens = 1, steer = 1, handBrake = 1;
            foreach (var part in config.parts)
            {
                int lv = GetLevel(car, part.id);
                engine += part.enginePower * lv;
                speed += part.maxSpeed * lv;
                brake += part.brakePower * lv;
                brakeFric += part.brakeFriction * lv;
                steerSens += part.steerSensibility * lv;
                steer += part.maxSteer * lv;
                handBrake += part.handBrakeFriction * lv;
            }
            controller.enginePower *= engine;
            controller.maxSpeed *= speed;
            controller.brakePower *= brake;
            controller.brakeFriction *= brakeFric;
            controller.steerSensibility *= steerSens;
            controller.maxSteer *= steer;
            controller.handBrakeFriction *= Mathf.Max(0.2f, handBrake);
        }
    }
}
