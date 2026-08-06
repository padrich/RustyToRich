using System.Collections.Generic;
using CarFlipTycoon.SaveSystem;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    public enum HistorySortMode
    {
        Date,
        Profit,
        Model,
        MeasuredHorsePower
    }

    /// <summary>Aggregierte Statistik-Übersicht über die gesamte Verkaufshistorie.</summary>
    public class SalesStatistics
    {
        public int totalSalesCount;
        public int totalProfit;
        public SaleRecord bestSale;
        public float highestMeasuredHorsePower;
        public float highestMeasuredTorqueNm;
        public string mostSoldModelName;
        public int mostSoldModelCount;
    }

    /// <summary>
    /// Liest die dauerhaft im Spielstand gespeicherte Verkaufshistorie aus und bietet Sortierung,
    /// Paginierung (für performanten Abruf auch bei vielen Einträgen) und eine aggregierte
    /// Statistik-Übersicht darüber an. Die Historie selbst lebt in <see cref="SaveData.salesHistory"/>
    /// und wird bereits von <see cref="GameManager.SellCar"/> befüllt.
    /// </summary>
    public class SalesHistoryManager : MonoBehaviour
    {
        public static SalesHistoryManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public IReadOnlyList<SaleRecord> AllRecords => SaveManager.Instance.CurrentSave.salesHistory;

        public int GetPageCount(int pageSize)
        {
            if (pageSize <= 0)
            {
                return 1;
            }

            return Mathf.Max(1, Mathf.CeilToInt(AllRecords.Count / (float)pageSize));
        }

        /// <summary>Liefert eine sortierte Seite der Historie, ohne bei jedem Aufruf die komplette Liste im UI zu materialisieren.</summary>
        public List<SaleRecord> GetSortedPage(HistorySortMode sortMode, bool descending, int pageIndex, int pageSize)
        {
            var sorted = new List<SaleRecord>(AllRecords);
            sorted.Sort((a, b) => Compare(a, b, sortMode));
            if (descending)
            {
                sorted.Reverse();
            }

            int start = Mathf.Clamp(pageIndex * pageSize, 0, sorted.Count);
            int count = Mathf.Clamp(pageSize, 0, sorted.Count - start);
            return sorted.GetRange(start, count);
        }

        private static int Compare(SaleRecord a, SaleRecord b, HistorySortMode mode)
        {
            switch (mode)
            {
                case HistorySortMode.Profit:
                    return a.Profit.CompareTo(b.Profit);
                case HistorySortMode.Model:
                    return string.Compare(a.modelNameSnapshot, b.modelNameSnapshot, System.StringComparison.OrdinalIgnoreCase);
                case HistorySortMode.MeasuredHorsePower:
                    return a.dynoHorsePower.CompareTo(b.dynoHorsePower);
                case HistorySortMode.Date:
                default:
                    return string.Compare(a.saleDateUtc, b.saleDateUtc, System.StringComparison.Ordinal);
            }
        }

        public SalesStatistics GetStatistics()
        {
            var stats = new SalesStatistics();
            var records = AllRecords;
            stats.totalSalesCount = records.Count;
            if (records.Count == 0)
            {
                return stats;
            }

            var modelCounts = new Dictionary<string, int>();

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                stats.totalProfit += record.Profit;

                if (stats.bestSale == null || record.Profit > stats.bestSale.Profit)
                {
                    stats.bestSale = record;
                }

                if (record.hasDynoResult)
                {
                    stats.highestMeasuredHorsePower = Mathf.Max(stats.highestMeasuredHorsePower, record.dynoHorsePower);
                    stats.highestMeasuredTorqueNm = Mathf.Max(stats.highestMeasuredTorqueNm, record.dynoTorqueNm);
                }

                string model = string.IsNullOrEmpty(record.modelNameSnapshot) ? "-" : record.modelNameSnapshot;
                modelCounts.TryGetValue(model, out int count);
                modelCounts[model] = count + 1;
            }

            foreach (var kvp in modelCounts)
            {
                if (kvp.Value > stats.mostSoldModelCount)
                {
                    stats.mostSoldModelCount = kvp.Value;
                    stats.mostSoldModelName = kvp.Key;
                }
            }

            return stats;
        }
    }
}
