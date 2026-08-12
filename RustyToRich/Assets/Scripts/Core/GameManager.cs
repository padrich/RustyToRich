using System;
using System.Collections.Generic;
using RustyToRich.Data;
using RustyToRich.SaveSystem;
using RustyToRich.Utility;
using UnityEngine;

namespace RustyToRich.Core
{
    /// <summary>
    /// Zentraler Einstiegspunkt und Orchestrator der Core-Manager. Erzeugt sich selbst
    /// (samt Save-/Economy-/AdManager) automatisch vor dem Laden der ersten Szene,
    /// sodass keine manuelle Szenen-Verkabelung nötig ist.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private const string CarTypeDatabaseResourcePath = "CarTypeDatabase";
        private static readonly List<CarType> EmptyCarTypes = new List<CarType>();

        public static GameManager Instance { get; private set; }

        private CarTypeDatabase _carTypeDatabase;

        /// <summary>Ausgelöst, nachdem sich der Fahrzeugbestand der Garage geändert hat (Kauf/Verkauf).</summary>
        public event Action OnGarageChanged;

        public IReadOnlyList<CarInstance> OwnedCars => SaveManager.Instance.CurrentSave.ownedCars;

        public IReadOnlyList<CarType> AllCarTypes =>
            _carTypeDatabase != null ? (IReadOnlyList<CarType>)_carTypeDatabase.carTypes : EmptyCarTypes;

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
            // GameClock direkt danach, da er ebenfalls nur SaveManager.Instance braucht und alle
            // zeitgesteuerten Systeme (Timer/Marktplatz/Auktionen/Tagesbelohnung) ihn in Start() nutzen.
            root.AddComponent<SaveManager>();
            root.AddComponent<GameClock>();
            root.AddComponent<EconomyManager>();
            root.AddComponent<GarageManager>();
            root.AddComponent<AdManager>();
            root.AddComponent<GameManager>();
            // MarketplaceManager/TimerManager/Tuning-Manager greifen in Start() auf
            // GameManager.Instance zu; alle Awake()-Aufrufe der zuvor hinzugefügten
            // Komponenten laufen garantiert vor jedem Start(), daher ist die
            // Reihenfolge ab hier unkritisch.
            root.AddComponent<ProgressionManager>();
            root.AddComponent<MarketplaceManager>();
            root.AddComponent<TimerManager>();
            root.AddComponent<CosmeticTuningManager>();
            root.AddComponent<PerformanceTuningManager>();
            root.AddComponent<DynoManager>();
            root.AddComponent<AuctionManager>();
            root.AddComponent<SalesHistoryManager>();
            root.AddComponent<AchievementManager>();
            root.AddComponent<DailyRewardManager>();
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

        public CarInstance AddCarToGarage(CarType carType, int purchasePrice, CarCondition condition)
        {
            if (carType == null)
            {
                Debug.LogError("[GameManager] AddCarToGarage: carType ist null.");
                return null;
            }

            var instance = new CarInstance(carType.CarTypeId, purchasePrice, condition);
            SaveManager.Instance.CurrentSave.ownedCars.Add(instance);
            SaveManager.Instance.Save();
            OnGarageChanged?.Invoke();
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

            int cosmeticTuningCost = 0;
            if (instance.cosmeticParts != null)
            {
                for (int i = 0; i < instance.cosmeticParts.Count; i++)
                {
                    cosmeticTuningCost += instance.cosmeticParts[i].purchasePrice;
                }
            }

            int performanceTuningCost = 0;
            if (instance.performanceParts != null)
            {
                for (int i = 0; i < instance.performanceParts.Count; i++)
                {
                    performanceTuningCost += instance.performanceParts[i].purchasePrice;
                }
            }

            bool hasDynoResult = instance.lastDynoResult != null && instance.lastDynoResult.hasResult;

            save.salesHistory.Add(new SaleRecord
            {
                id = IdFactory.NewId(),
                carInstanceId = instance.instanceId,
                carTypeId = instance.carTypeId,
                modelNameSnapshot = carType != null ? carType.modelName : instance.carTypeId,
                purchasePrice = instance.purchasePrice,
                purchaseDateUtc = instance.purchaseDateUtc,
                cosmeticTuningCost = cosmeticTuningCost,
                performanceTuningCost = performanceTuningCost,
                hasDynoResult = hasDynoResult,
                dynoHorsePower = hasDynoResult ? instance.lastDynoResult.horsePower : 0f,
                dynoTorqueNm = hasDynoResult ? instance.lastDynoResult.torqueNm : 0f,
                salePrice = salePrice,
                saleDateUtc = IdFactory.NowUtcIso(),
                saleType = saleType
            });

            EconomyManager.Instance.AddCoins(salePrice);
            SaveManager.Instance.Save();
            OnGarageChanged?.Invoke();
            return true;
        }
    }
}
