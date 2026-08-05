using System.Collections.Generic;
using CarFlipTycoon.Data;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>
    /// Meilenstein-basierte Freischaltung von Auto-Typen anhand des in der Verkaufshistorie
    /// erreichten Gesamtgewinns bzw. der Anzahl verkaufter Autos (<see cref="CarType.unlockRequiredTotalProfit"/>/
    /// <see cref="CarType.unlockRequiredSalesCount"/>). Wird u. a. von <see cref="MarketplaceManager"/>
    /// genutzt, damit noch nicht freigeschaltete Auto-Typen nicht als Angebot erscheinen.
    /// </summary>
    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public bool IsCarTypeUnlocked(CarType carType)
        {
            if (carType == null)
            {
                return false;
            }

            if (carType.unlockRequiredTotalProfit <= 0 && carType.unlockRequiredSalesCount <= 0)
            {
                return true;
            }

            var stats = SalesHistoryManager.Instance.GetStatistics();
            return stats.totalProfit >= carType.unlockRequiredTotalProfit
                && stats.totalSalesCount >= carType.unlockRequiredSalesCount;
        }

        public List<CarType> GetUnlockedCarTypes()
        {
            var result = new List<CarType>();
            var all = GameManager.Instance.AllCarTypes;
            for (int i = 0; i < all.Count; i++)
            {
                if (IsCarTypeUnlocked(all[i]))
                {
                    result.Add(all[i]);
                }
            }

            return result;
        }
    }
}
