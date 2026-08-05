using System;

namespace CarFlipTycoon.SaveSystem
{
    public enum AuctionStatus
    {
        Running,
        Ended,
        Cancelled
    }

    /// <summary>Eine laufende (oder abgeschlossene) Auktion für eine Auto-Instanz.</summary>
    [Serializable]
    public class AuctionData
    {
        public string id;
        public string carInstanceId;
        public int startPrice;
        public int currentBid;
        public string endTimeUtc;
        public AuctionStatus status = AuctionStatus.Running;
    }
}
