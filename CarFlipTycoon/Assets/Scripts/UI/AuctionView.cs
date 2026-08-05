using System.Collections.Generic;
using CarFlipTycoon.Core;
using CarFlipTycoon.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CarFlipTycoon.UI
{
    /// <summary>
    /// Auktions-Tab: Autos aus der Garage zur Auktion anmelden (Startpreis = System-Vorschlag
    /// aus geschätztem Wert) sowie Live-Anzeige aller aktuell laufenden Auktionen mit
    /// Höchstgebot, Fortschritt/Countdown und Rewarded-Ad-Platzhaltern ("Sofort beenden ⚡"
    /// über <see cref="TimerProgressView"/>, "Bieter-Boost").
    /// </summary>
    public class AuctionView : MonoBehaviour
    {
        private RectTransform _content;

        public void Build(RectTransform panel)
        {
            UIFactory.CreateScrollList(panel, out _content);

            GameManager.Instance.OnGarageChanged += Rebuild;
            TimerManager.Instance.OnTimersChanged += Rebuild;
            AuctionManager.Instance.OnAuctionsChanged += Rebuild;

            Rebuild();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGarageChanged -= Rebuild;
            }

            if (TimerManager.Instance != null)
            {
                TimerManager.Instance.OnTimersChanged -= Rebuild;
            }

            if (AuctionManager.Instance != null)
            {
                AuctionManager.Instance.OnAuctionsChanged -= Rebuild;
            }
        }

        private void Rebuild()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.GetChild(i).gameObject);
            }

            var runningCars = new List<CarInstance>();
            var availableCars = new List<CarInstance>();

            var ownedCars = GameManager.Instance.OwnedCars;
            for (int i = 0; i < ownedCars.Count; i++)
            {
                var car = ownedCars[i];
                if (car.status == CarStatus.InAuction)
                {
                    runningCars.Add(car);
                }
                else if (car.status == CarStatus.InGarage)
                {
                    availableCars.Add(car);
                }
            }

            CreateSectionHeader("Laufende Auktionen");
            if (runningCars.Count == 0)
            {
                CreateEmptyRow("Keine laufenden Auktionen.");
            }
            else
            {
                for (int i = 0; i < runningCars.Count; i++)
                {
                    CreateRunningAuctionRow(runningCars[i]);
                }
            }

            CreateSectionHeader("Zur Auktion anmelden");
            if (availableCars.Count == 0)
            {
                CreateEmptyRow("Keine verfügbaren Autos in der Garage.");
            }
            else
            {
                for (int i = 0; i < availableCars.Count; i++)
                {
                    CreateAvailableCarRow(availableCars[i]);
                }
            }
        }

        private void CreateSectionHeader(string label)
        {
            var header = UIFactory.CreateText(_content, label, 28, new Color(0.85f, 0.85f, 0.9f));
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
        }

        private void CreateEmptyRow(string message)
        {
            var text = UIFactory.CreateText(_content, message, 20, new Color(0.6f, 0.6f, 0.65f));
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
        }

        private void CreateRunningAuctionRow(CarInstance car)
        {
            var auction = AuctionManager.Instance.GetAuction(car.instanceId);
            if (auction == null)
            {
                return;
            }

            var carType = GameManager.Instance.GetCarType(car.carTypeId);
            string modelName = carType != null ? carType.modelName : car.carTypeId;

            var row = UIFactory.CreatePanel(_content, "RunningAuction_" + car.instanceId, new Color(1f, 1f, 1f, 0.08f));
            var rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 300;
            rowLayoutElement.minHeight = 300;

            var rowLayout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset(18, 18, 12, 12);
            rowLayout.spacing = 4;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandHeight = false;

            var nameText = UIFactory.CreateText(row, modelName, 30, Color.white);
            nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

            string dynoInfo = car.lastDynoResult != null && car.lastDynoResult.hasResult
                ? $"Geprüfte Leistung: {car.lastDynoResult.horsePower:0} PS / {car.lastDynoResult.torqueNm:0} Nm"
                : "Keine Prüfstand-Messung vorhanden";
            var dynoText = UIFactory.CreateText(row, dynoInfo, 20, new Color(0.7f, 0.85f, 1f));
            dynoText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

            var bidText = UIFactory.CreateText(row,
                $"Aktuelles Höchstgebot: {auction.currentBid:N0} Coins ({auction.bidCount} Gebote)", 24,
                new Color(0.6f, 0.9f, 0.6f));
            bidText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;

            var timerHolder = UIFactory.CreateContainer(row, "TimerHolder");
            timerHolder.gameObject.AddComponent<LayoutElement>().preferredHeight = 66;
            var timerProgress = timerHolder.gameObject.AddComponent<TimerProgressView>();
            timerProgress.Build(timerHolder);
            timerProgress.SetCar(car.instanceId);

            var boostButton = UIFactory.CreateButton(row, "Bieter-Boost 🚀 (Ad)", new Color(0.6f, 0.3f, 0.6f), Color.white, 20);
            boostButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            string carId = car.instanceId;
            boostButton.onClick.AddListener(() => ApplyBidderBoost(carId));
        }

        private void ApplyBidderBoost(string carInstanceId)
        {
            AdManager.Instance.ShowRewardedAd(RewardedAdPurpose.AuctionBidderBoost, success =>
            {
                if (!success)
                {
                    return;
                }

                if (!AuctionManager.Instance.TryApplyBidderBoost(carInstanceId, out string error))
                {
                    Debug.LogWarning($"[AuctionView] Bieter-Boost fehlgeschlagen: {error}");
                }
            });
        }

        private void CreateAvailableCarRow(CarInstance car)
        {
            var carType = GameManager.Instance.GetCarType(car.carTypeId);
            string modelName = carType != null ? carType.modelName : car.carTypeId;
            int suggestedPrice = AuctionManager.Instance.SuggestStartPrice(car);

            var row = UIFactory.CreatePanel(_content, "Available_" + car.instanceId, new Color(1f, 1f, 1f, 0.06f));
            var rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 150;
            rowLayoutElement.minHeight = 150;

            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(18, 18, 10, 10);
            rowLayout.spacing = 14;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandHeight = true;

            var infoColumn = UIFactory.CreateContainer(row, "Info");
            infoColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var infoLayout = infoColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.childControlWidth = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandHeight = false;
            infoLayout.spacing = 3;

            var nameText = UIFactory.CreateText(infoColumn, modelName, 27, Color.white);
            nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;

            var priceText = UIFactory.CreateText(infoColumn, $"Vorgeschlagener Startpreis: {suggestedPrice:N0} Coins", 19,
                new Color(0.8f, 0.85f, 0.6f));
            priceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;

            var startButton = UIFactory.CreateButton(row, "Zur Auktion\nanmelden", new Color(0.6f, 0.3f, 0.6f), Color.white, 19);
            startButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
            string carId = car.instanceId;
            startButton.onClick.AddListener(() => StartAuction(carId));
        }

        private void StartAuction(string carInstanceId)
        {
            var car = GameManager.Instance.GetCarInstance(carInstanceId);
            if (car == null)
            {
                return;
            }

            if (!AuctionManager.Instance.TryStartAuction(car, null, out string error))
            {
                Debug.LogWarning($"[AuctionView] Auktionsstart fehlgeschlagen: {error}");
            }
        }
    }
}
