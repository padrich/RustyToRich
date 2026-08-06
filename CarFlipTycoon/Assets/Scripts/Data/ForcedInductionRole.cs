namespace CarFlipTycoon.Data
{
    /// <summary>
    /// Beschreibt, wie ein Performance-Teil mit Aufladung (Turbolader/Kompressor) interagiert.
    /// Wird von <see cref="PerformanceCalculator"/> ausgewertet, um Kompatibilitäts-Regeln und
    /// abhängige Bonus-Multiplikatoren generisch anzuwenden, ohne Kategorien im Code fest zu verdrahten.
    /// </summary>
    public enum ForcedInductionRole
    {
        /// <summary>Kein Bezug zur Aufladung.</summary>
        None,

        /// <summary>Das Teil selbst ist ein Aufladungssystem (Turbolader, Kompressor). Nur eines gleichzeitig möglich.</summary>
        ProvidesForcedInduction,

        /// <summary>Das Teil ist nur sinnvoll, wenn bereits ein Aufladungssystem verbaut ist (z. B. Ladeluftkühler).</summary>
        RequiresForcedInduction,

        /// <summary>Der Bonus-Effekt fällt deutlich stärker aus, wenn ein Aufladungssystem verbaut ist (z. B. Chiptuning, Auspuff).</summary>
        StrongerWithForcedInduction,

        /// <summary>Der Bonus-Effekt fällt schwächer aus, wenn ein Aufladungssystem verbaut ist (z. B. Fächerkrümmer bei Saugmotoren).</summary>
        StrongerWithoutForcedInduction
    }
}
