using RustyToRich.Core;
using RustyToRich.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace RustyToRich.UI
{
    /// <summary>
    /// Wiederverwendbare Anzeige für einen laufenden Timer (Tuning-Einbau, Prüfstand, Auktion):
    /// Fortschrittsbalken, Countdown-Text und ein "Sofort fertig ⚡"-Button, der einen Rewarded
    /// Ad über <see cref="AdManager"/> anbietet. Blendet den Inhalt aus, sobald für das gesetzte
    /// Auto kein Timer mehr aktiv ist – das eigene GameObject bleibt dabei aktiv, damit
    /// <see cref="Update"/> weiterläuft und einen später neu gestarteten Timer erkennt.
    /// </summary>
    public class TimerProgressView : MonoBehaviour
    {
        private RectTransform _barRow;
        private RectTransform _fill;
        private Text _label;
        private Button _skipButton;
        private string _carInstanceId;
        private float _pollAccumulator;

        public void Build(RectTransform panel)
        {
            var rootLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(0, 0, 4, 4);
            rootLayout.spacing = 6;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandHeight = false;

            var barRow = UIFactory.CreateContainer(panel, "BarRow");
            _barRow = barRow;
            barRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            var barRowLayout = barRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            barRowLayout.spacing = 10;
            barRowLayout.childAlignment = TextAnchor.MiddleLeft;
            barRowLayout.childControlWidth = true;
            barRowLayout.childForceExpandWidth = false;
            barRowLayout.childControlHeight = true;
            barRowLayout.childForceExpandHeight = true;

            var barHolder = UIFactory.CreateContainer(barRow, "BarHolder");
            barHolder.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var barBackground = UIFactory.CreatePanel(barHolder, "BarBackground", new Color(0f, 0f, 0f, 0.35f));
            UIFactory.StretchFull(barBackground);

            var fillHolder = UIFactory.CreateContainer(barBackground, "FillHolder");
            fillHolder.anchorMin = Vector2.zero;
            fillHolder.anchorMax = new Vector2(0f, 1f);
            fillHolder.offsetMin = Vector2.zero;
            fillHolder.offsetMax = Vector2.zero;
            _fill = fillHolder;

            var fillImage = UIFactory.CreatePanel(fillHolder, "Fill", new Color(0.3f, 0.65f, 0.9f));
            UIFactory.StretchFull(fillImage);

            _label = UIFactory.CreateText(barBackground, string.Empty, 20, Color.white, TextAnchor.MiddleCenter);
            UIFactory.StretchFull(_label.rectTransform);

            _skipButton = UIFactory.CreateButton(barRow, "Sofort\nfertig ⚡", new Color(0.75f, 0.55f, 0.15f), Color.white, 18);
            _skipButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 140;
            _skipButton.onClick.AddListener(OnSkipClicked);

            _barRow.gameObject.SetActive(false);
        }

        public void SetCar(string carInstanceId)
        {
            _carInstanceId = carInstanceId;
            Refresh();
        }

        private void Update()
        {
            if (_carInstanceId == null)
            {
                return;
            }

            _pollAccumulator += Time.unscaledDeltaTime;
            if (_pollAccumulator < 0.25f)
            {
                return;
            }

            _pollAccumulator = 0f;
            Refresh();
        }

        private void Refresh()
        {
            var timer = _carInstanceId != null ? TimerManager.Instance.GetActiveTimer(_carInstanceId) : null;
            _barRow.gameObject.SetActive(timer != null);
            if (timer == null)
            {
                return;
            }

            float progress = TimerManager.Instance.GetProgress01(timer);
            float remaining = TimerManager.Instance.GetRemainingSeconds(timer);
            _fill.anchorMax = new Vector2(progress, 1f);
            _label.text = $"{TimerLabel(timer.timerType)}: noch {Mathf.CeilToInt(remaining)}s";
        }

        private void OnSkipClicked()
        {
            if (_carInstanceId == null)
            {
                return;
            }

            var timer = TimerManager.Instance.GetActiveTimer(_carInstanceId);
            if (timer == null)
            {
                return;
            }

            string carId = _carInstanceId;
            var purpose = PurposeForTimerType(timer.timerType);
            _skipButton.interactable = false;

            AdManager.Instance.ShowRewardedAd(purpose, success =>
            {
                _skipButton.interactable = true;
                if (success)
                {
                    TimerManager.Instance.FinishNow(carId);
                }
            });
        }

        private static string TimerLabel(TimerType timerType)
        {
            switch (timerType)
            {
                case TimerType.Tuning: return "Wird eingebaut";
                case TimerType.Dyno: return "Auf dem Prüfstand";
                case TimerType.Auction: return "Auktion läuft";
                case TimerType.Repair: return "Wird repariert";
                default: return "Läuft";
            }
        }

        private static RewardedAdPurpose PurposeForTimerType(TimerType timerType)
        {
            switch (timerType)
            {
                case TimerType.Dyno: return RewardedAdPurpose.SkipDynoTest;
                case TimerType.Auction: return RewardedAdPurpose.FinishAuctionNow;
                case TimerType.Tuning:
                case TimerType.Repair:
                default: return RewardedAdPurpose.SkipTuningInstall;
            }
        }
    }
}
