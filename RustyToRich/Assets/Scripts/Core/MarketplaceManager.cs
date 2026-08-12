using System;
using System.Collections.Generic;
using System.Globalization;
using RustyToRich.Data;
using RustyToRich.Utility;
using UnityEngine;

namespace RustyToRich.Core
{
    /// <summary>
    /// Verwaltet den Marktplatz: hält mehrere gleichzeitig kaufbare Auto-Angebote vor,
    /// erneuert einzelne Angebote nach Ablauf ihrer Standzeit und ersetzt gekaufte
    /// Angebote sofort durch ein neues, damit der Marktplatz nie leer wird.
    /// </summary>
    public class MarketplaceManager : MonoBehaviour
    {
        private const int OfferSlotCount = 5;
        private const float MinOfferLifetimeSeconds = 180f;
        private const float MaxOfferLifetimeSeconds = 420f;
        private const float ExpiryCheckIntervalSeconds = 5f;

        public static MarketplaceManager Instance { get; private set; }

        /// <summary>Ausgelöst, nachdem sich die Liste der aktuellen Angebote geändert hat.</summary>
        public event Action OnOffersChanged;

        public IReadOnlyList<MarketOffer> CurrentOffers => SaveManager.Instance.CurrentSave.marketOffers;

        private float _expiryCheckTimer;

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
            RefreshExpiredOffers();
        }

        private void Update()
        {
            _expiryCheckTimer += Time.unscaledDeltaTime;
            if (_expiryCheckTimer < ExpiryCheckIntervalSeconds)
            {
                return;
            }

            _expiryCheckTimer = 0f;
            RefreshExpiredOffers();
        }

        private void RefreshExpiredOffers()
        {
            var offers = SaveManager.Instance.CurrentSave.marketOffers;
            bool changed = false;

            for (int i = offers.Count - 1; i >= 0; i--)
            {
                if (IsExpired(offers[i]))
                {
                    offers.RemoveAt(i);
                    changed = true;
                }
            }

            while (offers.Count < OfferSlotCount)
            {
                var newOffer = GenerateOffer();
                if (newOffer == null)
                {
                    break;
                }

                offers.Add(newOffer);
                changed = true;
            }

            if (changed)
            {
                SaveManager.Instance.Save();
                OnOffersChanged?.Invoke();
            }
        }

        private static bool IsExpired(MarketOffer offer)
        {
            if (!DateTime.TryParse(offer.listedAtUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var listedAt))
            {
                return true;
            }

            double ageSeconds = (GameClock.Instance.UtcNow - listedAt.ToUniversalTime()).TotalSeconds;
            return ageSeconds >= offer.lifetimeSeconds;
        }

        private MarketOffer GenerateOffer()
        {
            var carTypes = GameManager.Instance.AllCarTypes;
            if (carTypes == null || carTypes.Count == 0)
            {
                return null;
            }

            // Nur bereits freigeschaltete Auto-Typen tauchen als Angebot auf (siehe ProgressionManager).
            var candidates = new List<CarType>();
            for (int i = 0; i < carTypes.Count; i++)
            {
                if (ProgressionManager.Instance == null || ProgressionManager.Instance.IsCarTypeUnlocked(carTypes[i]))
                {
                    candidates.Add(carTypes[i]);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var carType = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            var condition = (CarCondition)UnityEngine.Random.Range(0, Enum.GetValues(typeof(CarCondition)).Length);
            int basePrice = UnityEngine.Random.Range(carType.basePriceMin, carType.basePriceMax + 1);
            int price = Mathf.Max(1, Mathf.RoundToInt(basePrice * condition.GetPriceMultiplier()));

            return new MarketOffer
            {
                offerId = IdFactory.NewId(),
                carTypeId = carType.CarTypeId,
                condition = condition,
                price = price,
                listedAtUtc = IdFactory.NowUtcIso(),
                lifetimeSeconds = UnityEngine.Random.Range(MinOfferLifetimeSeconds, MaxOfferLifetimeSeconds)
            };
        }

        /// <summary>
        /// Kauft ein Angebot: prüft freien Garagenplatz und Guthaben, erzeugt die Auto-Instanz
        /// und ersetzt das Angebot sofort durch ein neues. Bei Fehlschlag steht der Grund in <paramref name="error"/>.
        /// </summary>
        public bool TryBuyOffer(string offerId, out string error)
        {
            var offers = SaveManager.Instance.CurrentSave.marketOffers;
            MarketOffer offer = null;
            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i].offerId == offerId)
                {
                    offer = offers[i];
                    break;
                }
            }

            if (offer == null)
            {
                error = "Angebot ist nicht mehr verfügbar.";
                return false;
            }

            if (!GarageManager.Instance.HasFreeSlot)
            {
                error = "Garage ist voll.";
                return false;
            }

            var carType = GameManager.Instance.GetCarType(offer.carTypeId);
            if (carType == null)
            {
                error = "Fahrzeugtyp nicht gefunden.";
                return false;
            }

            if (!EconomyManager.Instance.TrySpendCoins(offer.price))
            {
                error = "Nicht genug Coins.";
                return false;
            }

            GameManager.Instance.AddCarToGarage(carType, offer.price, offer.condition);

            offers.Remove(offer);
            var replacement = GenerateOffer();
            if (replacement != null)
            {
                offers.Add(replacement);
            }

            SaveManager.Instance.Save();
            OnOffersChanged?.Invoke();

            error = null;
            return true;
        }

        /// <summary>Rewarded-Ad-Platzhalter: ersetzt alle aktuellen Angebote sofort durch neue, unabhängig von ihrer Restlaufzeit.</summary>
        public void RefreshAllOffersNow()
        {
            var offers = SaveManager.Instance.CurrentSave.marketOffers;
            offers.Clear();

            while (offers.Count < OfferSlotCount)
            {
                var newOffer = GenerateOffer();
                if (newOffer == null)
                {
                    break;
                }

                offers.Add(newOffer);
            }

            SaveManager.Instance.Save();
            OnOffersChanged?.Invoke();
        }
    }
}
