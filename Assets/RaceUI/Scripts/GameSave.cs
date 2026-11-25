using System;
using UnityEngine;

namespace RaceUI
{
    /// <summary>Typed access to the game's existing PlayerPrefs keys (same keys as the original ALIyerEdon scripts).</summary>
    public static class GameSave
    {
        public const string CoinsKey = "TotalScores";
        public const string CarIdKey = "CarID";
        public const string LevelIdKey = "LevelID";
        private const int Unlocked = 3; // the original scripts mark owned/unlocked items with 3

        public static event Action<int> CoinsChanged;

        public static int Coins
        {
            get => PlayerPrefs.GetInt(CoinsKey);
            set { PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, value)); CoinsChanged?.Invoke(Coins); }
        }

        /// Call after other code (e.g. Start_Finish_UI) changed the coin key directly.
        public static void NotifyCoinsChanged() => CoinsChanged?.Invoke(Coins);

        public static bool TrySpend(int amount)
        {
            if (amount > Coins) return false;
            Coins -= amount;
            return true;
        }

        public static bool IsCarOwned(int index) => PlayerPrefs.GetInt("Car" + index) == Unlocked;
        public static void SetCarOwned(int index) => PlayerPrefs.SetInt("Car" + index, Unlocked);

        public static bool IsLevelUnlocked(int index) => PlayerPrefs.GetInt("Level" + index) == Unlocked;
        public static void SetLevelUnlocked(int index) => PlayerPrefs.SetInt("Level" + index, Unlocked);

        /// 0 = 1st place, 1 = 2nd, 2 = 3rd, 3 = no podium yet (original "Award_Level_" values).
        public static int GetBestFinish(int levelIndex) => PlayerPrefs.GetInt("Award_Level_" + levelIndex, 3);

        public static int SelectedCar
        {
            get => PlayerPrefs.GetInt(CarIdKey);
            set => PlayerPrefs.SetInt(CarIdKey, value);
        }

        public static int SelectedLevel
        {
            get => PlayerPrefs.GetInt(LevelIdKey);
            set => PlayerPrefs.SetInt(LevelIdKey, value);
        }

        public static string Ordinal(int position) // 0-based
        {
            int n = position + 1;
            string suffix = (n % 100 >= 11 && n % 100 <= 13) ? "TH" : (n % 10) switch { 1 => "ST", 2 => "ND", 3 => "RD", _ => "TH" };
            return n + suffix;
        }
    }
}
