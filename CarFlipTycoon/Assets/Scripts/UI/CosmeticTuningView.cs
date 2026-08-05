using System.Collections.Generic;
using CarFlipTycoon.Core;
using CarFlipTycoon.Data;
using CarFlipTycoon.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CarFlipTycoon.UI
{
    /// <summary>
    /// Optik-Tuning-Tab der Auto-Detailseite: Fahrzeug-Vorschau mit Sprite-Layering, optionaler
    /// Stil-Filter, Kategorie-Tabs, Teileliste mit Kauf/Einbau sowie geschätzter Auktionswert.
    /// </summary>
    public class CosmeticTuningView : MonoBehaviour
    {
        private static readonly CosmeticPartCategory[] Categories =
        {
            CosmeticPartCategory.Felgen, CosmeticPartCategory.Hoehe, CosmeticPartCategory.Spoiler,
            CosmeticPartCategory.Bodykit, CosmeticPartCategory.Rennsitze, CosmeticPartCategory.Kaefig,
            CosmeticPartCategory.Lackierung
        };

        private static readonly Color TabIdleColor = new Color(0.2f, 0.22f, 0.28f);
        private static readonly Color TabSelectedColor = new Color(0.3f, 0.55f, 0.85f);

        private string _carInstanceId;
        private CosmeticPartCategory _selectedCategory = CosmeticPartCategory.Felgen;
        private StyleTag? _styleFilter;

        private CarPreviewView _preview;
        private RectTransform _styleFilterBar;
        private RectTransform _categoryTabBar;
        private RectTransform _partsListContainer;
        private Text _valueText;
        private Text _timerText;

        private readonly Dictionary<CosmeticPartCategory, Button> _categoryButtons = new Dictionary<CosmeticPartCategory, Button>();
        private readonly Dictionary<int, Button> _styleButtons = new Dictionary<int, Button>();

        private float _timerPollAccumulator;

        public void Build(RectTransform panel)
        {
            var rootLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandHeight = true;

            var previewHolder = UIFactory.CreatePanel(panel, "PreviewHolder", new Color(0f, 0f, 0f, 0.15f));
            var previewLayoutElement = previewHolder.gameObject.AddComponent<LayoutElement>();
            previewLayoutElement.preferredHeight = 400;
            previewLayoutElement.minHeight = 400;
            _preview = previewHolder.gameObject.AddComponent<CarPreviewView>();
            _preview.Build(previewHolder);

            var infoBar = UIFactory.CreatePanel(panel, "InfoBar", new Color(0f, 0f, 0f, 0.2f));
            var infoBarLayoutElement = infoBar.gameObject.AddComponent<LayoutElement>();
            infoBarLayoutElement.preferredHeight = 80;
            infoBarLayoutElement.minHeight = 80;
            var infoBarLayout = infoBar.gameObject.AddComponent<VerticalLayoutGroup>();
            infoBarLayout.padding = new RectOffset(20, 20, 6, 6);
            infoBarLayout.childControlWidth = true;
            infoBarLayout.childForceExpandWidth = true;
            infoBarLayout.childControlHeight = true;
            infoBarLayout.childForceExpandHeight = false;
            infoBarLayout.spacing = 2;

            _valueText = UIFactory.CreateText(infoBar, "Geschätzter Auktionswert: ~0 Coins", 26, new Color(0.6f, 0.9f, 0.6f));
            _valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;

            _timerText = UIFactory.CreateText(infoBar, string.Empty, 22, new Color(1f, 0.8f, 0.4f));
            _timerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

            _styleFilterBar = UIFactory.CreateContainer(panel, "StyleFilterBar");
            var styleFilterLayoutElement = _styleFilterBar.gameObject.AddComponent<LayoutElement>();
            styleFilterLayoutElement.preferredHeight = 76;
            styleFilterLayoutElement.minHeight = 76;
            var styleFilterLayout = _styleFilterBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            styleFilterLayout.padding = new RectOffset(12, 12, 6, 6);
            styleFilterLayout.spacing = 8;
            styleFilterLayout.childControlWidth = true;
            styleFilterLayout.childForceExpandWidth = true;
            styleFilterLayout.childControlHeight = true;
            styleFilterLayout.childForceExpandHeight = true;

            CreateStyleFilterButton(-1, "Alle");
            CreateStyleFilterButton((int)StyleTag.Offroad, "Offroad");
            CreateStyleFilterButton((int)StyleTag.Racing, "Racing");
            CreateStyleFilterButton((int)StyleTag.Street, "Street");

            _categoryTabBar = UIFactory.CreateContainer(panel, "CategoryTabBar");
            var categoryTabLayoutElement = _categoryTabBar.gameObject.AddComponent<LayoutElement>();
            categoryTabLayoutElement.preferredHeight = 72;
            categoryTabLayoutElement.minHeight = 72;
            var categoryTabLayout = _categoryTabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            categoryTabLayout.padding = new RectOffset(6, 6, 4, 4);
            categoryTabLayout.spacing = 4;
            categoryTabLayout.childControlWidth = true;
            categoryTabLayout.childForceExpandWidth = true;
            categoryTabLayout.childControlHeight = true;
            categoryTabLayout.childForceExpandHeight = true;

            for (int i = 0; i < Categories.Length; i++)
            {
                CreateCategoryTabButton(Categories[i]);
            }

            var listContainer = UIFactory.CreateContainer(panel, "PartsListContainer");
            listContainer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            UIFactory.CreateScrollList(listContainer, out _partsListContainer);

            CosmeticTuningManager.Instance.OnCosmeticPartsChanged += HandlePartsChanged;
        }

        private void OnDestroy()
        {
            if (CosmeticTuningManager.Instance != null)
            {
                CosmeticTuningManager.Instance.OnCosmeticPartsChanged -= HandlePartsChanged;
            }
        }

        private void CreateStyleFilterButton(int styleValue, string label)
        {
            var button = UIFactory.CreateButton(_styleFilterBar, label, TabIdleColor, Color.white, 22);
            _styleButtons[styleValue] = button;
            button.onClick.AddListener(() =>
            {
                _styleFilter = styleValue < 0 ? (StyleTag?)null : (StyleTag)styleValue;
                RefreshStyleFilterHighlight();
                RefreshPartsList();
            });
        }

        private void CreateCategoryTabButton(CosmeticPartCategory category)
        {
            var button = UIFactory.CreateButton(_categoryTabBar, ShortLabel(category), TabIdleColor, Color.white, 20);
            _categoryButtons[category] = button;
            button.onClick.AddListener(() =>
            {
                _selectedCategory = category;
                RefreshCategoryHighlight();
                RefreshPartsList();
            });
        }

        private static string ShortLabel(CosmeticPartCategory category)
        {
            switch (category)
            {
                case CosmeticPartCategory.Felgen: return "Felgen";
                case CosmeticPartCategory.Hoehe: return "Höhe";
                case CosmeticPartCategory.Spoiler: return "Spoiler";
                case CosmeticPartCategory.Bodykit: return "Bodykit";
                case CosmeticPartCategory.Rennsitze: return "Sitze";
                case CosmeticPartCategory.Kaefig: return "Käfig";
                case CosmeticPartCategory.Lackierung: return "Farbe";
                default: return category.ToString();
            }
        }

        public void SetCar(string carInstanceId)
        {
            _carInstanceId = carInstanceId;
            _selectedCategory = CosmeticPartCategory.Felgen;
            _styleFilter = null;
            RefreshStyleFilterHighlight();
            RefreshCategoryHighlight();
            RefreshAll();
        }

        private void Update()
        {
            if (_carInstanceId == null)
            {
                return;
            }

            _timerPollAccumulator += Time.unscaledDeltaTime;
            if (_timerPollAccumulator < 0.5f)
            {
                return;
            }

            _timerPollAccumulator = 0f;
            RefreshTimerStatus();
        }

        private void HandlePartsChanged(string carInstanceId)
        {
            if (carInstanceId == _carInstanceId)
            {
                RefreshAll();
            }
        }

        private CarInstance GetCar() => _carInstanceId != null ? GameManager.Instance.GetCarInstance(_carInstanceId) : null;

        private void RefreshAll()
        {
            _preview.Refresh(GetCar());
            RefreshValue();
            RefreshTimerStatus();
            RefreshPartsList();
        }

        private void RefreshValue()
        {
            var car = GetCar();
            _valueText.text = car != null
                ? $"Geschätzter Auktionswert: ~{CosmeticTuningManager.Instance.GetEstimatedValue(car):N0} Coins"
                : string.Empty;
        }

        private void RefreshTimerStatus()
        {
            var car = GetCar();
            if (car == null)
            {
                _timerText.text = string.Empty;
                return;
            }

            var timer = TimerManager.Instance.GetActiveTimer(car.instanceId);
            if (timer == null || timer.payloadKind != TimerPayloadKind.InstallCosmeticPart)
            {
                _timerText.text = string.Empty;
                return;
            }

            float remaining = TimerManager.Instance.GetRemainingSeconds(timer);
            _timerText.text = $"Wird eingebaut: noch {Mathf.CeilToInt(remaining)}s";
        }

        private void RefreshStyleFilterHighlight()
        {
            int selected = _styleFilter.HasValue ? (int)_styleFilter.Value : -1;
            foreach (var kvp in _styleButtons)
            {
                kvp.Value.GetComponent<Image>().color = kvp.Key == selected ? TabSelectedColor : TabIdleColor;
            }
        }

        private void RefreshCategoryHighlight()
        {
            foreach (var kvp in _categoryButtons)
            {
                kvp.Value.GetComponent<Image>().color = kvp.Key == _selectedCategory ? TabSelectedColor : TabIdleColor;
            }
        }

        private void RefreshPartsList()
        {
            for (int i = _partsListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_partsListContainer.GetChild(i).gameObject);
            }

            var parts = CosmeticTuningManager.Instance.GetPartsForCategory(_selectedCategory, _styleFilter);
            for (int i = 0; i < parts.Count; i++)
            {
                CreatePartRow(parts[i]);
            }
        }

        private void CreatePartRow(CosmeticPart part)
        {
            var car = GetCar();
            var installedPart = car != null ? CosmeticTuningManager.Instance.GetInstalledPart(car, part.category) : null;
            bool isInstalled = installedPart != null && installedPart.PartId == part.PartId;

            var activeTimer = car != null ? TimerManager.Instance.GetActiveTimer(car.instanceId) : null;
            bool isPending = activeTimer != null && activeTimer.payloadKind == TimerPayloadKind.InstallCosmeticPart
                && activeTimer.payloadPartId == part.PartId;
            bool carBusy = activeTimer != null;

            var row = UIFactory.CreatePanel(_partsListContainer, "Part_" + part.PartId,
                new Color(1f, 1f, 1f, isInstalled ? 0.14f : 0.06f));
            var rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 150;
            rowLayoutElement.minHeight = 150;

            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(18, 18, 10, 10);
            rowLayout.spacing = 14;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandHeight = true;

            var infoColumn = UIFactory.CreateContainer(row, "Info");
            infoColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var infoLayout = infoColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.childControlWidth = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandHeight = false;
            infoLayout.spacing = 3;

            var nameText = UIFactory.CreateText(infoColumn, part.partName, 27, Color.white);
            nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;

            var styleText = UIFactory.CreateText(infoColumn, StyleTagsLabel(part.styleTags), 19, new Color(0.75f, 0.75f, 0.8f));
            styleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;

            var priceText = UIFactory.CreateText(infoColumn, $"{part.price:N0} Coins", 19, new Color(0.8f, 0.85f, 0.6f));
            priceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;

            string buttonLabel;
            Color buttonColor;
            bool interactable;
            if (isInstalled)
            {
                buttonLabel = "Installiert";
                buttonColor = new Color(0.25f, 0.5f, 0.3f);
                interactable = false;
            }
            else if (isPending)
            {
                buttonLabel = "Wird\neingebaut...";
                buttonColor = new Color(0.55f, 0.45f, 0.2f);
                interactable = false;
            }
            else if (carBusy)
            {
                buttonLabel = "Werkstatt\nbelegt";
                buttonColor = new Color(0.3f, 0.3f, 0.32f);
                interactable = false;
            }
            else
            {
                buttonLabel = "Kaufen &\nEinbauen";
                buttonColor = new Color(0.2f, 0.45f, 0.7f);
                interactable = true;
            }

            var buyButton = UIFactory.CreateButton(row, buttonLabel, buttonColor, Color.white, 19);
            buyButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 190;
            buyButton.interactable = interactable;
            string partId = part.PartId;
            buyButton.onClick.AddListener(() => BuyPart(partId));
        }

        private static string StyleTagsLabel(StyleTag[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return "-";
            }

            var names = new string[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {
                names[i] = tags[i].GetDisplayName();
            }

            return string.Join(" / ", names);
        }

        private void BuyPart(string partId)
        {
            var car = GetCar();
            if (car == null)
            {
                return;
            }

            var part = CosmeticTuningManager.Instance.GetPart(partId);
            if (part == null)
            {
                return;
            }

            if (!CosmeticTuningManager.Instance.TryPurchaseAndInstall(car, part, out string error))
            {
                Debug.LogWarning($"[CosmeticTuningView] Kauf fehlgeschlagen: {error}");
                return;
            }

            RefreshPartsList();
            RefreshTimerStatus();
        }
    }
}
