using System;
using System.Collections.Generic;
using RustyToRich.Data;
using RustyToRich.SaveSystem;
using RustyToRich.Utility;
using UnityEngine;

namespace RustyToRich.Core
{
    /// <summary>
    /// Verwaltet Optik-Tuning: verfügbare Teile je Kategorie/Stil, Kauf &amp; Einbau
    /// (über <see cref="TimerManager"/>) sowie die Stil-Konsistenz- und Wertschätzung
    /// einer Auto-Instanz. Pro Kategorie ist immer höchstens ein Teil verbaut – ein neuer
    /// Kauf ersetzt das bisherige Teil dieser Kategorie.
    /// </summary>
    public class CosmeticTuningManager : MonoBehaviour
    {
        private const string DatabaseResourcePath = "CosmeticPartDatabase";

        // Unterhalb dieser Konsistenz (Anteil der Teile mit dem dominanten Stil) gibt es keinen Bonus.
        private const float ConsistencyBonusThreshold = 0.5f;
        // Bonus bei 100% Stil-Konsistenz, z. B. 0.25 = +25% auf den geschätzten Wert.
        private const float MaxConsistencyBonus = 0.25f;

        public static CosmeticTuningManager Instance { get; private set; }

        /// <summary>Ausgelöst, nachdem ein Optik-Teil für die angegebene Auto-Instanz verbaut wurde.</summary>
        public event Action<string> OnCosmeticPartsChanged;

        private CosmeticPartDatabase _database;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _database = Resources.Load<CosmeticPartDatabase>(DatabaseResourcePath);
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

        public CosmeticPart GetPart(string partId) => _database != null ? _database.GetById(partId) : null;

        public List<CosmeticPart> GetPartsForCategory(CosmeticPartCategory category, StyleTag? styleFilter)
        {
            var all = _database != null ? _database.GetByCategory(category) : new List<CosmeticPart>();
            if (styleFilter == null)
            {
                return all;
            }

            var filtered = new List<CosmeticPart>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].HasStyle(styleFilter.Value))
                {
                    filtered.Add(all[i]);
                }
            }

            return filtered;
        }

        public InstalledCosmeticPart GetInstalledSlot(CarInstance car, CosmeticPartCategory category)
        {
            if (car == null || car.cosmeticParts == null)
            {
                return null;
            }

            for (int i = 0; i < car.cosmeticParts.Count; i++)
            {
                if (car.cosmeticParts[i].category == category)
                {
                    return car.cosmeticParts[i];
                }
            }

            return null;
        }

        public CosmeticPart GetInstalledPart(CarInstance car, CosmeticPartCategory category)
        {
            var slot = GetInstalledSlot(car, category);
            return slot != null ? GetPart(slot.partId) : null;
        }

        /// <summary>Kauft ein Optik-Teil für ein Auto und startet den Einbau-Timer. Blockiert, falls das Auto bereits in der Werkstatt ist.</summary>
        public bool TryPurchaseAndInstall(CarInstance car, CosmeticPart part, out string error)
        {
            if (car == null || part == null)
            {
                error = "Ungültige Anfrage.";
                return false;
            }

            if (TimerManager.Instance.HasActiveTimer(car.instanceId))
            {
                error = "Dieses Auto ist bereits in der Werkstatt beschäftigt.";
                return false;
            }

            if (!EconomyManager.Instance.TrySpendCoins(part.price))
            {
                error = "Nicht genug Coins.";
                return false;
            }

            if (!TimerManager.Instance.TryStartTimer(car.instanceId, TimerType.Tuning, part.installDurationSeconds,
                    TimerPayloadKind.InstallCosmeticPart, part.PartId, out error))
            {
                EconomyManager.Instance.AddCoins(part.price);
                return false;
            }

            error = null;
            return true;
        }

        private void HandleTimerCompleted(TimerData timer)
        {
            if (timer.payloadKind != TimerPayloadKind.InstallCosmeticPart)
            {
                return;
            }

            var part = GetPart(timer.payloadId);
            if (part == null)
            {
                return;
            }

            var car = GameManager.Instance.GetCarInstance(timer.carInstanceId);
            if (car == null)
            {
                return;
            }

            for (int i = car.cosmeticParts.Count - 1; i >= 0; i--)
            {
                if (car.cosmeticParts[i].category == part.category)
                {
                    car.cosmeticParts.RemoveAt(i);
                }
            }

            car.cosmeticParts.Add(new InstalledCosmeticPart
            {
                category = part.category,
                partId = part.PartId,
                partName = part.partName,
                purchasePrice = part.price,
                installedDateUtc = IdFactory.NowUtcIso()
            });

            SaveManager.Instance.Save();
            OnCosmeticPartsChanged?.Invoke(car.instanceId);
        }

        /// <summary>Anteil der installierten Teile, die dem dominanten Stil zugeordnet sind (0, falls keine Teile installiert).</summary>
        public float GetStyleConsistency(CarInstance car, out StyleTag dominantStyle)
        {
            dominantStyle = StyleTag.Street;
            if (car == null || car.cosmeticParts == null || car.cosmeticParts.Count == 0)
            {
                return 0f;
            }

            var counts = new int[Enum.GetValues(typeof(StyleTag)).Length];
            int totalTaggedParts = 0;

            for (int i = 0; i < car.cosmeticParts.Count; i++)
            {
                var part = GetPart(car.cosmeticParts[i].partId);
                if (part == null || part.styleTags == null || part.styleTags.Length == 0)
                {
                    continue;
                }

                totalTaggedParts++;
                for (int s = 0; s < part.styleTags.Length; s++)
                {
                    counts[(int)part.styleTags[s]]++;
                }
            }

            if (totalTaggedParts == 0)
            {
                return 0f;
            }

            int bestIndex = 0;
            for (int i = 1; i < counts.Length; i++)
            {
                if (counts[i] > counts[bestIndex])
                {
                    bestIndex = i;
                }
            }

            dominantStyle = (StyleTag)bestIndex;
            return counts[bestIndex] / (float)totalTaggedParts;
        }

        /// <summary>Wertsteigerungs-Multiplikator aus der Stil-Konsistenz (1.0 = kein Bonus, bis 1 + MaxConsistencyBonus bei 100%).</summary>
        public float GetStyleConsistencyBonusMultiplier(CarInstance car)
        {
            float consistency = GetStyleConsistency(car, out _);
            if (consistency < ConsistencyBonusThreshold)
            {
                return 1f;
            }

            float t = (consistency - ConsistencyBonusThreshold) / (1f - ConsistencyBonusThreshold);
            return 1f + t * MaxConsistencyBonus;
        }

        /// <summary>Grobe Schätzung des aktuellen Auktionswerts: Basiswert × Zustand × Teile-Wertfaktoren × Stil-Konsistenz-Bonus.</summary>
        public int GetEstimatedValue(CarInstance car)
        {
            if (car == null)
            {
                return 0;
            }

            var carType = GameManager.Instance.GetCarType(car.carTypeId);
            float baseValue = carType != null
                ? (carType.basePriceMin + carType.basePriceMax) / 2f
                : car.purchasePrice;
            baseValue *= car.condition.GetPriceMultiplier();

            float partsMultiplier = 1f;
            if (car.cosmeticParts != null)
            {
                for (int i = 0; i < car.cosmeticParts.Count; i++)
                {
                    var part = GetPart(car.cosmeticParts[i].partId);
                    if (part != null)
                    {
                        partsMultiplier *= part.valueFactor;
                    }
                }
            }

            float styleBonus = GetStyleConsistencyBonusMultiplier(car);
            float dynoBonus = GetDynoResultBonusMultiplier(car, carType);

            return Mathf.RoundToInt(baseValue * partsMultiplier * styleBonus * dynoBonus);
        }

        /// <summary>
        /// Wertsteigerung aus einem vorliegenden Prüfstand-Ergebnis: vergleicht die gemessene
        /// Spitzenleistung mit der Serien-Basiskurve (beide über dieselbe Nm→PS-Formel berechnet,
        /// damit der Vergleich fair ist) und honoriert die prozentuale Mehrleistung, gedeckelt bei +40%.
        /// </summary>
        private static float GetDynoResultBonusMultiplier(CarInstance car, CarType carType)
        {
            if (car.lastDynoResult == null || !car.lastDynoResult.hasResult || carType == null)
            {
                return 1f;
            }

            var (baselinePeakHp, _) = PerformanceCalculator.GetPeakOutput(
                PerformanceCalculator.BuildMeasuredCurve(carType.baseEngineCurve));
            if (baselinePeakHp <= 0f)
            {
                return 1f;
            }

            float improvement = Mathf.Max(0f, car.lastDynoResult.horsePower / baselinePeakHp - 1f);
            return 1f + Mathf.Min(improvement * 0.5f, 0.4f);
        }
    }
}
