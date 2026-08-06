using System;
using CarFlipTycoon.Data;
using CarFlipTycoon.SaveSystem;
using CarFlipTycoon.Utility;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>
    /// Prüfstand-Feature: schickt ein Auto für eine kurze Messung auf den Prüfstand
    /// (Timer-System) und berechnet danach die tatsächliche Leistungskurve aus Basis-
    /// Motorkurve plus verbauten Performance-Teilen, inkl. realer Nm→PS-Umrechnung
    /// (siehe <see cref="PerformanceCalculator"/>). Das vorherige Ergebnis bleibt für
    /// einen Vorher/Nachher-Vergleich erhalten.
    /// </summary>
    public class DynoManager : MonoBehaviour
    {
        private const float MinDurationSeconds = 30f;
        private const float MaxDurationSeconds = 90f;

        public static DynoManager Instance { get; private set; }

        /// <summary>Ausgelöst, nachdem für die angegebene Auto-Instanz ein neues Prüfstand-Ergebnis vorliegt.</summary>
        public event Action<string> OnDynoResultChanged;

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

        /// <summary>Schickt ein Auto zum Prüfstand: startet einen kurzen Timer, blockiert falls das Auto bereits beschäftigt ist.</summary>
        public bool TrySendToDyno(CarInstance car, out string error)
        {
            if (car == null)
            {
                error = "Ungültige Anfrage.";
                return false;
            }

            float duration = UnityEngine.Random.Range(MinDurationSeconds, MaxDurationSeconds);
            return TimerManager.Instance.TryStartTimer(car.instanceId, TimerType.Dyno, duration,
                TimerPayloadKind.None, null, out error);
        }

        private void HandleTimerCompleted(TimerData timer)
        {
            if (timer.timerType != TimerType.Dyno)
            {
                return;
            }

            var car = GameManager.Instance.GetCarInstance(timer.carInstanceId);
            if (car == null)
            {
                return;
            }

            var effectiveCurve = PerformanceTuningManager.Instance.GetEffectiveCurve(car);
            var measuredCurve = PerformanceCalculator.BuildMeasuredCurve(effectiveCurve);

            float peakHp = 0f;
            int peakHpRpm = 0;
            float peakNm = 0f;
            int peakNmRpm = 0;

            for (int i = 0; i < measuredCurve.points.Count; i++)
            {
                var point = measuredCurve.points[i];
                if (point.horsePower > peakHp)
                {
                    peakHp = point.horsePower;
                    peakHpRpm = point.rpm;
                }

                if (point.torqueNm > peakNm)
                {
                    peakNm = point.torqueNm;
                    peakNmRpm = point.rpm;
                }
            }

            car.previousDynoResult = car.lastDynoResult;
            car.lastDynoResult = new DynoResult
            {
                hasResult = true,
                horsePower = peakHp,
                horsePowerRpm = peakHpRpm,
                torqueNm = peakNm,
                torqueRpm = peakNmRpm,
                testDateUtc = IdFactory.NowUtcIso(),
                curvePoints = measuredCurve.points
            };

            SaveManager.Instance.Save();
            OnDynoResultChanged?.Invoke(car.instanceId);
        }
    }
}
