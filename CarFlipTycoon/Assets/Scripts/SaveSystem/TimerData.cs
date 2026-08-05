using System;

namespace CarFlipTycoon.SaveSystem
{
    public enum TimerType
    {
        Tuning,
        Dyno,
        Repair
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
    }
}
