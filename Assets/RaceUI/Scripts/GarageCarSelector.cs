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

        [Header("Stat bar reference maximums")]
        [SerializeField] private float maxSpeedReference = 200f;
        [SerializeField] private float enginePowerReference = 1500f;
        [SerializeField] private float steerReference = 45f;

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
        [SerializeField] private StatBar nitroBar;

        [Header("Flow")]
        [SerializeField] private UIDialog dialog;
        [SerializeField] private LevelSelector levelSelector;

        private int index;
        private GameObject spawnedCar;

        public int Index => index;
        public string CarDisplayName => CarName(index);

        private void Start()
        {
            index = Mathf.Clamp(GameSave.SelectedCar, 0, carPrefabs.Length - 1);
            ShowCar();
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
            EasyCarController car = carPrefabs[index] != null ? carPrefabs[index].GetComponent<EasyCarController>() : null;
            if (car == null) return;
            Nitro_Feature nitro = car.GetComponent<Nitro_Feature>();
            SetStat(speedBar, car.maxSpeed / maxSpeedReference);
            SetStat(accelerationBar, car.enginePower / enginePowerReference);
            SetStat(handlingBar, car.maxSteer / steerReference);
            SetStat(nitroBar, nitro == null || !nitro.enableNitro ? 0f : ((int)nitro.nitroBoost + 1) / 3f);
        }

        private static void SetStat(StatBar bar, float value)
        {
            if (bar != null) bar.SetValue(Mathf.Clamp01(value));
        }

        private string CarName(int i) => i < carNames.Length && !string.IsNullOrEmpty(carNames[i]) ? carNames[i] : "CAR " + (i + 1);
    }
}
