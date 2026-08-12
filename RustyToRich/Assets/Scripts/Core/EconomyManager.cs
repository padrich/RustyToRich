using System;
using UnityEngine;

namespace RustyToRich.Core
{
    /// <summary>Verwaltet die Spielwährung (Coins). Änderungen werden sofort persistiert.</summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        public event Action<long> OnCoinsChanged;

        public long Coins => SaveManager.Instance.CurrentSave.coins;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public void AddCoins(long amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SaveManager.Instance.CurrentSave.coins += amount;
            SaveManager.Instance.Save();
            OnCoinsChanged?.Invoke(Coins);
        }

        public bool CanAfford(long amount) => Coins >= amount;

        public bool TrySpendCoins(long amount)
        {
            if (amount <= 0 || !CanAfford(amount))
            {
                return false;
            }

            SaveManager.Instance.CurrentSave.coins -= amount;
            SaveManager.Instance.Save();
            OnCoinsChanged?.Invoke(Coins);
            return true;
        }
    }
}
