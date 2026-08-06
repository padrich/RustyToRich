using System;
using System.Globalization;
using CarFlipTycoon.SaveSystem;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>
    /// Manipulationssichere Ersatz-Quelle für "die aktuelle UTC-Zeit". Alle zeitgesteuerten
    /// Systeme (Timer, Marktplatz-Angebote, Auktionen, tägliche Belohnung) müssen
    /// <see cref="UtcNow"/> statt <see cref="DateTime.UtcNow"/> verwenden.
    ///
    /// Zwei Angriffe auf die Systemuhr werden abgewehrt:
    /// 1. Zurückstellen der Uhr (z. B. um die tägliche Belohnung erneut freizuschalten oder einen
    ///    gerade erst gestarteten Timer wieder "neu" aussehen zu lassen): die zurückgegebene Zeit
    ///    fällt nie unter die höchste jemals beobachtete, in <see cref="SaveData.lastKnownUtc"/>
    ///    persistierte Zeit.
    /// 2. Vorstellen der Uhr während einer laufenden Session (z. B. um Einbau-/Prüfstand-Timer,
    ///    Auktionen oder Marktangebote sofort ablaufen zu lassen): <see cref="Time.realtimeSinceStartupAsDouble"/>
    ///    basiert auf der Geräte-Laufzeit und reagiert nicht auf Änderungen der Systemuhr. Läuft die
    ///    Systemuhr der so gemessenen Echtzeit spürbar voraus, wird der Sprung ignoriert und
    ///    stattdessen an die tatsächlich vergangene Echtzeit gekoppelt.
    ///
    /// Ein Vorstellen der Uhr bei geschlossener App (kein laufender Prozess, der die Differenz
    /// messen könnte) lässt sich clientseitig ohne Server-Zeitquelle grundsätzlich nicht erkennen;
    /// das ist eine bekannte Grenze rein lokaler Spielstände.
    ///
    /// Der Vorwärtssprung-Schutz (Punkt 2) gilt bewusst NUR innerhalb einer ununterbrochen im
    /// Vordergrund laufenden Session: <see cref="Time.realtimeSinceStartupAsDouble"/> zählt auf
    /// manchen Plattformen (u. a. Android Doze/Deep-Sleep) während einer echten Hintergrund-Pause
    /// nicht zuverlässig weiter. Nach einem über <see cref="OnApplicationPause"/> erkannten
    /// Hintergrund-Aufenthalt wird die Uhr deshalb einmalig wie beim App-Start wieder direkt an die
    /// Systemuhr angeglichen (weiterhin nie rückwärts), statt echte Wartezeit fälschlich als Angriff
    /// zu behandeln und für den Rest der Session hinter der realen Zeit zurückzubleiben.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        // Toleranz für normale Zeitsynchronisation/Uhr-Drift, damit kleine, legitime
        // Sprünge (z. B. NTP-Sync) nicht fälschlich als Manipulation behandelt werden.
        private const double ForwardJumpToleranceSeconds = 2.0;

        public static GameClock Instance { get; private set; }

        private DateTime _trustedUtc;
        private double _monotonicAtAnchor;
        private bool _resyncOnNextAdvance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            DateTime lastKnown = ParseOrMinValue(SaveManager.Instance.CurrentSave.lastKnownUtc);
            DateTime systemNow = DateTime.UtcNow;

            _trustedUtc = systemNow > lastKnown ? systemNow : lastKnown;
            _monotonicAtAnchor = Time.realtimeSinceStartupAsDouble;
            PersistIfNewer();
        }

        private void Update() => Advance();

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Advance();
                // Ab jetzt bis zum nächsten Advance() kann die App suspendiert sein; der nächste
                // Aufruf nach dem Aufwachen soll deshalb nicht gegen die (dann möglicherweise
                // fälschlich kaum fortgeschrittene) monotone Zeit klemmen, sondern direkt resynchronisieren.
                _resyncOnNextAdvance = true;
            }
        }

        /// <summary>Aktuelle, manipulationssichere UTC-Zeit. Ersetzt <see cref="DateTime.UtcNow"/> in allen Zeit-Vergleichen.</summary>
        public DateTime UtcNow
        {
            get
            {
                Advance();
                return _trustedUtc;
            }
        }

        private void Advance()
        {
            double nowMonotonic = Time.realtimeSinceStartupAsDouble;
            DateTime systemNow = DateTime.UtcNow;

            if (_resyncOnNextAdvance)
            {
                // Erster Tick nach einer Hintergrund-Pause: wie beim App-Start direkt an die
                // Systemuhr angleichen (nur nie rückwärts) statt an die ggf. durch den Suspend
                // verfälschte monotone Schätzung zu klemmen.
                _trustedUtc = systemNow > _trustedUtc ? systemNow : _trustedUtc;
                _monotonicAtAnchor = nowMonotonic;
                _resyncOnNextAdvance = false;
                PersistIfNewer();
                return;
            }

            double monotonicDelta = Math.Max(0.0, nowMonotonic - _monotonicAtAnchor);
            DateTime expectedFromMonotonic = _trustedUtc.AddSeconds(monotonicDelta);

            // Systemuhr lief rückwärts (< letzte vertrauenswürdige Zeit) oder ist der seit dem
            // letzten Tick tatsächlich vergangenen Echtzeit spürbar vorausgeeilt (> Toleranz):
            // in beiden Fällen der manipulationssicheren, monotonen Fortschreibung folgen statt
            // der (potenziell manipulierten) Systemuhr zu vertrauen.
            _trustedUtc = systemNow < _trustedUtc || systemNow > expectedFromMonotonic.AddSeconds(ForwardJumpToleranceSeconds)
                ? expectedFromMonotonic
                : systemNow;

            _monotonicAtAnchor = nowMonotonic;
            PersistIfNewer();
        }

        private void PersistIfNewer()
        {
            var save = SaveManager.Instance.CurrentSave;
            DateTime lastKnown = ParseOrMinValue(save.lastKnownUtc);
            if (_trustedUtc > lastKnown)
            {
                save.lastKnownUtc = _trustedUtc.ToString("o", CultureInfo.InvariantCulture);
            }
        }

        private static DateTime ParseOrMinValue(string iso)
        {
            if (!string.IsNullOrEmpty(iso) && DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var parsed))
            {
                return parsed.ToUniversalTime();
            }

            return DateTime.MinValue;
        }
    }
}
