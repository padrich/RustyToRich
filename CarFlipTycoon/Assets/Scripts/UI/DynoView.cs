using CarFlipTycoon.Core;
using CarFlipTycoon.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CarFlipTycoon.UI
{
    /// <summary>
    /// Prüfstand-Tab der Auto-Detailseite: "Zum Prüfstand schicken", Fortschrittsanzeige während
    /// der Messung, Liniendiagramm (PS/Nm über Drehzahl) mit Spitzenwerten, sowie ein optionaler
    /// Vorher/Nachher-Vergleich mit dem vorherigen Prüfstand-Ergebnis nach weiterem Tuning.
    /// </summary>
    public class DynoView : MonoBehaviour
    {
        private string _carInstanceId;

        private Button _sendButton;
        private Text _sendButtonText;
        private TimerProgressView _timerProgress;
        private Text _peakText;
        private Text _compareToggleLabel;
        private DynoChartView _chart;
        private Text _emptyStateText;

        private bool _showComparison = true;

        public void Build(RectTransform panel)
        {
            var rootLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(16, 16, 12, 12);
            rootLayout.spacing = 10;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandHeight = true;

            _sendButton = UIFactory.CreateButton(panel, "Zum Prüfstand schicken", new Color(0.2f, 0.5f, 0.7f), Color.white, 26);
            _sendButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;
            _sendButtonText = _sendButton.GetComponentInChildren<Text>();
            _sendButton.onClick.AddListener(SendToDyno);

            var timerHolder = UIFactory.CreateContainer(panel, "TimerHolder");
            timerHolder.gameObject.AddComponent<LayoutElement>().preferredHeight = 66;
            _timerProgress = timerHolder.gameObject.AddComponent<TimerProgressView>();
            _timerProgress.Build(timerHolder);

            _peakText = UIFactory.CreateText(panel, string.Empty, 22, Color.white);
            _peakText.gameObject.AddComponent<LayoutElement>().preferredHeight = 62;

            var compareButton = UIFactory.CreateButton(panel, "Vorher/Nachher: An", new Color(0.25f, 0.3f, 0.4f), Color.white, 20);
            compareButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            _compareToggleLabel = compareButton.GetComponentInChildren<Text>();
            compareButton.onClick.AddListener(ToggleComparison);

            var chartHolder = UIFactory.CreateContainer(panel, "ChartHolder");
            chartHolder.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            _chart = chartHolder.gameObject.AddComponent<DynoChartView>();
            _chart.Build(chartHolder);

            _emptyStateText = UIFactory.CreateText(panel, "Noch keine Prüfstand-Messung für dieses Auto.", 22,
                new Color(0.75f, 0.75f, 0.8f), TextAnchor.MiddleCenter);
            _emptyStateText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

            DynoManager.Instance.OnDynoResultChanged += HandleDynoResultChanged;
        }

        private void OnDestroy()
        {
            if (DynoManager.Instance != null)
            {
                DynoManager.Instance.OnDynoResultChanged -= HandleDynoResultChanged;
            }
        }

        public void SetCar(string carInstanceId)
        {
            _carInstanceId = carInstanceId;
            _timerProgress.SetCar(carInstanceId);
            RefreshAll();
        }

        private void HandleDynoResultChanged(string carInstanceId)
        {
            if (carInstanceId == _carInstanceId)
            {
                RefreshAll();
            }
        }

        private CarInstance GetCar() => _carInstanceId != null ? GameManager.Instance.GetCarInstance(_carInstanceId) : null;

        private void RefreshAll()
        {
            var car = GetCar();
            bool busy = car != null && TimerManager.Instance.HasActiveTimer(car.instanceId);
            _sendButton.interactable = car != null && !busy;
            _sendButtonText.text = busy ? "Auto ist beschäftigt" : "Zum Prüfstand schicken";

            RefreshPeakText(car);
            RefreshChart();
        }

        private void RefreshPeakText(CarInstance car)
        {
            if (car == null || car.lastDynoResult == null || !car.lastDynoResult.hasResult)
            {
                _peakText.text = string.Empty;
                return;
            }

            var result = car.lastDynoResult;
            _peakText.text = $"Spitzen-PS: {result.horsePower:0} bei {result.horsePowerRpm} U/min\n" +
                              $"Spitzen-Nm: {result.torqueNm:0} bei {result.torqueRpm} U/min";
        }

        private void ToggleComparison()
        {
            _showComparison = !_showComparison;
            _compareToggleLabel.text = _showComparison ? "Vorher/Nachher: An" : "Vorher/Nachher: Aus";
            RefreshChart();
        }

        private void RefreshChart()
        {
            var car = GetCar();
            bool hasResult = car != null && car.lastDynoResult != null && car.lastDynoResult.hasResult;
            _emptyStateText.gameObject.SetActive(!hasResult);

            if (!hasResult)
            {
                _chart.SetCurves(null, null);
                return;
            }

            bool hasPrevious = _showComparison && car.previousDynoResult != null && car.previousDynoResult.hasResult;
            _chart.SetCurves(car.lastDynoResult.curvePoints, hasPrevious ? car.previousDynoResult.curvePoints : null);
        }

        private void SendToDyno()
        {
            var car = GetCar();
            if (car == null)
            {
                return;
            }

            if (!DynoManager.Instance.TrySendToDyno(car, out string error))
            {
                Debug.LogWarning($"[DynoView] Prüfstand fehlgeschlagen: {error}");
                return;
            }

            RefreshAll();
        }
    }
}
