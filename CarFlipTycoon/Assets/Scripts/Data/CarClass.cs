namespace CarFlipTycoon.Data
{
    /// <summary>Fahrzeugklasse eines Auto-Typs (Vorlage).</summary>
    /// <remarks>
    /// Neue Werte werden am Ende angehängt, da CarType-Assets den Enum-Wert als
    /// Index in YAML serialisieren – ein Einfügen in der Mitte würde bestehende
    /// Assets stillschweigend auf die falsche Klasse verschieben.
    /// </remarks>
    public enum CarClass
    {
        Kleinwagen,
        Kompaktklasse,
        Mittelklasse,
        Oberklasse,
        SportLimousine,
        Sportwagen,
        Muscle,
        OffroadPickup,
        SUV,
        Kombi,
        Oldtimer,
        Coupe
    }
}
