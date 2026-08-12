namespace RustyToRich.Data
{
    /// <summary>
    /// Zustand eines Fahrzeugs im Marktplatz-Angebot bzw. einer gekauften Auto-Instanz.
    /// Beeinflusst den Angebotspreis relativ zur Basis-Preisspanne des Auto-Typs.
    /// </summary>
    public enum CarCondition
    {
        Schrottreif,
        Gebraucht,
        Gepflegt,
        TopZustand,
        Neuwertig
    }

    /// <summary>Anzeige-Namen und Preis-Multiplikatoren für <see cref="CarCondition"/>.</summary>
    public static class CarConditionInfo
    {
        public static string GetDisplayName(this CarCondition condition)
        {
            switch (condition)
            {
                case CarCondition.Schrottreif: return "Schrottreif";
                case CarCondition.Gebraucht: return "Gebraucht";
                case CarCondition.Gepflegt: return "Gepflegt";
                case CarCondition.TopZustand: return "Top-Zustand";
                case CarCondition.Neuwertig: return "Neuwertig";
                default: return condition.ToString();
            }
        }

        /// <summary>Multiplikator auf die Basis-Preisspanne (basePriceMin/Max) des Auto-Typs.</summary>
        public static float GetPriceMultiplier(this CarCondition condition)
        {
            switch (condition)
            {
                case CarCondition.Schrottreif: return 0.35f;
                case CarCondition.Gebraucht: return 0.6f;
                case CarCondition.Gepflegt: return 0.85f;
                case CarCondition.TopZustand: return 1.05f;
                case CarCondition.Neuwertig: return 1.3f;
                default: return 1f;
            }
        }
    }
}
