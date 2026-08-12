namespace RustyToRich.Data
{
    /// <summary>Kategorien für Optik-Teile (kosmetisches Tuning). Pro Auto-Instanz ist je Kategorie höchstens ein Teil verbaut.</summary>
    public enum CosmeticPartCategory
    {
        Felgen,
        Hoehe,
        Spoiler,
        Bodykit,
        Rennsitze,
        Kaefig,
        Lackierung
    }

    /// <summary>Anzeige-Namen für <see cref="CosmeticPartCategory"/>.</summary>
    public static class CosmeticPartCategoryInfo
    {
        public static string GetDisplayName(this CosmeticPartCategory category)
        {
            switch (category)
            {
                case CosmeticPartCategory.Felgen: return "Felgen";
                case CosmeticPartCategory.Hoehe: return "Tieferlegung/Höhe";
                case CosmeticPartCategory.Spoiler: return "Spoiler";
                case CosmeticPartCategory.Bodykit: return "Bodykit";
                case CosmeticPartCategory.Rennsitze: return "Rennsitze";
                case CosmeticPartCategory.Kaefig: return "Käfig";
                case CosmeticPartCategory.Lackierung: return "Lackierung";
                default: return category.ToString();
            }
        }
    }

    /// <summary>Kategorien für Performance-Teile (technisches Tuning). Pro Auto-Instanz ist je Kategorie höchstens ein Teil verbaut.</summary>
    public enum PerformancePartCategory
    {
        Ansaugung,
        Turbolader,
        Kompressor,
        Ladeluftkuehler,
        Auspuffanlage,
        Faecherkruemmer,
        Nockenwellen,
        Chiptuning
    }

    /// <summary>Anzeige-Namen für <see cref="PerformancePartCategory"/>.</summary>
    public static class PerformancePartCategoryInfo
    {
        public static string GetDisplayName(this PerformancePartCategory category)
        {
            switch (category)
            {
                case PerformancePartCategory.Ansaugung: return "Ansaugung";
                case PerformancePartCategory.Turbolader: return "Turbolader";
                case PerformancePartCategory.Kompressor: return "Kompressor";
                case PerformancePartCategory.Ladeluftkuehler: return "Ladeluftkühler";
                case PerformancePartCategory.Auspuffanlage: return "Auspuffanlage/Downpipe";
                case PerformancePartCategory.Faecherkruemmer: return "Fächerkrümmer";
                case PerformancePartCategory.Nockenwellen: return "Nockenwellen";
                case PerformancePartCategory.Chiptuning: return "Chiptuning/ECU-Remap";
                default: return category.ToString();
            }
        }
    }
}
