namespace RustyToRich.Data
{
    /// <summary>Anzeige-Namen für <see cref="CarClass"/>, z. B. für Marktplatz- und Garage-UI.</summary>
    public static class CarClassInfo
    {
        public static string GetDisplayName(this CarClass carClass)
        {
            switch (carClass)
            {
                case CarClass.Kleinwagen: return "Kleinwagen";
                case CarClass.Kompaktklasse: return "Kompaktklasse";
                case CarClass.Mittelklasse: return "Mittelklasse";
                case CarClass.Oberklasse: return "Oberklasse";
                case CarClass.SportLimousine: return "Sport-Limousine";
                case CarClass.Sportwagen: return "Sportwagen";
                case CarClass.Muscle: return "Muscle";
                case CarClass.OffroadPickup: return "Offroad/Pickup";
                case CarClass.SUV: return "SUV";
                case CarClass.Kombi: return "Kombi";
                case CarClass.Oldtimer: return "Oldtimer";
                case CarClass.Coupe: return "Coupé";
                default: return carClass.ToString();
            }
        }
    }
}
