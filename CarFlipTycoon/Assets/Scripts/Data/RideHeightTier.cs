namespace CarFlipTycoon.Data
{
    /// <summary>
    /// Stufen-System für die Kategorie <see cref="CosmeticPartCategory.Hoehe"/>.
    /// Die Reihenfolge der Werte entspricht der tatsächlichen Höhen-Abstufung
    /// (von angehoben bis stark tiefergelegt) und wird u. a. für die Vorschau-Darstellung genutzt.
    /// </summary>
    public enum RideHeightTier
    {
        OffroadLift,
        Serienhoehe,
        LeichtTiefer,
        StarkTieferStance
    }

    /// <summary>Anzeige-Namen für <see cref="RideHeightTier"/>.</summary>
    public static class RideHeightTierInfo
    {
        public static string GetDisplayName(this RideHeightTier tier)
        {
            switch (tier)
            {
                case RideHeightTier.OffroadLift: return "Offroad-Lift";
                case RideHeightTier.Serienhoehe: return "Serienhöhe";
                case RideHeightTier.LeichtTiefer: return "Leicht tiefer";
                case RideHeightTier.StarkTieferStance: return "Stark tiefer / Stance";
                default: return tier.ToString();
            }
        }
    }
}
