using System;

namespace CarFlipTycoon.SaveSystem
{
    public enum SaleType
    {
        DirectSale,
        Auction
    }

    /// <summary>Ein abgeschlossener Verkauf, für die Verkaufshistorie.</summary>
    [Serializable]
    public class SaleRecord
    {
        public string id;
        public string carInstanceId;
        public string carTypeId;
        public string modelNameSnapshot;
        public int salePrice;
        public string saleDateUtc;
        public SaleType saleType;
    }
}
