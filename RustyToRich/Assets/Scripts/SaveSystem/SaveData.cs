using System;
using System.Collections.Generic;
using RustyToRich.Data;
using UnityEngine;

namespace RustyToRich.SaveSystem
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

        public long coins = 2000;

        [Tooltip("Anzahl verfügbarer Stellplätze in der Garage. Start: 3, erweiterbar gegen Coins.")]
        public int garageCapacity = 3;

        public List<CarInstance> ownedCars = new List<CarInstance>();
        public List<MarketOffer> marketOffers = new List<MarketOffer>();
        public List<TimerData> activeTimers = new List<TimerData>();
        public List<AuctionData> activeAuctions = new List<AuctionData>();
        public List<SaleRecord> salesHistory = new List<SaleRecord>();

        [Header("Progression")]
        public List<string> claimedAchievementIds = new List<string>();
        public string lastDailyRewardDateUtc;
        public int dailyRewardStreak;

        [Tooltip("Höchste jemals von GameClock beobachtete, vertrauenswürdige UTC-Zeit. Schützt Timer, " +
                 "Marktangebote, Auktionen und die tägliche Belohnung davor, dass die Systemuhr " +
                 "zurückgestellt wird, um Cooldowns zu umgehen.")]
        public string lastKnownUtc;
    }
}
