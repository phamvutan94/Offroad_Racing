using System.Collections;
using ALIyerEdon;
using UnityEngine;
using UnityEngine.UI;

namespace RaceUI
{
    /// <summary>
    /// Finish screen. Race_Manager.Finish_Race activates it and Start_Finish_UI grants the award coins;
    /// this view then shows the final position, reward and standings.
    /// </summary>
    public sealed class RaceResultView : MonoBehaviour
    {
        [SerializeField] private RaceSceneLinks links;
        [SerializeField] private Start_Finish_UI startFinish;
        [SerializeField] private RaceHUD hud;

        [SerializeField] private Text positionText;
        [SerializeField] private Text captionText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Text[] rowPositions;
        [SerializeField] private Text[] rowNames;
        [SerializeField] private Text[] rowTimes;
        [SerializeField] private Image[] rowBackgrounds;
        [SerializeField] private Color playerRowColor = RaceUITheme.Accent;
        [SerializeField] private Color otherRowColor = RaceUITheme.Row;

        private void OnEnable()
        {
            if (hud != null) hud.MarkFinished();
            StartCoroutine(FillNextFrame());
        }

        private IEnumerator FillNextFrame()
        {
            yield return null; // Start_Finish_UI.Update_Award runs right after this screen is activated
            Race_Manager raceManager = links.RaceManager;
            Car_Position player = raceManager.PlayerPosition;
            if (player == null) yield break;

            int place = player.currentPosition;
            int reward = place == 0 ? startFinish.goldAward : place == 1 ? startFinish.bronzeAward : place == 2 ? startFinish.silverAward : 0;
            positionText.text = GameSave.Ordinal(place);
            captionText.text = place == 0 ? "TRAIL CONQUERED" : place < 3 ? "ON THE PODIUM" : "KEEP PUSHING";
            rewardText.text = reward > 0 ? "+" + reward.ToString("N0") + " COINS" : "NO REWARD";
            GameSave.NotifyCoinsChanged();

            for (int i = 0; i < rowNames.Length; i++)
            {
                string racer = raceManager.GetRacerNameAtPosition(i);
                bool used = racer.Length > 0;
                rowBackgrounds[i].gameObject.SetActive(used);
                if (!used) continue;
                bool isPlayer = racer == raceManager.PlayerName;
                rowPositions[i].text = (i + 1).ToString("00");
                rowNames[i].text = racer.ToUpperInvariant();
                rowTimes[i].text = isPlayer && hud != null ? RaceHUD.FormatTime(hud.RaceTime) : "RACING";
                rowBackgrounds[i].color = isPlayer ? playerRowColor : otherRowColor;
            }
        }
    }
}
