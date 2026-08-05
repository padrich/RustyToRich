using System;

namespace CarFlipTycoon.Data
{
    /// <summary>Ergebnis eines Prüfstand-Laufs (Dyno) für eine Auto-Instanz.</summary>
    [Serializable]
    public class DynoResult
    {
        public bool hasResult;
        public float horsePower;
        public float torqueNm;
        public string testDateUtc;
    }
}
