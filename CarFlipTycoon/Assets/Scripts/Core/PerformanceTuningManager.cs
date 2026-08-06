using System;
using System.Collections.Generic;
using CarFlipTycoon.Data;
using CarFlipTycoon.SaveSystem;
using CarFlipTycoon.Utility;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>
    /// Verwaltet Performance-Tuning: verfügbare Teile je Kategorie, Kompatibilitätsprüfung,
    /// Kauf &amp; Einbau (über <see cref="TimerManager"/>) sowie die live berechnete effektive
    /// Motorkurve einer Auto-Instanz. Pro Kategorie ist immer höchstens ein Teil verbaut.
    /// </summary>
    public class PerformanceTuningManager : MonoBehaviour
    {
        private const string DatabaseResourcePath = "PerformancePartDatabase";

        public static PerformanceTuningManager Instance { get; private set; }

        /// <summary>Ausgelöst, nachdem ein Performance-Teil für die angegebene Auto-Instanz verbaut wurde.</summary>
        public event Action<string> OnPerformancePartsChanged;

        private PerformancePartDatabase _database;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _database = Resources.Load<PerformancePartDatabase>(DatabaseResourcePath);
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

        public PerformancePart GetPart(string partId) => _database != null ? _database.GetById(partId) : null;

        public List<PerformancePart> GetPartsForCategory(PerformancePartCategory category)
        {
            return _database != null ? _database.GetByCategory(category) : new List<PerformancePart>();
        }

        public InstalledPerformancePart GetInstalledSlot(CarInstance car, PerformancePartCategory category)
        {
            if (car == null || car.performanceParts == null)
            {
                return null;
            }

            for (int i = 0; i < car.performanceParts.Count; i++)
            {
                if (car.performanceParts[i].category == category)
                {
                    return car.performanceParts[i];
                }
            }

            return null;
        }

        public PerformancePart GetInstalledPart(CarInstance car, PerformancePartCategory category)
        {
            var slot = GetInstalledSlot(car, category);
            return slot != null ? GetPart(slot.partId) : null;
        }

        public List<PerformancePart> ResolveInstalledParts(CarInstance car)
        {
            var result = new List<PerformancePart>();
            if (car == null || car.performanceParts == null)
            {
                return result;
            }

            for (int i = 0; i < car.performanceParts.Count; i++)
            {
                var part = GetPart(car.performanceParts[i].partId);
                if (part != null)
                {
                    result.Add(part);
                }
            }

            return result;
        }

        /// <summary>
        /// Prüft harte Inkompatibilität, die den Kauf blockiert. Aktuell: zwei gleichzeitige
        /// Aufladungssysteme (Turbolader und Kompressor schließen sich gegenseitig aus).
        /// </summary>
        public bool IsHardIncompatible(CarInstance car, PerformancePart candidate, out string reason)
        {
            reason = null;
            if (candidate.forcedInductionRole != ForcedInductionRole.ProvidesForcedInduction)
            {
                return false;
            }

            var installed = ResolveInstalledParts(car);
            for (int i = 0; i < installed.Count; i++)
            {
                if (installed[i].category != candidate.category &&
                    installed[i].forcedInductionRole == ForcedInductionRole.ProvidesForcedInduction)
                {
                    reason = $"Schließt sich mit {installed[i].partName} aus (nur ein Aufladungssystem gleichzeitig möglich).";
                    return true;
                }
            }

            return false;
        }

        /// <summary>Nicht-blockierender Hinweis, z. B. wenn ein Teil ohne Aufladung aktuell wenig bringt.</summary>
        public string GetAdvisoryWarning(CarInstance car, PerformancePart candidate)
        {
            if (candidate.forcedInductionRole == ForcedInductionRole.RequiresForcedInduction)
            {
                bool hasForcedInduction = PerformanceCalculator.HasForcedInduction(ResolveInstalledParts(car));
                if (!hasForcedInduction)
                {
                    return "Ohne Turbolader oder Kompressor bringt dieses Teil aktuell keinen Vorteil.";
                }
            }

            return null;
        }

        /// <summary>Kauft ein Performance-Teil für ein Auto und startet den Einbau-Timer.</summary>
        public bool TryPurchaseAndInstall(CarInstance car, PerformancePart part, out string error)
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

            if (IsHardIncompatible(car, part, out string incompatibilityReason))
            {
                error = incompatibilityReason;
                return false;
            }

            if (!EconomyManager.Instance.TrySpendCoins(part.price))
            {
                error = "Nicht genug Coins.";
                return false;
            }

            if (!TimerManager.Instance.TryStartTimer(car.instanceId, TimerType.Tuning, part.installDurationSeconds,
                    TimerPayloadKind.InstallPerformancePart, part.PartId, out error))
            {
                EconomyManager.Instance.AddCoins(part.price);
                return false;
            }

            error = null;
            return true;
        }

        private void HandleTimerCompleted(TimerData timer)
        {
            if (timer.payloadKind != TimerPayloadKind.InstallPerformancePart)
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

            for (int i = car.performanceParts.Count - 1; i >= 0; i--)
            {
                if (car.performanceParts[i].category == part.category)
                {
                    car.performanceParts.RemoveAt(i);
                }
            }

            car.performanceParts.Add(new InstalledPerformancePart
            {
                category = part.category,
                partId = part.PartId,
                partName = part.partName,
                purchasePrice = part.price,
                installedDateUtc = IdFactory.NowUtcIso(),
                horsePowerBonus = part.horsePowerBonusFlat,
                torqueBonus = part.torqueBonusFlat
            });

            SaveManager.Instance.Save();
            OnPerformancePartsChanged?.Invoke(car.instanceId);
        }

        /// <summary>Effektive Motorkurve der Basis-Kurve des Auto-Typs plus aller verbauten Performance-Teile.</summary>
        public EngineCurve GetEffectiveCurve(CarInstance car)
        {
            if (car == null)
            {
                return new EngineCurve();
            }

            var carType = GameManager.Instance.GetCarType(car.carTypeId);
            var baseCurve = carType != null ? carType.baseEngineCurve : new EngineCurve();
            return PerformanceCalculator.ComputeEffectiveCurve(baseCurve, ResolveInstalledParts(car));
        }

        /// <summary>Grobe PS/Nm-Spitzenwert-Schätzung vor dem tatsächlichen Prüfstand-Test.</summary>
        public (float horsePower, float torqueNm) GetEstimatedPeakOutput(CarInstance car)
        {
            return PerformanceCalculator.GetPeakOutput(GetEffectiveCurve(car));
        }
    }
}
