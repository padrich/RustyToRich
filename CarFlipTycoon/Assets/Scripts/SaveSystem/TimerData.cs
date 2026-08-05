using System;
using UnityEngine;

namespace CarFlipTycoon.SaveSystem
{
    public enum TimerType
    {
        Tuning,
        Dyno,
        Repair
    }

    /// <summary>Was beim Abschluss eines Timers zusätzlich zur Timer-eigenen Logik ausgeführt werden soll.</summary>
    public enum TimerPayloadKind
    {
        None,
        InstallCosmeticPart,
        InstallPerformancePart
    }

    /// <summary>Ein laufender Timer, der einer Auto-Instanz zugeordnet ist (z. B. Tuning-Arbeit, Prüfstand-Lauf).</summary>
    [Serializable]
    public class TimerData
    {
        public string id;
        public TimerType timerType;
        public string carInstanceId;
        public string startTimeUtc;
        public float durationSeconds;

        [Tooltip("Optionale Nutzlast, die TimerManager bei Abschluss auswertet, z. B. der Einbau eines Tuning-Teils.")]
        public TimerPayloadKind payloadKind = TimerPayloadKind.None;
        public string payloadPartId;
    }
}
