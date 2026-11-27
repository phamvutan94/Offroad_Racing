using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// Performance tuning: six part categories, each with N upgrade levels bought with coins.
    /// Shows the car card (class + performance index), the stat radar and the level path of the selected part.
    /// </summary>
    public sealed class TuningScreen : MonoBehaviour
    {
        [SerializeField] private TuningConfig config;
        [SerializeField] private GarageCarSelector cars;

        [Header("Car card")]
        [SerializeField] private Image carThumb;
        [SerializeField] private Text carName;
        [SerializeField] private Text carSubtitle;
        [SerializeField] private Text classLetter;
        [SerializeField] private Image classBadge;
        [SerializeField] private Text performanceIndex;
        [SerializeField] private Color[] classColors = { RaceUITheme.ClassC, RaceUITheme.ClassB, RaceUITheme.ClassA, RaceUITheme.Gold };

        [Header("Stats")]
        [SerializeField] private RadarChart radar;
        [SerializeField] private RadarChart radarPreview; // stats after the next upgrade
        [SerializeField] private Text[] statValues;      // Acceleration, Speed, Handling, Braking, Drifting, Offroad

        [Header("Part")]
        [SerializeField] private Text partTitle;
        [SerializeField] private Text partLevel;
        [SerializeField] private Image[] levelNodes;      // one per level
        [SerializeField] private Image[] levelLinks;      // between nodes (count = levels - 1)
        [SerializeField] private Image[] tierImages;      // 3 milestones
        [SerializeField] private Image[] tierLeaders;     // line from the milestone art down to its node
        [SerializeField] private RectTransform upgradeButton;
        [SerializeField] private Text upgradePrice;
        [SerializeField] private GameObject upgradePriceGroup;
        [SerializeField] private GameObject maxedLabel;
        [SerializeField] private Color ownedColor = RaceUITheme.Green;
        [SerializeField] private Color lockedColor = new Color(0.36f, 0.38f, 0.42f, 1f);
        [SerializeField] private Color ownedLinkColor = RaceUITheme.Green;
        [SerializeField] private Color lockedLinkColor = new Color(0.55f, 0.57f, 0.6f, 1f);
        [SerializeField] private Color tierLockedTint = new Color(1f, 1f, 1f, 0.45f);

        [Header("Tabs")]
        [SerializeField] private Image[] tabImages;
        [SerializeField] private Text[] tabLabels;
        [SerializeField] private GameObject[] tabSelected;

        [Header("Popups")]
        [SerializeField] private UIPopup popup;
        [SerializeField] private UIDialog confirm;

        private int partIndex;

        private int Car => cars != null ? cars.Index : GameSave.SelectedCar;

        private void OnEnable()
        {
            for (int i = 0; i < config.parts.Length && i < tabImages.Length; i++)
            {
                var spr = config.parts[i].tierSprites;
                tabImages[i].sprite = spr != null && spr.Length > 0 ? spr[spr.Length - 1] : null;
                tabImages[i].enabled = tabImages[i].sprite != null;
                tabLabels[i].text = config.parts[i].displayName;
            }
            Refresh();
        }

        /// Called by each category tab (persistent int listener).
        public void SelectPart(int index)
        {
            partIndex = Mathf.Clamp(index, 0, config.parts.Length - 1);
            Refresh();
        }

        /// The "+" node.
        public void Upgrade()
        {
            int car = Car;
            if (!GameSave.IsCarOwned(car))
            {
                popup.Show(null, RaceUITheme.Accent, "CAR LOCKED", "Buy this car in the garage before tuning it.", null, "OK", null, null, null);
                return;
            }
            var part = config.parts[partIndex];
            int level = CarTuning.GetLevel(car, part.id);
            if (level >= config.MaxLevel) return;
            int price = config.PriceForNextLevel(level);
            if (GameSave.Coins < price) { popup.ShowNotEnoughCoins(price - GameSave.Coins); return; }

            confirm.Show("UPGRADE " + part.displayName,
                "Level " + (level + 1) + " for " + price.ToString("N0") + " coins?", "UPGRADE", () =>
                {
                    var before = config.Stats(car, CarTuning.Levels(config, car));
                    var result = CarTuning.TryUpgrade(config, car, partIndex, out int cost);
                    if (result == CarTuning.UpgradeResult.NotEnoughCoins) { popup.ShowNotEnoughCoins(cost - GameSave.Coins); return; }
                    if (result != CarTuning.UpgradeResult.Upgraded) return;
                    var after = config.Stats(car, CarTuning.Levels(config, car));
                    Refresh();
                    popup.ShowUpgradeSuccess(TierSprite(part, level + 1), "UPGRADE COMPLETE",
                        part.displayName + "  LEVEL " + (level + 1) + "\n" + Gains(before, after));
                });
        }

        private static readonly string[] StatNames = { "ACCELERATION", "SPEED", "HANDLING", "BRAKING", "DRIFTING", "OFFROAD" };

        private static string Gains(float[] before, float[] after)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < before.Length; i++)
            {
                float d = after[i] - before[i];
                if (d > 0.04f) { if (sb.Length > 0) sb.Append("   "); sb.Append(StatNames[i]).Append(" +").Append(d.ToString("0.0#")); }
            }
            return sb.ToString();
        }

        private Sprite TierSprite(TuningConfig.Part part, int level)
        {
            if (part.tierSprites == null || part.tierSprites.Length == 0) return null;
            int tier = Mathf.Clamp(Mathf.CeilToInt(level / (float)config.MaxLevel * part.tierSprites.Length) - 1, 0, part.tierSprites.Length - 1);
            return part.tierSprites[tier];
        }

        private void Refresh()
        {
            if (config == null || config.parts.Length == 0) return;
            int car = Car;
            var levels = CarTuning.Levels(config, car);
            var stats = config.Stats(car, levels);
            int pi = config.PerformanceIndex(stats);
            string cls = config.ClassLetter(pi);

            carName.text = cars != null ? cars.CarDisplayName : "CAR";
            if (carThumb != null && cars != null && cars.CarTitle != null) carThumb.sprite = cars.CarTitle;
            carSubtitle.text = GameSave.IsCarOwned(car) ? "STOCK" : "NOT OWNED";
            int tuned = 0; foreach (var l in levels) tuned += l;
            if (GameSave.IsCarOwned(car) && tuned > 0) carSubtitle.text = "TUNED  /  " + tuned + (tuned == 1 ? " UPGRADE" : " UPGRADES");
            classLetter.text = cls;
            classBadge.color = classColors[Mathf.Clamp("CBAS".IndexOf(cls), 0, classColors.Length - 1)];
            performanceIndex.text = pi.ToString();

            radar.SetValues(stats);
            for (int i = 0; i < statValues.Length && i < stats.Length; i++) statValues[i].text = stats[i].ToString("0.0");

            var part = config.parts[partIndex];
            int level = levels[partIndex];
            partTitle.text = part.displayName;
            partLevel.text = "LEVEL " + level;

            if (radarPreview != null)
            {
                bool canPreview = level < config.MaxLevel;
                radarPreview.gameObject.SetActive(canPreview);
                if (canPreview) { var next = (int[])levels.Clone(); next[partIndex]++; radarPreview.SetValues(config.Stats(car, next)); }
            }

            for (int i = 0; i < levelNodes.Length; i++)
            {
                levelNodes[i].color = i < level ? ownedColor : lockedColor;
                levelNodes[i].gameObject.SetActive(i != level); // the upgrade button sits on the next node
            }
            for (int i = 0; i < levelLinks.Length; i++) levelLinks[i].color = i < level ? ownedLinkColor : lockedLinkColor;

            bool maxed = level >= config.MaxLevel;
            upgradeButton.gameObject.SetActive(!maxed);
            if (!maxed) upgradeButton.position = levelNodes[level].rectTransform.position;
            upgradePriceGroup.SetActive(!maxed);
            if (!maxed) upgradePrice.text = config.PriceForNextLevel(level).ToString("N0");
            maxedLabel.SetActive(maxed);

            int[] milestoneLevel = { 1, Mathf.CeilToInt(config.MaxLevel / 2f), config.MaxLevel };
            for (int t = 0; t < tierImages.Length; t++)
            {
                bool has = part.tierSprites != null && t < part.tierSprites.Length && part.tierSprites[t] != null;
                tierImages[t].sprite = has ? part.tierSprites[t] : null;
                tierImages[t].enabled = has;
                bool unlocked = level >= milestoneLevel[t];
                tierImages[t].color = unlocked ? Color.white : tierLockedTint;
                if (t < tierLeaders.Length) tierLeaders[t].color = unlocked ? ownedLinkColor : lockedLinkColor;
            }

            for (int i = 0; i < tabSelected.Length; i++) tabSelected[i].SetActive(i == partIndex);
        }
    }
}
