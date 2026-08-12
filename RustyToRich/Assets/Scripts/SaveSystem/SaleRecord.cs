using System;

namespace RustyToRich.SaveSystem
{
    public enum SaleType
    {
        DirectSale,
        Auction
    }

    /// <summary>
    /// Ein abgeschlossener Verkauf, für die dauerhafte Verkaufshistorie. Enthält alle Werte,
    /// die für Statistiken (Gewinn, Bestwerte, meistverkauftes Modell) gebraucht werden, sodass
    /// die Historie ohne erneuten Zugriff auf die (ggf. bereits verkaufte) Auto-Instanz auskommt.
    /// </summary>
    [Serializable]
    public class SaleRecord
    {
        public string id;
        public string carInstanceId;
        public string carTypeId;
        public string modelNameSnapshot;

        public int purchasePrice;
        public string purchaseDateUtc;

        public int cosmeticTuningCost;
        public int performanceTuningCost;

        public bool hasDynoResult;
        public float dynoHorsePower;
        public float dynoTorqueNm;

        public int salePrice;
        public string saleDateUtc;
        public SaleType saleType;

        /// <summary>Gewinn/Verlust: Verkaufserlös abzüglich Kaufpreis und aller Tuning-Kosten.</summary>
        public int Profit => salePrice - purchasePrice - cosmeticTuningCost - performanceTuningCost;
    }
}
