using System;
using System.Collections.Generic;
using System.Globalization;
using CarFlipTycoon.Data;
using CarFlipTycoon.SaveSystem;
using CarFlipTycoon.Utility;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>
    /// Auktions-System: meldet ein Auto zur Auktion an (Timer-System aus <see cref="TimerManager"/>
    /// steuert die Laufzeit), simuliert in unregelmäßigen Abständen neue, leicht höhere Gebote
    /// und schließt die Auktion beim Ablauf des Timers ab (Verkauf + Verkaufshistorie-Eintrag über
    /// <see cref="GameManager.SellCar"/>). Mehrere Auktionen für unterschiedliche Autos laufen
    /// unabhängig voneinander parallel.
    /// </summary>
    public class AuctionManager : MonoBehaviour
    {
        private const float BidCheckIntervalSeconds = 3f;
        private const float MinBidIntervalSeconds = 8f;
        private const float MaxBidIntervalSeconds = 20f;
        private const float MinBidIncrementFraction = 0.03f;
        private const float MaxBidIncrementFraction = 0.12f;
        private const float CeilingMultiplierMin = 1.15f;
        private const float CeilingMultiplierMax = 1.5f;
        private const float BoostBidIntervalSeconds = 4f;
        private const float BoostCeilingMultiplier = 1.15f;

        public static AuctionManager Instance { get; private set; }

        /// <summary>Ausgelöst, wenn sich Gebote/Auktionen geändert haben (neues Gebot, Start, Abschluss).</summary>
        public event Action OnAuctionsChanged;

        /// <summary>Ausgelöst, sobald eine Auktion abgeschlossen (verkauft) wurde.</summary>
        public event Action<AuctionData> OnAuctionSettled;

        private float _tickAccumulator;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            TimerManager.Instance.OnTimerCompleted += HandleTimerCompleted;
        }

        private void OnDestroy()
        {
            if (TimerManager.Instance != null)
            {
                TimerManager.Instance.OnTimerCompleted -= HandleTimerCompleted;
            }
        }

        private void Update()
        {
            _tickAccumulator += Time.unscaledDeltaTime;
            if (_tickAccumulator < BidCheckIntervalSeconds)
            {
                return;
            }

            _tickAccumulator = 0f;

            var auctions = SaveManager.Instance.CurrentSave.activeAuctions;
            bool changed = false;

            for (int i = 0; i < auctions.Count; i++)
            {
                if (auctions[i].status == AuctionStatus.Running && TryGenerateDueBid(auctions[i]))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                SaveManager.Instance.Save();
                OnAuctionsChanged?.Invoke();
            }
        }

        public IReadOnlyList<AuctionData> ActiveAuctions => SaveManager.Instance.CurrentSave.activeAuctions;

        public AuctionData GetAuction(string carInstanceId)
        {
            var auctions = SaveManager.Instance.CurrentSave.activeAuctions;
            for (int i = 0; i < auctions.Count; i++)
            {
                if (auctions[i].carInstanceId == carInstanceId)
                {
                    return auctions[i];
                }
            }

            return null;
        }

        /// <summary>Vorgeschlagener Startpreis: geschätzter Wert inkl. Optik-/Stil-/Prüfstand-Bonus (siehe CosmeticTuningManager).</summary>
        public int SuggestStartPrice(CarInstance car) => CosmeticTuningManager.Instance.GetEstimatedValue(car);

        /// <summary>Meldet ein Auto zur Auktion an. Ohne expliziten Startpreis wird <see cref="SuggestStartPrice"/> verwendet.</summary>
        public bool TryStartAuction(CarInstance car, int? startPrice, out string error)
        {
            if (car == null)
            {
                error = "Ungültige Anfrage.";
                return false;
            }

            if (TimerManager.Instance.HasActiveTimer(car.instanceId))
            {
                error = "Dieses Auto ist aktuell beschäftigt und nicht verfügbar.";
                return false;
            }

            var carType = GameManager.Instance.GetCarType(car.carTypeId);
            int suggestedValue = SuggestStartPrice(car);
            int price = Mathf.Max(1, startPrice ?? suggestedValue);

            var (minSeconds, maxSeconds) = DurationRangeForClass(carType != null ? carType.carClass : CarClass.Kompaktklasse);
            float duration = UnityEngine.Random.Range(minSeconds, maxSeconds);

            var auction = new AuctionData
            {
                id = IdFactory.NewId(),
                carInstanceId = car.instanceId,
                startPrice = price,
                currentBid = price,
                bidCount = 0,
                maxBidCeiling = Mathf.Max(price, Mathf.RoundToInt(suggestedValue * UnityEngine.Random.Range(CeilingMultiplierMin, CeilingMultiplierMax))),
                startTimeUtc = IdFactory.NowUtcIso(),
                endTimeUtc = DateTime.UtcNow.AddSeconds(duration).ToString("o", CultureInfo.InvariantCulture),
                lastBidTimeUtc = IdFactory.NowUtcIso(),
                nextBidIntervalSeconds = UnityEngine.Random.Range(MinBidIntervalSeconds, MaxBidIntervalSeconds),
                status = AuctionStatus.Running
            };

            if (!TimerManager.Instance.TryStartTimer(car.instanceId, TimerType.Auction, duration,
                    TimerPayloadKind.None, auction.id, out error))
            {
                return false;
            }

            SaveManager.Instance.CurrentSave.activeAuctions.Add(auction);
            SaveManager.Instance.Save();
            OnAuctionsChanged?.Invoke();

            error = null;
            return true;
        }

        /// <summary>Rewarded-Ad-Platzhalter: beendet die laufende Auktion für dieses Auto sofort (aktuelles Gebot wird final abgerechnet).</summary>
        public bool TryFinishNow(string carInstanceId, out string error)
        {
            var timer = TimerManager.Instance.GetActiveTimer(carInstanceId);
            if (timer == null || timer.timerType != TimerType.Auction)
            {
                error = "Keine laufende Auktion für dieses Auto.";
                return false;
            }

            TimerManager.Instance.FinishNow(carInstanceId);
            error = null;
            return true;
        }

        /// <summary>
        /// Rewarded-Ad-Platzhalter: sofortiger Gebotsschub (ein zusätzliches Gebot, höhere
        /// Obergrenze) und ein kürzeres Intervall bis zum nächsten simulierten Gebot.
        /// </summary>
        public bool TryApplyBidderBoost(string carInstanceId, out string error)
        {
            var auction = GetAuction(carInstanceId);
            if (auction == null || auction.status != AuctionStatus.Running)
            {
                error = "Keine laufende Auktion für dieses Auto.";
                return false;
            }

            auction.maxBidCeiling = Mathf.RoundToInt(auction.maxBidCeiling * BoostCeilingMultiplier);
            GenerateBid(auction, MaxBidIncrementFraction * 1.5f);
            auction.nextBidIntervalSeconds = BoostBidIntervalSeconds;

            SaveManager.Instance.Save();
            OnAuctionsChanged?.Invoke();

            error = null;
            return true;
        }

        private bool TryGenerateDueBid(AuctionData auction)
        {
            if (!DateTime.TryParse(auction.lastBidTimeUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var lastBidAt))
            {
                return false;
            }

            double elapsedSeconds = (DateTime.UtcNow - lastBidAt.ToUniversalTime()).TotalSeconds;
            if (elapsedSeconds < auction.nextBidIntervalSeconds)
            {
                return false;
            }

            // Hinweis: Nach längerer App-Abwesenheit wird bewusst nur EIN Gebot nachgeholt
            // (statt alle verpassten Intervalle rückwirkend zu simulieren), um unrealistische
            // Gebotsschübe beim Wiedereinstieg zu vermeiden. Das Intervall läuft ab jetzt neu.
            bool placedBid = GenerateBid(auction, MaxBidIncrementFraction);
            auction.lastBidTimeUtc = IdFactory.NowUtcIso();
            auction.nextBidIntervalSeconds = UnityEngine.Random.Range(MinBidIntervalSeconds, MaxBidIntervalSeconds);
            return placedBid;
        }

        private static bool GenerateBid(AuctionData auction, float maxIncrementFraction)
        {
            if (auction.currentBid >= auction.maxBidCeiling)
            {
                return false;
            }

            float incrementFraction = UnityEngine.Random.Range(MinBidIncrementFraction, maxIncrementFraction);
            int newBid = auction.currentBid + Mathf.Max(1, Mathf.RoundToInt(auction.currentBid * incrementFraction));
            auction.currentBid = Mathf.Min(newBid, auction.maxBidCeiling);
            auction.bidCount++;
            return true;
        }

        private void HandleTimerCompleted(TimerData timer)
        {
            if (timer.timerType != TimerType.Auction)
            {
                return;
            }

            var auctions = SaveManager.Instance.CurrentSave.activeAuctions;
            AuctionData auction = null;
            for (int i = 0; i < auctions.Count; i++)
            {
                if (auctions[i].id == timer.payloadId)
                {
                    auction = auctions[i];
                    break;
                }
            }

            if (auction == null)
            {
                return;
            }

            SettleAuction(auction);
        }

        private void SettleAuction(AuctionData auction)
        {
            auction.status = AuctionStatus.Ended;
            SaveManager.Instance.CurrentSave.activeAuctions.Remove(auction);

            GameManager.Instance.SellCar(auction.carInstanceId, auction.currentBid, SaleType.Auction);

            SaveManager.Instance.Save();
            OnAuctionsChanged?.Invoke();
            OnAuctionSettled?.Invoke(auction);
        }

        /// <summary>Auktionsdauer-Spanne abhängig von der Fahrzeugklasse (grob 2-20 Minuten).</summary>
        private static (float minSeconds, float maxSeconds) DurationRangeForClass(CarClass carClass)
        {
            switch (carClass)
            {
                case CarClass.Kleinwagen:
                case CarClass.Kompaktklasse:
                case CarClass.Kombi:
                case CarClass.Oldtimer:
                    return (120f, 300f);
                case CarClass.Sportwagen:
                case CarClass.Muscle:
                case CarClass.Oberklasse:
                    return (720f, 1200f);
                case CarClass.Mittelklasse:
                case CarClass.SportLimousine:
                case CarClass.OffroadPickup:
                case CarClass.SUV:
                case CarClass.Coupe:
                default:
                    return (300f, 720f);
            }
        }
    }
}
