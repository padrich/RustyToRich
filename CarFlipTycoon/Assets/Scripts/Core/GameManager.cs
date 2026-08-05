using System.Collections.Generic;
using CarFlipTycoon.Data;
using CarFlipTycoon.SaveSystem;
using CarFlipTycoon.Utility;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>
    /// Zentraler Einstiegspunkt und Orchestrator der Core-Manager. Erzeugt sich selbst
    /// (samt Save-/Economy-/AdManager) automatisch vor dem Laden der ersten Szene,
    /// sodass keine manuelle Szenen-Verkabelung nötig ist.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private const string CarTypeDatabaseResourcePath = "CarTypeDatabase";

        public static GameManager Instance { get; private set; }

        private CarTypeDatabase _carTypeDatabase;

        public IReadOnlyList<CarInstance> OwnedCars => SaveManager.Instance.CurrentSave.ownedCars;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
            {
                return;
            }

            var root = new GameObject("~GameCore");
            DontDestroyOnLoad(root);

            // Reihenfolge ist wichtig: SaveManager muss vor allen anderen existieren,
            // da EconomyManager/GameManager direkt auf SaveManager.Instance zugreifen.
            root.AddComponent<SaveManager>();
            root.AddComponent<EconomyManager>();
            root.AddComponent<AdManager>();
            root.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _carTypeDatabase = Resources.Load<CarTypeDatabase>(CarTypeDatabaseResourcePath);
        }

        public CarType GetCarType(string carTypeId)
        {
            return _carTypeDatabase != null ? _carTypeDatabase.GetById(carTypeId) : null;
        }

        public CarInstance AddCarToGarage(CarType carType, int purchasePrice)
        {
            if (carType == null)
            {
                Debug.LogError("[GameManager] AddCarToGarage: carType ist null.");
                return null;
            }

            var instance = new CarInstance(carType.CarTypeId, purchasePrice);
            SaveManager.Instance.CurrentSave.ownedCars.Add(instance);
            SaveManager.Instance.Save();
            return instance;
        }

        public CarInstance GetCarInstance(string instanceId)
        {
            var ownedCars = SaveManager.Instance.CurrentSave.ownedCars;
            for (int i = 0; i < ownedCars.Count; i++)
            {
                if (ownedCars[i].instanceId == instanceId)
                {
                    return ownedCars[i];
                }
            }

            return null;
        }

        public bool SellCar(string instanceId, int salePrice, SaleType saleType = SaleType.DirectSale)
        {
            var save = SaveManager.Instance.CurrentSave;
            var instance = GetCarInstance(instanceId);
            if (instance == null)
            {
                Debug.LogWarning($"[GameManager] SellCar: Keine Auto-Instanz mit ID {instanceId} gefunden.");
                return false;
            }

            instance.status = CarStatus.Sold;
            save.ownedCars.Remove(instance);

            var carType = GetCarType(instance.carTypeId);
            save.salesHistory.Add(new SaleRecord
            {
                id = IdFactory.NewId(),
                carInstanceId = instance.instanceId,
                carTypeId = instance.carTypeId,
                modelNameSnapshot = carType != null ? carType.modelName : instance.carTypeId,
                salePrice = salePrice,
                saleDateUtc = IdFactory.NowUtcIso(),
                saleType = saleType
            });

            EconomyManager.Instance.AddCoins(salePrice);
            SaveManager.Instance.Save();
            return true;
        }
    }
}
