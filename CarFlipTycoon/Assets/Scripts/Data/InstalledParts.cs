using System;

namespace CarFlipTycoon.Data
{
    /// <summary>Ein verbautes Optik-Teil (kosmetisches Tuning) an einer Auto-Instanz.</summary>
    [Serializable]
    public class InstalledCosmeticPart
    {
        public CosmeticPartCategory category;
        public string partId;
        public string partName;
        public int purchasePrice;
        public string installedDateUtc;
    }

    /// <summary>Ein verbautes Performance-Teil (technisches Tuning) an einer Auto-Instanz.</summary>
    [Serializable]
    public class InstalledPerformancePart
    {
        public PerformancePartCategory category;
        public string partId;
        public string partName;
        public int purchasePrice;
        public string installedDateUtc;

        public float horsePowerBonus;
        public float torqueBonus;
    }
}
