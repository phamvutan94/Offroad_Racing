using UnityEngine;

namespace RaceUI
{
    /// <summary>Performance stats shown on the tuning radar (0..10).</summary>
    public enum TuningStat { Acceleration = 0, Speed = 1, Handling = 2, Braking = 3, Drifting = 4, Offroad = 5 }

    /// <summary>
    /// All tuning data in one asset: upgrade prices, what every part level adds to the radar stats,
    /// and how much it changes the car physics in the race.
    /// </summary>
    [CreateAssetMenu(menuName = "Offroad Rush/Tuning Config", fileName = "TuningConfig")]
    public sealed class TuningConfig : ScriptableObject
    {
        public const int StatCount = 6;

        [System.Serializable]
        public sealed class Part
        {
            public string id = "engine";
            public string displayName = "ENGINE";
            [Tooltip("Tier art shown at levels 1, 4 and 7 (and on the category tab: the highest tier)")]
            public Sprite[] tierSprites = new Sprite[3];
            [Tooltip("Radar stat gain per level: Acceleration, Speed, Handling, Braking, Drifting, Offroad")]
            public float[] statPerLevel = new float[StatCount];

            [Header("Physics change per level (fraction, 0.05 = +5%)")]
            public float enginePower;
            public float maxSpeed;
            public float brakePower;
            public float brakeFriction;
            public float steerSensibility;
            public float maxSteer;
            [Tooltip("Negative = the car slides more on the handbrake (better drifting)")]
            public float handBrakeFriction;
        }

        [Tooltip("Coin price of level 1..N (index 0 = buying level 1)")]
        public int[] levelPrices = { 500, 800, 1200, 1800, 2600, 3600, 5000 };
        public Part[] parts = new Part[0];

        [Tooltip("Stock radar stats per car (same order as the garage car list)")]
        public CarStats[] cars = new CarStats[0];

        [Tooltip("Performance index = sum of radar stats x this")]
        public float performanceScale = 15f;
        public int classBAt = 600;
        public int classAAt = 700;
        public int classSAt = 800;

        [System.Serializable]
        public sealed class CarStats
        {
            public float[] stats = { 6.1f, 5.5f, 6.1f, 5.7f, 5.8f, 5.1f };
        }

        public int MaxLevel => levelPrices.Length;

        public int PriceForNextLevel(int currentLevel) =>
            currentLevel >= 0 && currentLevel < levelPrices.Length ? levelPrices[currentLevel] : 0;

        public float[] StockStats(int car)
        {
            var src = car >= 0 && car < cars.Length ? cars[car].stats : null;
            var res = new float[StatCount];
            for (int i = 0; i < StatCount; i++) res[i] = src != null && i < src.Length ? src[i] : 5f;
            return res;
        }

        /// Radar stats of a car with the given part levels (levels[p] for parts[p]).
        public float[] Stats(int car, int[] levels)
        {
            var res = StockStats(car);
            for (int p = 0; p < parts.Length; p++)
                for (int s = 0; s < StatCount && s < parts[p].statPerLevel.Length; s++)
                    res[s] += parts[p].statPerLevel[s] * levels[p];
            for (int s = 0; s < StatCount; s++) res[s] = Mathf.Clamp(res[s], 0f, 10f);
            return res;
        }

        public int PerformanceIndex(float[] stats)
        {
            float sum = 0; foreach (var v in stats) sum += v;
            return Mathf.RoundToInt(sum * performanceScale);
        }

        public string ClassLetter(int pi) => pi >= classSAt ? "S" : pi >= classAAt ? "A" : pi >= classBAt ? "B" : "C";
    }
}
