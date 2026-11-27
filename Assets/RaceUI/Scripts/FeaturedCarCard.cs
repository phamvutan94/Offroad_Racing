using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// Garage "NEW CAR" card: shows a random car from the strongest cars the player does not own yet
    /// (by performance index); tapping it shows that car in the showroom. Hidden once every car is owned.
    /// </summary>
    public sealed class FeaturedCarCard : MonoBehaviour
    {
        [SerializeField] private GarageCarSelector cars;
        [Tooltip("Pick randomly among this many of the strongest cars not owned yet")]
        [SerializeField, Min(1)] private int candidates = 4;

        [Header("UI")]
        [Tooltip("Hides the card (alpha + raycasts) when every car is owned; the object stays active so it can come back")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image carArt;
        [Tooltip("Placeholder shown when the car has no title art")]
        [SerializeField] private GameObject silhouette;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text priceLabel;

        private int shown = -1;

        private void OnEnable()
        {
            if (cars != null) cars.Changed += Refresh;
        }

        private void OnDisable()
        {
            if (cars != null) cars.Changed -= Refresh;
        }

        // the selector fills its data in Start; refresh once it is ready
        private void Start() => Refresh();

        /// Button OnClick: show the featured car in the showroom.
        public void Open()
        {
            if (cars != null && shown >= 0) cars.ShowCarAt(shown);
        }

        private void Refresh()
        {
            if (cars == null) return;
            // keep the current pick until it is bought
            if (shown < 0 || shown >= cars.CarCount || GameSave.IsCarOwned(shown)) shown = Pick();
            SetVisible(shown >= 0);
            if (shown < 0) return;

            Sprite art = cars.TitleAt(shown);
            if (carArt != null) { carArt.sprite = art; carArt.enabled = art != null; }
            if (silhouette != null) silhouette.SetActive(art == null);
            if (nameLabel != null) nameLabel.text = cars.NameAt(shown);
            if (priceLabel != null) priceLabel.text = cars.PriceAt(shown).ToString("N0") + " COINS";
        }

        private int Pick()
        {
            var notOwned = new List<int>();
            for (int i = 0; i < cars.CarCount; i++)
                if (!GameSave.IsCarOwned(i)) notOwned.Add(i);
            if (notOwned.Count == 0) return -1;
            TuningConfig tuning = cars.Tuning;
            if (tuning != null)
                notOwned.Sort((a, b) => Power(tuning, b).CompareTo(Power(tuning, a)));
            return notOwned[Random.Range(0, Mathf.Min(candidates, notOwned.Count))];
        }

        private static int Power(TuningConfig tuning, int car) =>
            car < tuning.cars.Length ? tuning.PerformanceIndex(tuning.StockStats(car)) : 0;

        private void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private void Reset()
        {
            group = GetComponent<CanvasGroup>();
        }
    }
}
