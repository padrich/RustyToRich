namespace CarFlipTycoon.Data
{
    /// <summary>Aktueller Status einer Auto-Instanz im Besitz des Spielers.</summary>
    public enum CarStatus
    {
        InGarage,
        BeingTuned,
        OnDyno,
        InAuction,
        Sold
    }

    /// <summary>Anzeige-Namen für <see cref="CarStatus"/>, z. B. für die Garage-UI.</summary>
    public static class CarStatusInfo
    {
        public static string GetDisplayName(this CarStatus status)
        {
            switch (status)
            {
                case CarStatus.InGarage: return "In der Garage";
                case CarStatus.BeingTuned: return "Wird getunt";
                case CarStatus.OnDyno: return "Auf dem Prüfstand";
                case CarStatus.InAuction: return "In Auktion";
                case CarStatus.Sold: return "Verkauft";
                default: return status.ToString();
            }
        }
    }
}
