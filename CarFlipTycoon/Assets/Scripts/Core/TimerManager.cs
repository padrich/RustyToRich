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
    /// Generisches Timer-System für zeitverzögerte Vorgänge an einer Auto-Instanz
    /// (aktuell: Optik-/Performance-Einbau, siehe <see cref="TimerPayloadKind"/>).
    /// Läuft auf UTC-Zeitstempeln statt Frame-Zeit, damit Timer auch nach Neustart
    /// der App korrekt weiterlaufen bzw. sofort als abgeschlossen erkannt werden.
    /// Pro Auto ist immer nur ein Timer gleichzeitig aktiv (Status <see cref="CarStatus.BeingTuned"/>).
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

        private static double GetElapsedSeconds(TimerData timer)
        {
            if (!DateTime.TryParse(timer.startTimeUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var startedAt))
            {
                return double.MaxValue;
            }

            return (DateTime.UtcNow - startedAt.ToUniversalTime()).TotalSeconds;
        }

        /// <summary>
        /// Startet einen neuen Timer für ein Auto, sofern dieses aktuell nicht schon
        /// in der Werkstatt beschäftigt ist. Setzt den Auto-Status auf <see cref="CarStatus.BeingTuned"/>.
        /// </summary>
        public bool TryStartTimer(string carInstanceId, TimerType timerType, float durationSeconds,
            TimerPayloadKind payloadKind, string payloadPartId, out string error)
        {
            if (HasActiveTimer(carInstanceId))
            {
                error = "Dieses Auto ist bereits in der Werkstatt beschäftigt.";
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
                payloadPartId = payloadPartId
            };

            SaveManager.Instance.CurrentSave.activeTimers.Add(timer);
            car.status = CarStatus.BeingTuned;
            SaveManager.Instance.Save();
            OnTimersChanged?.Invoke();

            error = null;
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
            if (car != null && car.status == CarStatus.BeingTuned)
            {
                car.status = CarStatus.InGarage;
            }

            SaveManager.Instance.Save();
            OnTimerCompleted?.Invoke(timer);
            OnTimersChanged?.Invoke();
        }
    }
}
