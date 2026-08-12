using System.Collections.Generic;
using System.Text;
using RustyToRich.Core;
using RustyToRich.Data;
using RustyToRich.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace RustyToRich.UI
{
    /// <summary>
    /// Performance-Tuning-Tab der Auto-Detailseite: Kategorie-Liste mit den verfügbaren
    /// Stufen/Varianten, Kompatibilitäts-Warnungen/-Hinweise sowie eine live berechnete
    /// PS/Nm-Schätzung (grobe Vorschau vor dem tatsächlichen Prüfstand-Test).
    /// </summary>
    public class PerformanceTuningView : MonoBehaviour
    {
        private static readonly PerformancePartCategory[] Categories =
        {
            PerformancePartCategory.Ansaugung, PerformancePartCategory.Turbolader, PerformancePartCategory.Kompressor,
            PerformancePartCategory.Ladeluftkuehler, PerformancePartCategory.Auspuffanlage,
            PerformancePartCategory.Faecherkruemmer, PerformancePartCategory.Nockenwellen, PerformancePartCategory.Chiptuning
        };

        private static readonly Color TabIdleColor = new Color(0.2f, 0.22f, 0.28f);
        private static readonly Color TabSelectedColor = new Color(0.3f, 0.55f, 0.85f);

        private string _carInstanceId;
        private PerformancePartCategory _selectedCategory = PerformancePartCategory.Ansaugung;

        private RectTransform _categoryTabBar;
        private RectTransform _partsListContainer;
        private Text _statsText;
        private TimerProgressView _timerProgress;

        private readonly Dictionary<PerformancePartCategory, Button> _categoryButtons = new Dictionary<PerformancePartCategory, Button>();

        public void Build(RectTransform panel)
        {
            var rootLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandHeight = true;

            var infoBar = UIFactory.CreatePanel(panel, "InfoBar", new Color(0f, 0f, 0f, 0.2f));
            var infoBarLayoutElement = infoBar.gameObject.AddComponent<LayoutElement>();
            infoBarLayoutElement.preferredHeight = 160;
            infoBarLayoutElement.minHeight = 160;
            var infoBarLayout = infoBar.gameObject.AddComponent<VerticalLayoutGroup>();
            infoBarLayout.padding = new RectOffset(20, 20, 8, 8);
            infoBarLayout.childControlWidth = true;
            infoBarLayout.childForceExpandWidth = true;
            infoBarLayout.childControlHeight = true;
            infoBarLayout.childForceExpandHeight = false;
            infoBarLayout.spacing = 2;

            _statsText = UIFactory.CreateText(infoBar, "Geschätzte Leistung: ~0 PS / ~0 Nm", 26, new Color(0.6f, 0.8f, 1f));
            _statsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;

            var timerHolder = UIFactory.CreateContainer(infoBar, "TimerHolder");
            timerHolder.gameObject.AddComponent<LayoutElement>().preferredHeight = 66;
            _timerProgress = timerHolder.gameObject.AddComponent<TimerProgressView>();
            _timerProgress.Build(timerHolder);

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

            PerformanceTuningManager.Instance.OnPerformancePartsChanged += HandlePartsChanged;
        }

        private void OnDestroy()
        {
            if (PerformanceTuningManager.Instance != null)
            {
                PerformanceTuningManager.Instance.OnPerformancePartsChanged -= HandlePartsChanged;
            }
        }

        private void CreateCategoryTabButton(PerformancePartCategory category)
        {
            var button = UIFactory.CreateButton(_categoryTabBar, ShortLabel(category), TabIdleColor, Color.white, 18);
            _categoryButtons[category] = button;
            button.onClick.AddListener(() =>
            {
                _selectedCategory = category;
                RefreshCategoryHighlight();
                RefreshPartsList();
            });
        }

        private static string ShortLabel(PerformancePartCategory category)
        {
            switch (category)
            {
                case PerformancePartCategory.Ansaugung: return "Ansaug.";
                case PerformancePartCategory.Turbolader: return "Turbo";
                case PerformancePartCategory.Kompressor: return "Kompr.";
                case PerformancePartCategory.Ladeluftkuehler: return "LLK";
                case PerformancePartCategory.Auspuffanlage: return "Auspuff";
                case PerformancePartCategory.Faecherkruemmer: return "Krümmer";
                case PerformancePartCategory.Nockenwellen: return "Nocken";
                case PerformancePartCategory.Chiptuning: return "Chip";
                default: return category.ToString();
            }
        }

        public void SetCar(string carInstanceId)
        {
            _carInstanceId = carInstanceId;
            _selectedCategory = PerformancePartCategory.Ansaugung;
            RefreshCategoryHighlight();
            _timerProgress.SetCar(carInstanceId);
            RefreshAll();
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
            RefreshStats();
            RefreshPartsList();
        }

        private void RefreshStats()
        {
            var car = GetCar();
            if (car == null)
            {
                _statsText.text = string.Empty;
                return;
            }

            var (hp, nm) = PerformanceTuningManager.Instance.GetEstimatedPeakOutput(car);
            _statsText.text = $"Geschätzte Leistung: ~{hp:0} PS / ~{nm:0} Nm";
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

            var parts = PerformanceTuningManager.Instance.GetPartsForCategory(_selectedCategory);
            for (int i = 0; i < parts.Count; i++)
            {
                CreatePartRow(parts[i]);
            }
        }

        private void CreatePartRow(PerformancePart part)
        {
            var car = GetCar();
            var installedPart = car != null ? PerformanceTuningManager.Instance.GetInstalledPart(car, part.category) : null;
            bool isInstalled = installedPart != null && installedPart.PartId == part.PartId;

            var activeTimer = car != null ? TimerManager.Instance.GetActiveTimer(car.instanceId) : null;
            bool isPending = activeTimer != null && activeTimer.payloadKind == TimerPayloadKind.InstallPerformancePart
                && activeTimer.payloadId == part.PartId;
            bool carBusy = activeTimer != null;

            bool hardBlocked = car != null && PerformanceTuningManager.Instance.IsHardIncompatible(car, part, out string blockReason);
            string advisory = car != null ? PerformanceTuningManager.Instance.GetAdvisoryWarning(car, part) : null;
            string note = hardBlocked ? blockReason : (advisory ?? part.compatibilityNote);
            bool hasNote = !string.IsNullOrEmpty(note);

            var row = UIFactory.CreatePanel(_partsListContainer, "Part_" + part.PartId,
                new Color(1f, 1f, 1f, isInstalled ? 0.14f : 0.06f));
            var rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            float rowHeight = hasNote ? 210 : 160;
            rowLayoutElement.preferredHeight = rowHeight;
            rowLayoutElement.minHeight = rowHeight;

            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(18, 18, 10, 10);
            rowLayout.spacing = 14;
            rowLayout.childAlignment = TextAnchor.UpperLeft;
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

            var nameText = UIFactory.CreateText(infoColumn, $"{part.partName} (Stufe {part.stage})", 25, Color.white);
            nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;

            var effectText = UIFactory.CreateText(infoColumn, EffectSummary(part), 18, new Color(0.75f, 0.85f, 0.95f));
            effectText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;

            var priceText = UIFactory.CreateText(infoColumn, $"{part.price:N0} Coins", 18, new Color(0.8f, 0.85f, 0.6f));
            priceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;

            if (hasNote)
            {
                Color noteColor = hardBlocked ? new Color(0.9f, 0.4f, 0.35f) : new Color(0.95f, 0.75f, 0.3f);
                string prefix = hardBlocked ? "Warnung: " : "Hinweis: ";
                var noteText = UIFactory.CreateText(infoColumn, prefix + note, 17, noteColor);
                noteText.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;
            }

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
            else if (hardBlocked)
            {
                buttonLabel = "Inkompatibel";
                buttonColor = new Color(0.45f, 0.22f, 0.2f);
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

            var buyButton = UIFactory.CreateButton(row, buttonLabel, buttonColor, Color.white, 18);
            buyButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 190;
            buyButton.interactable = interactable;
            string partId = part.PartId;
            buyButton.onClick.AddListener(() => BuyPart(partId));
        }

        private static string EffectSummary(PerformancePart part)
        {
            var segments = new List<string>();

            if (part.horsePowerBonusFlat != 0f || part.horsePowerBonusPercent != 0f)
            {
                var sb = new StringBuilder("PS: ");
                if (part.horsePowerBonusFlat != 0f)
                {
                    sb.Append(part.horsePowerBonusFlat > 0 ? "+" : string.Empty).Append(part.horsePowerBonusFlat.ToString("0"));
                }

                if (part.horsePowerBonusPercent != 0f)
                {
                    if (part.horsePowerBonusFlat != 0f)
                    {
                        sb.Append(" / ");
                    }

                    sb.Append(part.horsePowerBonusPercent > 0 ? "+" : string.Empty).Append(part.horsePowerBonusPercent.ToString("P0"));
                }

                segments.Add(sb.ToString());
            }

            if (part.torqueBonusFlat != 0f || part.torqueBonusPercent != 0f)
            {
                var sb = new StringBuilder("Nm: ");
                if (part.torqueBonusFlat != 0f)
                {
                    sb.Append(part.torqueBonusFlat > 0 ? "+" : string.Empty).Append(part.torqueBonusFlat.ToString("0"));
                }

                if (part.torqueBonusPercent != 0f)
                {
                    if (part.torqueBonusFlat != 0f)
                    {
                        sb.Append(" / ");
                    }

                    sb.Append(part.torqueBonusPercent > 0 ? "+" : string.Empty).Append(part.torqueBonusPercent.ToString("P0"));
                }

                segments.Add(sb.ToString());
            }

            if (part.rpmShift != 0)
            {
                segments.Add($"RPM {(part.rpmShift > 0 ? "+" : string.Empty)}{part.rpmShift}");
            }

            if (part.dynoStabilityBonus > 0f)
            {
                segments.Add($"Prüfstand-Stabilität +{part.dynoStabilityBonus:P0}");
            }

            return segments.Count > 0 ? string.Join("   ·   ", segments) : "-";
        }

        private void BuyPart(string partId)
        {
            var car = GetCar();
            if (car == null)
            {
                return;
            }

            var part = PerformanceTuningManager.Instance.GetPart(partId);
            if (part == null)
            {
                return;
            }

            if (!PerformanceTuningManager.Instance.TryPurchaseAndInstall(car, part, out string error))
            {
                Debug.LogWarning($"[PerformanceTuningView] Kauf fehlgeschlagen: {error}");
                return;
            }

            RefreshPartsList();
        }
    }
}
