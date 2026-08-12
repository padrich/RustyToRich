using System;
using UnityEngine;

namespace RustyToRich.SaveSystem
{
    public enum TimerType
    {
        Tuning,
        Dyno,
        Repair,
        Auction
    }

    /// <summary>Was beim Abschluss eines Timers zusätzlich zur Timer-eigenen Logik ausgeführt werden soll.</summary>
    public enum TimerPayloadKind
    {
        None,
        InstallCosmeticPart,
        InstallPerformancePart
    }

    /// <summary>
    /// Generische zeitgesteuerte Aktion ("TimedAction"), die einer Auto-Instanz zugeordnet ist –
    /// Tuning-Einbau, Prüfstand-Lauf oder Auktions-Laufzeit. Läuft rein über reale
    /// UTC-Zeitstempel weiter, auch wenn die App im Hintergrund ist oder geschlossen wurde.
    /// </summary>
    [Serializable]
    public class TimerData
    {
        public string id;
        public TimerType timerType;
        public string carInstanceId;
        public string startTimeUtc;
        public float durationSeconds;

        [Tooltip("Optionale Nutzlast, die bei Abschluss ausgewertet wird: partId beim Tuning-Einbau, auctionId bei Auktionen.")]
        public TimerPayloadKind payloadKind = TimerPayloadKind.None;
        public string payloadId;
    }
}
