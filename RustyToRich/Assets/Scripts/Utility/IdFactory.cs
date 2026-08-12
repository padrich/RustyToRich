using System;
using System.Globalization;
using RustyToRich.Core;

namespace RustyToRich.Utility
{
    public static class IdFactory
    {
        public static string NewId() => Guid.NewGuid().ToString("N");

        /// <summary>
        /// Manipulationssicherer Zeitstempel (siehe <see cref="GameClock"/>). Fällt außerhalb der
        /// Spiel-Laufzeit (z. B. in Editor-Tools ohne aktive GameClock-Instanz) auf die reguläre
        /// Systemuhr zurück.
        /// </summary>
        public static string NowUtcIso()
        {
            DateTime now = GameClock.Instance != null ? GameClock.Instance.UtcNow : DateTime.UtcNow;
            return now.ToString("o", CultureInfo.InvariantCulture);
        }
    }
}
