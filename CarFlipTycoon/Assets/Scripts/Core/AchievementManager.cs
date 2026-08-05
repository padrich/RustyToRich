using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>Statische Definition eines Achievements: wann es erreicht ist und welche Coin-Belohnung es gibt.</summary>
    public class AchievementDefinition
    {
        public string id;
        public string name;
        public string description;
        public int coinReward;
        public Func<SalesStatistics, bool> IsReached;
    }

    /// <summary>
    /// Wertet nach jedem relevanten Fortschritts-Ereignis (Verkauf, neues Prüfstand-Ergebnis)
    /// automatisch aus, ob neue Achievements erreicht wurden, und schreibt deren Coin-Belohnung
    /// gut. Bereits eingesammelte Achievements bleiben dauerhaft im Spielstand vermerkt.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        private static readonly List<AchievementDefinition> Definitions = new List<AchievementDefinition>
        {
            new AchievementDefinition
            {
                id = "first_sale", name = "Erster Verkauf", description = "Verkaufe dein erstes Auto.",
                coinReward = 200, IsReached = s => s.totalSalesCount >= 1
            },
            new AchievementDefinition
            {
                id = "big_sale_10k", name = "Großer Deal", description = "Erster Verkauf mit über 10.000 Coins Gewinn.",
                coinReward = 1000, IsReached = s => s.bestSale != null && s.bestSale.Profit >= 10000
            },
            new AchievementDefinition
            {
                id = "power_500", name = "Halbes Tausend", description = "500 PS auf dem Prüfstand gemessen.",
                coinReward = 800, IsReached = s => s.highestMeasuredHorsePower >= 500f
            },
            new AchievementDefinition
            {
                id = "sold_10", name = "Gebrauchtwagenhändler", description = "10 Autos verkauft.",
                coinReward = 600, IsReached = s => s.totalSalesCount >= 10
            },
            new AchievementDefinition
            {
                id = "sold_50", name = "Autohaus-Imperium", description = "50 Autos verkauft.",
                coinReward = 3000, IsReached = s => s.totalSalesCount >= 50
            },
            new AchievementDefinition
            {
                id = "profit_50k", name = "Erste 50.000", description = "50.000 Coins Gesamtgewinn erreicht.",
                coinReward = 1500, IsReached = s => s.totalProfit >= 50000
            }
        };

        public static AchievementManager Instance { get; private set; }

        /// <summary>Ausgelöst, sobald ein Achievement automatisch eingesammelt (und die Belohnung gutgeschrieben) wurde.</summary>
        public event Action<AchievementDefinition> OnAchievementUnlocked;

        public IReadOnlyList<AchievementDefinition> AllDefinitions => Definitions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            GameManager.Instance.OnGarageChanged += HandlePossibleProgress;
            DynoManager.Instance.OnDynoResultChanged += HandleDynoResultChanged;
            HandlePossibleProgress();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGarageChanged -= HandlePossibleProgress;
            }

            if (DynoManager.Instance != null)
            {
                DynoManager.Instance.OnDynoResultChanged -= HandleDynoResultChanged;
            }
        }

        private void HandleDynoResultChanged(string carInstanceId) => HandlePossibleProgress();

        public bool IsClaimed(string achievementId) =>
            SaveManager.Instance.CurrentSave.claimedAchievementIds.Contains(achievementId);

        public bool IsReachedNow(AchievementDefinition definition) =>
            definition.IsReached(SalesHistoryManager.Instance.GetStatistics());

        /// <summary>Sammelt alle aktuell erreichten, aber noch nicht eingesammelten Achievements ein.</summary>
        private void HandlePossibleProgress()
        {
            var stats = SalesHistoryManager.Instance.GetStatistics();
            bool changed = false;

            for (int i = 0; i < Definitions.Count; i++)
            {
                var definition = Definitions[i];
                if (IsClaimed(definition.id) || !definition.IsReached(stats))
                {
                    continue;
                }

                SaveManager.Instance.CurrentSave.claimedAchievementIds.Add(definition.id);
                EconomyManager.Instance.AddCoins(definition.coinReward);
                changed = true;
                Debug.Log($"[AchievementManager] Achievement freigeschaltet: {definition.name} (+{definition.coinReward} Coins)");
                OnAchievementUnlocked?.Invoke(definition);
            }

            if (changed)
            {
                SaveManager.Instance.Save();
            }
        }
    }
}
