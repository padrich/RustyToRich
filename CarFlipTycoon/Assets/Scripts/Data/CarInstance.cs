using System;
using System.Collections.Generic;
using CarFlipTycoon.Utility;

namespace CarFlipTycoon.Data
{
    /// <summary>
    /// Laufzeit-Objekt eines konkreten Fahrzeugs im Besitz des Spielers.
    /// Mehrere Instanzen desselben <see cref="CarType"/> können gleichzeitig existieren,
    /// daher referenziert jede Instanz ihre Vorlage nur über die stabile carTypeId.
    /// </summary>
    [Serializable]
    public class CarInstance
    {
        public string instanceId;
        public string carTypeId;

        public int purchasePrice;
        public string purchaseDateUtc;
        public CarCondition condition;

        public List<InstalledCosmeticPart> cosmeticParts = new List<InstalledCosmeticPart>();
        public List<InstalledPerformancePart> performanceParts = new List<InstalledPerformancePart>();

        public DynoResult lastDynoResult = new DynoResult();

        public CarStatus status = CarStatus.InGarage;

        public CarInstance() { }

        public CarInstance(string carTypeId, int purchasePrice, CarCondition condition)
        {
            instanceId = IdFactory.NewId();
            this.carTypeId = carTypeId;
            this.purchasePrice = purchasePrice;
            this.condition = condition;
            purchaseDateUtc = IdFactory.NowUtcIso();
            status = CarStatus.InGarage;
        }
    }
}
