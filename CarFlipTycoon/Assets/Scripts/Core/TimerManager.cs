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
    /// Generisches Timer-System ("TimedAction") für zeitgesteuerte Vorgänge an einer
    /// Auto-Instanz: Optik-/Performance-Einbau, Prüfstand-Lauf und Auktions-Laufzeit
    /// (siehe <see cref="TimerType"/>/<see cref="TimerPayloadKind"/>). Läuft auf realen
    /// UTC-Zeitstempeln statt Frame-Zeit, damit Timer auch nach Hintergrund/Neustart der
    /// App korrekt weiterlaufen bzw. beim nächsten Start sofort als abgeschlossen erkannt
    /// werden. Mehrere Timer für unterschiedliche Autos laufen unabhängig voneinander;
    /// pro Auto ist aber immer nur ein Timer gleichzeitig aktiv.
    /// </summary>
    public class TimerManager : MonoBehaviour
    {
        private const float TickIntervalSeconds = 1f;

        public static TimerManager Instance { get; private set; }

        /// <summary>Ausgelöst, sobald ein Timer abgeschlossen wurde (nach Anwendung seiner Payload).</summary>
        public event Action<TimerData> OnTimerCompleted;

        /// <summary>Ausgelöst, wenn sich die Liste aktiver Timer geändert hat (Start oder Abschluss).</summary>
        public event Action OnTimersChanged;

        private float _tickTimer;

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
            CompleteElapsedTimers();
        }

        private void Update()
        {
            _tickTimer += Time.unscaledDeltaTime;
            if (_tickTimer < TickIntervalSeconds)
            {
                return;
            }

            _tickTimer = 0f;
            CompleteElapsedTimers();
        }

        public bool HasActiveTimer(string carInstanceId) => GetActiveTimer(carInstanceId) != null;

        public TimerData GetActiveTimer(string carInstanceId)
        {
            var timers = SaveManager.Instance.CurrentSave.activeTimers;
            for (int i = 0; i < timers.Count; i++)
            {
                if (timers[i].carInstanceId == carInstanceId)
                {
                    return timers[i];
                }
            }

            return null;
        }

        /// <summary>Verbleibende Sekunden bis zum Abschluss (0, falls bereits fällig).</summary>
        public float GetRemainingSeconds(TimerData timer)
        {
            if (timer == null)
            {
                return 0f;
            }

            float elapsed = (float)GetElapsedSeconds(timer);
            return Mathf.Max(0f, timer.durationSeconds - elapsed);
        }

        /// <summary>Fortschritt von 0 (gerade gestartet) bis 1 (fertig) – für Fortschrittsbalken.</summary>
        public float GetProgress01(TimerData timer)
        {
            if (timer == null || timer.durationSeconds <= 0f)
            {
                return 1f;
            }

            float elapsed = (float)GetElapsedSeconds(timer);
            return Mathf.Clamp01(elapsed / timer.durationSeconds);
        }

        private static double GetElapsedSeconds(TimerData timer)
        {
            if (!DateTime.TryParse(timer.startTimeUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var startedAt))
            {
                return double.MaxValue;
            }

            return (GameClock.Instance.UtcNow - startedAt.ToUniversalTime()).TotalSeconds;
        }

        /// <summary>Welchen Auto-Status ein laufender Timer dieses Typs repräsentiert.</summary>
        private static CarStatus StatusForTimerType(TimerType timerType)
        {
            switch (timerType)
            {
                case TimerType.Dyno: return CarStatus.OnDyno;
                case TimerType.Auction: return CarStatus.InAuction;
                case TimerType.Tuning:
                case TimerType.Repair:
                default: return CarStatus.BeingTuned;
            }
        }

        /// <summary>
        /// Startet einen neuen Timer für ein Auto, sofern dieses aktuell nicht schon durch
        /// einen anderen Timer beschäftigt ist. Setzt den zum Timer-Typ passenden Auto-Status.
        /// </summary>
        public bool TryStartTimer(string carInstanceId, TimerType timerType, float durationSeconds,
            TimerPayloadKind payloadKind, string payloadId, out string error)
        {
            if (HasActiveTimer(carInstanceId))
            {
                error = "Dieses Auto ist aktuell beschäftigt und nicht verfügbar.";
                return false;
            }

            var car = GameManager.Instance.GetCarInstance(carInstanceId);
            if (car == null)
            {
                error = "Auto nicht gefunden.";
                return false;
            }

            var timer = new TimerData
            {
                id = IdFactory.NewId(),
                timerType = timerType,
                carInstanceId = carInstanceId,
                startTimeUtc = IdFactory.NowUtcIso(),
                durationSeconds = Mathf.Max(1f, durationSeconds),
                payloadKind = payloadKind,
                payloadId = payloadId
            };

            SaveManager.Instance.CurrentSave.activeTimers.Add(timer);
            car.status = StatusForTimerType(timerType);
            SaveManager.Instance.Save();
            OnTimersChanged?.Invoke();

            error = null;
            return true;
        }

        /// <summary>
        /// Schließt einen laufenden Timer sofort ab, unabhängig von der verbleibenden Zeit –
        /// z. B. über den "Sofort fertig ⚡"-Button bzw. später einen Rewarded Ad.
        /// </summary>
        public bool FinishNow(string carInstanceId)
        {
            var timer = GetActiveTimer(carInstanceId);
            if (timer == null)
            {
                return false;
            }

            CompleteTimer(timer);
            return true;
        }

        private void CompleteElapsedTimers()
        {
            var timers = SaveManager.Instance.CurrentSave.activeTimers;
            List<TimerData> due = null;

            for (int i = 0; i < timers.Count; i++)
            {
                if (GetElapsedSeconds(timers[i]) >= timers[i].durationSeconds)
                {
                    if (due == null)
                    {
                        due = new List<TimerData>();
                    }

                    due.Add(timers[i]);
                }
            }

            if (due == null)
            {
                return;
            }

            for (int i = 0; i < due.Count; i++)
            {
                CompleteTimer(due[i]);
            }
        }

        private void CompleteTimer(TimerData timer)
        {
            SaveManager.Instance.CurrentSave.activeTimers.Remove(timer);

            var car = GameManager.Instance.GetCarInstance(timer.carInstanceId);
            // Auktions-Timer setzen den Status nicht selbst zurück: AuctionManager übernimmt
            // das Auto beim Abschluss vollständig (Verkauf/Status "Sold").
            if (car != null && timer.timerType != TimerType.Auction && car.status == StatusForTimerType(timer.timerType))
            {
                car.status = CarStatus.InGarage;
            }

            SaveManager.Instance.Save();
            OnTimerCompleted?.Invoke(timer);
            OnTimersChanged?.Invoke();
        }
    }
}
