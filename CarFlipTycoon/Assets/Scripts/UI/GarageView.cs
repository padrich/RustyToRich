using System;
using CarFlipTycoon.Core;
using CarFlipTycoon.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CarFlipTycoon.UI
{
    /// <summary>
    /// Garage-Ansicht: Liste aller aktuell im Besitz befindlichen Autos mit Status-Anzeige
    /// pro Auto, ein Button je Auto für die Tuning-Detailseite, sowie ein Button, um die
    /// Garagen-Kapazität gegen Coins zu erweitern.
    /// </summary>
    public class GarageView : MonoBehaviour
    {
        private RectTransform _content;
        private Button _expandButton;
        private Text _expandButtonText;
        private Action<string> _onOpenCarDetail;

        public void Build(RectTransform panel, Action<string> onOpenCarDetail)
        {
            _onOpenCarDetail = onOpenCarDetail;

            var rootLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandHeight = true;

            var expandBar = UIFactory.CreatePanel(panel, "ExpandBar", new Color(0f, 0f, 0f, 0.2f));
            var expandBarLayoutElement = expandBar.gameObject.AddComponent<LayoutElement>();
            expandBarLayoutElement.preferredHeight = 110;
            expandBarLayoutElement.minHeight = 110;

            var expandButtonHolder = UIFactory.CreateContainer(expandBar, "ExpandButtonHolder");
            expandButtonHolder.anchorMin = new Vector2(0f, 0f);
            expandButtonHolder.anchorMax = new Vector2(1f, 1f);
            expandButtonHolder.offsetMin = new Vector2(16f, 12f);
            expandButtonHolder.offsetMax = new Vector2(-16f, -12f);

            _expandButton = UIFactory.CreateButton(expandButtonHolder, "Garage erweitern", new Color(0.2f, 0.4f, 0.7f), Color.white, 28);
            UIFactory.StretchFull(_expandButton.GetComponent<RectTransform>());
            _expandButtonText = _expandButton.GetComponentInChildren<Text>();
            _expandButton.onClick.AddListener(Expand);

            var scrollContainer = UIFactory.CreateContainer(panel, "ScrollContainer");
            scrollContainer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            UIFactory.CreateScrollList(scrollContainer, out _content);

            GameManager.Instance.OnGarageChanged += Rebuild;
            GarageManager.Instance.OnCapacityChanged += Rebuild;
            TimerManager.Instance.OnTimersChanged += Rebuild;
            EconomyManager.Instance.OnCoinsChanged += OnCoinsChanged;

            Rebuild();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGarageChanged -= Rebuild;
            }

            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnCapacityChanged -= Rebuild;
            }

            if (TimerManager.Instance != null)
            {
                TimerManager.Instance.OnTimersChanged -= Rebuild;
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCoinsChanged -= OnCoinsChanged;
            }
        }

        private void OnCoinsChanged(long coins) => RefreshExpandButton();

        private void Expand()
        {
            if (!GarageManager.Instance.TryExpandCapacity())
            {
                Debug.LogWarning("[GarageView] Garagen-Erweiterung fehlgeschlagen: nicht genug Coins.");
            }
        }

        private void RefreshExpandButton()
        {
            int cost = GarageManager.Instance.NextExpansionCost;
            _expandButtonText.text = $"Garage erweitern ({cost:N0} Coins)";
            _expandButton.interactable = EconomyManager.Instance.CanAfford(cost);
        }

        private void Rebuild()
        {
            RefreshExpandButton();

            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.GetChild(i).gameObject);
            }

            var ownedCars = GameManager.Instance.OwnedCars;
            for (int i = 0; i < ownedCars.Count; i++)
            {
                CreateCarRow(ownedCars[i]);
            }
        }

        private void CreateCarRow(CarInstance car)
        {
            var carType = GameManager.Instance.GetCarType(car.carTypeId);
            string modelName = carType != null ? carType.modelName : car.carTypeId;
            string className = carType != null ? carType.carClass.GetDisplayName() : "-";

            var row = UIFactory.CreatePanel(_content, "Car_" + car.instanceId, new Color(1f, 1f, 1f, 0.06f));
            var rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 260;
            rowLayoutElement.minHeight = 260;

            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(20, 20, 12, 12);
            rowLayout.spacing = 16;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandHeight = true;

            var infoColumn = UIFactory.CreateContainer(row, "Info");
            var infoLayoutElement = infoColumn.gameObject.AddComponent<LayoutElement>();
            infoLayoutElement.flexibleWidth = 1;

            var infoLayout = infoColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.childControlWidth = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandHeight = false;
            infoLayout.spacing = 4;

            var modelText = UIFactory.CreateText(infoColumn, modelName, 34, Color.white);
            modelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;

            var classText = UIFactory.CreateText(infoColumn, $"{className} · Kaufpreis: {car.purchasePrice:N0} Coins", 24,
                new Color(0.8f, 0.8f, 0.8f));
            classText.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;

            var conditionText = UIFactory.CreateText(infoColumn, car.condition.GetDisplayName(), 24,
                UIFactory.GetConditionColor(car.condition));
            conditionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;

            var statusText = UIFactory.CreateText(infoColumn, car.status.GetDisplayName(), 24, UIFactory.GetStatusColor(car.status));
            statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;

            var timerHolder = UIFactory.CreateContainer(infoColumn, "TimerHolder");
            timerHolder.gameObject.AddComponent<LayoutElement>().preferredHeight = 66;
            var timerProgress = timerHolder.gameObject.AddComponent<TimerProgressView>();
            timerProgress.Build(timerHolder);
            timerProgress.SetCar(car.instanceId);

            var tuneButton = UIFactory.CreateButton(row, "Tunen", new Color(0.3f, 0.35f, 0.45f), Color.white, 26);
            tuneButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 180;
            string instanceId = car.instanceId;
            tuneButton.onClick.AddListener(() => _onOpenCarDetail?.Invoke(instanceId));
        }
    }
}
