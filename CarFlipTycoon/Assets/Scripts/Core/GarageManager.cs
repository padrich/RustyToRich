using System;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>
    /// Verwaltet die Stellplatz-Kapazität der Garage. Startet begrenzt (siehe
    /// <see cref="CarFlipTycoon.SaveSystem.SaveData.garageCapacity"/>) und lässt sich
    /// gegen Coins erweitern; <see cref="GrantBonusCapacity"/> steht zusätzlich für
    /// kostenlose Fortschritts-Belohnungen zur Verfügung.
    /// </summary>
    public class GarageManager : MonoBehaviour
    {
        private const int StartingCapacity = 3;
        private const int BaseExpansionCost = 500;

        public static GarageManager Instance { get; private set; }

        /// <summary>Ausgelöst, nachdem sich die Garagen-Kapazität geändert hat.</summary>
        public event Action OnCapacityChanged;

        public int Capacity => SaveManager.Instance.CurrentSave.garageCapacity;

        public int UsedSlots => SaveManager.Instance.CurrentSave.ownedCars.Count;

        public bool HasFreeSlot => UsedSlots < Capacity;

        /// <summary>Coin-Kosten für die nächste Kapazitätserweiterung um einen Stellplatz.</summary>
        public int NextExpansionCost => BaseExpansionCost * (Capacity - StartingCapacity + 1);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        /// <summary>Kauft einen zusätzlichen Stellplatz gegen Coins, sofern das Guthaben reicht.</summary>
        public bool TryExpandCapacity()
        {
            if (!EconomyManager.Instance.TrySpendCoins(NextExpansionCost))
            {
                return false;
            }

            SaveManager.Instance.CurrentSave.garageCapacity++;
            SaveManager.Instance.Save();
            OnCapacityChanged?.Invoke();
            return true;
        }

        /// <summary>Gewährt zusätzliche Stellplätze kostenlos, z. B. als Fortschritts-Belohnung.</summary>
        public void GrantBonusCapacity(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SaveManager.Instance.CurrentSave.garageCapacity += amount;
            SaveManager.Instance.Save();
            OnCapacityChanged?.Invoke();
        }
    }
}
