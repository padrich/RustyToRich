using System;

namespace CarFlipTycoon.Data
{
    /// <summary>
    /// Ein einzelnes, zeitlich begrenztes Kaufangebot im Marktplatz. Angebote werden von
    /// <see cref="CarFlipTycoon.Core.MarketplaceManager"/> erzeugt, verwaltet und
    /// nach Ablauf oder Kauf durch ein neues Angebot ersetzt.
    /// </summary>
    [Serializable]
    public class MarketOffer
    {
        public string offerId;
        public string carTypeId;
        public CarCondition condition;
        public int price;
        public string listedAtUtc;
        public float lifetimeSeconds;
    }
}
