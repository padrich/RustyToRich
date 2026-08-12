using System;

namespace RustyToRich.SaveSystem
{
    public enum AuctionStatus
    {
        Running,
        Ended,
        Cancelled
    }

    /// <summary>
    /// Eine laufende (oder gerade abgeschlossene) Auktion für eine Auto-Instanz. Die eigentliche
    /// Laufzeit wird über einen <see cref="TimerData"/> (TimerType.Auction, payloadId = AuctionData.id)
    /// gesteuert; diese Struktur hält nur den Bieter-Zustand.
    /// </summary>
    [Serializable]
    public class AuctionData
    {
        public string id;
        public string carInstanceId;

        public int startPrice;
        public int currentBid;
        public int bidCount;
        /// <summary>Obergrenze für simulierte Gebote, einmalig bei Auktionsstart bestimmt.</summary>
        public int maxBidCeiling;

        public string startTimeUtc;
        public string endTimeUtc;
        public string lastBidTimeUtc;
        /// <summary>Sekunden bis zum nächstmöglichen simulierten Gebot, wird nach jedem Gebot neu gewürfelt.</summary>
        public float nextBidIntervalSeconds;

        public AuctionStatus status = AuctionStatus.Running;
    }
}
