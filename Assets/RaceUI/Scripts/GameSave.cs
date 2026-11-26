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

        // Coins won somewhere else (race result) that the garage announces with the "coins received" popup.
        public static int PendingReward => PlayerPrefs.GetInt("PendingReward", 0);
        public static string PendingRewardSource => PlayerPrefs.GetString("PendingRewardSource", "");

        public static void AddPendingReward(int coins, string source)
        {
            if (coins <= 0) return;
            PlayerPrefs.SetInt("PendingReward", PendingReward + coins);
            PlayerPrefs.SetString("PendingRewardSource", source);
        }

        public static void ClearPendingReward()
        {
            PlayerPrefs.DeleteKey("PendingReward");
            PlayerPrefs.DeleteKey("PendingRewardSource");
        }

        // ---- profile ----
        public const string DefaultPlayerName = "PLAYER";
        public const int MaxNameLength = 16;
        public static event Action ProfileChanged;

        public static string PlayerName
        {
            get => PlayerPrefs.GetString("PlayerName", DefaultPlayerName);
            set
            {
                string n = string.IsNullOrWhiteSpace(value) ? DefaultPlayerName : value.Trim();
                if (n.Length > MaxNameLength) n = n.Substring(0, MaxNameLength);
                PlayerPrefs.SetString("PlayerName", n);
                ProfileChanged?.Invoke();
            }
        }

        public static int RacesPlayed => PlayerPrefs.GetInt("Stat_Races", 0);
        public static int Wins => PlayerPrefs.GetInt("Stat_Place0", 0);
        public static int Seconds => PlayerPrefs.GetInt("Stat_Place1", 0);
        public static int Thirds => PlayerPrefs.GetInt("Stat_Place2", 0);

        /// Called once when a race is finished; place is 0-based.
        public static void RecordRaceResult(int place)
        {
            PlayerPrefs.SetInt("Stat_Races", RacesPlayed + 1);
            if (place >= 0 && place < 3) PlayerPrefs.SetInt("Stat_Place" + place, PlayerPrefs.GetInt("Stat_Place" + place, 0) + 1);
            PlayerPrefs.Save();
            ProfileChanged?.Invoke();
        }

        // XP: every race counts, podiums count more, so winning levels up fastest.
        public const int XpPerRace = 50, XpWin = 150, XpSecond = 90, XpThird = 60, XpLevelStep = 200;

        public static int Xp => RacesPlayed * XpPerRace + Wins * XpWin + Seconds * XpSecond + Thirds * XpThird;

        /// XP needed to go from level n to n+1 grows by XpLevelStep each level (200, 400, 600 ...).
        public static int XpForLevel(int level) => XpLevelStep * level * (level - 1) / 2;

        public static int PlayerLevel
        {
            get
            {
                int xp = Xp, level = 1;
                while (xp >= XpForLevel(level + 1)) level++;
                return level;
            }
        }

        /// 0..1 progress inside the current level.
        public static float LevelProgress
        {
            get
            {
                int level = PlayerLevel, from = XpForLevel(level), to = XpForLevel(level + 1);
                return (Xp - from) / (float)(to - from);
            }
        }

        public static string RankTitle(int level) =>
            level >= 30 ? "LEGEND" : level >= 20 ? "ELITE DRIVER" : level >= 10 ? "PRO DRIVER" : level >= 5 ? "AMATEUR DRIVER" : "ROOKIE DRIVER";

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
