namespace RustyToRich.Data
{
    /// <summary>
    /// Stil-Ausrichtung eines Optik-Teils. Ein Teil kann mehrere Stile bedienen (z. B. ein
    /// Bodykit, das sowohl als Offroad- als auch als Street-Look funktioniert). Weitere
    /// Stile lassen sich bei Bedarf einfach ergänzen.
    /// </summary>
    public enum StyleTag
    {
        Offroad,
        Racing,
        Street
    }

    /// <summary>Anzeige-Namen für <see cref="StyleTag"/>.</summary>
    public static class StyleTagInfo
    {
        public static string GetDisplayName(this StyleTag styleTag)
        {
            switch (styleTag)
            {
                case StyleTag.Offroad: return "Offroad";
                case StyleTag.Racing: return "Racing";
                case StyleTag.Street: return "Street";
                default: return styleTag.ToString();
            }
        }
    }
}
