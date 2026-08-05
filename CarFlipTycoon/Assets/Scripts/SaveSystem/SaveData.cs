using System;
using System.Collections.Generic;
using CarFlipTycoon.Data;

namespace CarFlipTycoon.SaveSystem
{
    /// <summary>
    /// Wurzel-Objekt des lokalen Spielstands. Wird als Ganzes über JsonUtility
    /// serialisiert. Unterstützt von Anfang an Listen von Auto-Instanzen,
    /// da mehrere Fahrzeuge gleichzeitig existieren können.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1;

        public long coins;

        public List<CarInstance> ownedCars = new List<CarInstance>();
        public List<TimerData> activeTimers = new List<TimerData>();
        public List<AuctionData> activeAuctions = new List<AuctionData>();
        public List<SaleRecord> salesHistory = new List<SaleRecord>();
    }
}
