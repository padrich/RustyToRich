using System;
using CarFlipTycoon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CarFlipTycoon.UI
{
    /// <summary>
    /// Detailseite einer einzelnen Auto-Instanz: Kopfzeile mit Zurück-Button und Fahrzeugname,
    /// sowie die beiden klar getrennten Tuning-Bereiche Optik und Performance als Unter-Tabs.
    /// </summary>
    public class CarDetailView : MonoBehaviour
    {
        private Text _titleText;
        private RectTransform _cosmeticPanel;
        private RectTransform _performancePanel;
        private CosmeticTuningView _cosmeticView;
        private PerformanceTuningView _performanceView;
        private Button _cosmeticTabButton;
        private Button _performanceTabButton;

        public void Build(RectTransform panel, Action onBack)
        {
            var rootLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandHeight = true;

            var header = UIFactory.CreatePanel(panel, "Header", new Color(0f, 0f, 0f, 0.25f));
            var headerLayoutElement = header.gameObject.AddComponent<LayoutElement>();
            headerLayoutElement.preferredHeight = 110;
            headerLayoutElement.minHeight = 110;

            var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(16, 16, 12, 12);
            headerLayout.spacing = 16;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandHeight = true;

            var backButton = UIFactory.CreateButton(header, "< Zurück", new Color(0.3f, 0.3f, 0.35f), Color.white, 26);
            backButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 180;
            backButton.onClick.AddListener(() => onBack?.Invoke());

            _titleText = UIFactory.CreateText(header, string.Empty, 34, Color.white);
            _titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var subTabBar = UIFactory.CreatePanel(panel, "SubTabBar", new Color(0.05f, 0.05f, 0.06f, 0.95f));
            var subTabLayoutElement = subTabBar.gameObject.AddComponent<LayoutElement>();
            subTabLayoutElement.preferredHeight = 100;
            subTabLayoutElement.minHeight = 100;

            var subTabLayout = subTabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            subTabLayout.padding = new RectOffset(12, 12, 10, 10);
            subTabLayout.spacing = 12;
            subTabLayout.childControlWidth = true;
            subTabLayout.childForceExpandWidth = true;
            subTabLayout.childControlHeight = true;
            subTabLayout.childForceExpandHeight = true;

            _cosmeticTabButton = UIFactory.CreateButton(subTabBar, "Optik", new Color(0.25f, 0.3f, 0.4f), Color.white, 28);
            _performanceTabButton = UIFactory.CreateButton(subTabBar, "Performance", new Color(0.25f, 0.3f, 0.4f), Color.white, 28);

            var body = UIFactory.CreateContainer(panel, "DetailBody");
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            _cosmeticPanel = UIFactory.CreatePanel(body, "CosmeticPanel", new Color(0.12f, 0.13f, 0.16f, 1f));
            UIFactory.StretchFull(_cosmeticPanel);
            _cosmeticView = _cosmeticPanel.gameObject.AddComponent<CosmeticTuningView>();
            _cosmeticView.Build(_cosmeticPanel);

            _performancePanel = UIFactory.CreatePanel(body, "PerformancePanel", new Color(0.12f, 0.13f, 0.16f, 1f));
            UIFactory.StretchFull(_performancePanel);
            _performanceView = _performancePanel.gameObject.AddComponent<PerformanceTuningView>();
            _performanceView.Build(_performancePanel);

            _cosmeticTabButton.onClick.AddListener(() => ShowSubTab(true));
            _performanceTabButton.onClick.AddListener(() => ShowSubTab(false));

            ShowSubTab(true);
        }

        public void Open(string carInstanceId)
        {
            var car = GameManager.Instance.GetCarInstance(carInstanceId);
            var carType = car != null ? GameManager.Instance.GetCarType(car.carTypeId) : null;
            _titleText.text = carType != null ? carType.modelName : "Auto";

            _cosmeticView.SetCar(carInstanceId);
            _performanceView.SetCar(carInstanceId);
            ShowSubTab(true);
        }

        private void ShowSubTab(bool showCosmetic)
        {
            _cosmeticPanel.gameObject.SetActive(showCosmetic);
            _performancePanel.gameObject.SetActive(!showCosmetic);
        }
    }
}
