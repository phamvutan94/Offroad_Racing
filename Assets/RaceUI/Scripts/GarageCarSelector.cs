using ALIyerEdon;
using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// Car showroom: spawns the selected car prefab at the garage spawn point, handles buying (coins)
    /// and starting the race on the track chosen in the World Tour. Replaces the old CarSelect UI logic
    /// and keeps its PlayerPrefs keys ("CarID", "Car{i}", "TotalScores").
    /// </summary>
    public sealed class GarageCarSelector : MonoBehaviour
    {
        [Header("Cars")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject[] carPrefabs;
        [SerializeField] private int[] carPrices;
        [SerializeField] private string[] carNames;

        [Header("Performance bars (stats from the tuning config, upgrades included)")]
        [SerializeField] private TuningConfig tuning;
        [Tooltip("Stat value (0..10) shown as an empty / full bar; a narrow range makes the differences between cars visible")]
        [SerializeField] private float statEmpty = 3f;
        [SerializeField] private float statFull = 9f;

        [Header("UI")]
        [SerializeField] private Text[] nameLabels;
        [SerializeField] private Text[] statusLabels;
        [SerializeField] private Text counterLabel;
        [SerializeField] private Text priceLabel;
        [SerializeField] private GameObject priceGroup;
        [SerializeField] private GameObject buyButton;
        [SerializeField] private StatBar speedBar;
        [SerializeField] private StatBar accelerationBar;
        [SerializeField] private StatBar handlingBar;
        [UnityEngine.Serialization.FormerlySerializedAs("nitroBar")]
        [SerializeField] private StatBar offroadBar;

        [Header("Flow")]
        [SerializeField] private UIDialog dialog;
        [SerializeField] private LevelSelector levelSelector;

        private int index;
        private GameObject spawnedCar;
        private bool statsShown;

        public int Index => index;
        public string CarDisplayName => CarName(index);

        private void Start()
        {
            index = Mathf.Clamp(GameSave.SelectedCar, 0, carPrefabs.Length - 1);
            ShowCar();
        }

        private void OnEnable() => CarTuning.Changed += OnTuningChanged;
        private void OnDisable() => CarTuning.Changed -= OnTuningChanged;

        private void OnTuningChanged(int car)
        {
            if (car == index) RefreshStats();
        }

        public void Next() { index = (index + 1) % carPrefabs.Length; ShowCar(); }
        public void Previous() { index = (index + carPrefabs.Length - 1) % carPrefabs.Length; ShowCar(); }

        public void Buy()
        {
            if (GameSave.IsCarOwned(index)) return;
            int price = carPrices[index];
            dialog.Show("BUY " + CarName(index), "Unlock this car for " + price.ToString("N0") + " coins?", "BUY", () =>
            {
                if (GameSave.TrySpend(price)) { GameSave.SetCarOwned(index); GameSave.SelectedCar = index; Refresh(); }
                else dialog.ShowInfo("NOT ENOUGH COINS", "You need " + (price - GameSave.Coins).ToString("N0") + " more coins. Win races to earn more.");
            });
        }

        /// Starts the race with this car on the track selected in the World Tour.
        public void SelectAndRace()
        {
            if (!GameSave.IsCarOwned(index)) { Buy(); return; }
            GameSave.SelectedCar = index;
            SceneNavigation.Load(levelSelector.SelectedSceneName, levelSelector.SelectedThumbnail, levelSelector.SelectedTitle, levelSelector.SelectedTitleArt);
        }

        private void ShowCar()
        {
            if (spawnedCar != null) Destroy(spawnedCar);
            if (spawnPoint != null && carPrefabs[index] != null)
                spawnedCar = Instantiate(carPrefabs[index], spawnPoint.position, spawnPoint.rotation);
            if (GameSave.IsCarOwned(index)) GameSave.SelectedCar = index;
            Refresh();
        }

        private void Refresh()
        {
            bool owned = GameSave.IsCarOwned(index);
            string price = carPrices[index].ToString("N0") + " COINS";
            foreach (Text t in nameLabels) if (t != null) t.text = CarName(index);
            foreach (Text t in statusLabels) if (t != null) t.text = owned ? "OWNED" : price;
            if (counterLabel != null) counterLabel.text = (index + 1).ToString("00") + " / " + carPrefabs.Length.ToString("00");
            if (priceLabel != null) priceLabel.text = price;
            if (priceGroup != null) priceGroup.SetActive(!owned);
            if (buyButton != null) buyButton.SetActive(!owned);
            RefreshStats();
        }

        private void RefreshStats()
        {
            if (tuning == null) return;
            float[] stats = tuning.Stats(index, CarTuning.Levels(tuning, index));
            SetStat(speedBar, stats[(int)TuningStat.Speed]);
            SetStat(accelerationBar, stats[(int)TuningStat.Acceleration]);
            SetStat(handlingBar, stats[(int)TuningStat.Handling]);
            SetStat(offroadBar, stats[(int)TuningStat.Offroad]);
            statsShown = true;
        }

        private void SetStat(StatBar bar, float stat)
        {
            if (bar == null) return;
            float value = Mathf.Clamp01(Mathf.InverseLerp(statEmpty, statFull, stat));
            // first fill jumps; switching cars or upgrading slides the bar
            if (statsShown) bar.AnimateTo(value); else bar.SetValue(value);
        }

        private string CarName(int i) => i < carNames.Length && !string.IsNullOrEmpty(carNames[i]) ? carNames[i] : "CAR " + (i + 1);
    }
}
