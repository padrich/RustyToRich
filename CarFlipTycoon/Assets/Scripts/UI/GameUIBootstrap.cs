using UnityEngine;
using UnityEngine.UI;

namespace CarFlipTycoon.UI
{
    /// <summary>
    /// Baut die komplette Laufzeit-UI (Kopfzeile, Marktplatz, Garage, Auto-Detailseite) beim
    /// Start der Game-Szene rein aus Code auf – analog dazu, wie GameManager sich seine
    /// Core-Manager selbst erzeugt. Es ist keine manuell verkabelte Canvas-Hierarchie nötig.
    /// </summary>
    public class GameUIBootstrap : MonoBehaviour
    {
        private RectTransform _tabBarPanel;
        private RectTransform _marketplacePanel;
        private RectTransform _garagePanel;
        private RectTransform _carDetailPanel;
        private CarDetailView _carDetailView;

        private void Start()
        {
            var canvas = UIFactory.CreateCanvas("UIRoot");
            var root = (RectTransform)canvas.transform;

            var rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandHeight = true;

            var topBarPanel = UIFactory.CreatePanel(root, "TopBar", new Color(0.08f, 0.09f, 0.11f, 0.95f));
            var topBarLayoutElement = topBarPanel.gameObject.AddComponent<LayoutElement>();
            topBarLayoutElement.preferredHeight = 120;
            topBarLayoutElement.minHeight = 120;
            topBarPanel.gameObject.AddComponent<TopBarView>().Build(topBarPanel);

            _tabBarPanel = UIFactory.CreatePanel(root, "TabBar", new Color(0.05f, 0.05f, 0.06f, 0.95f));
            var tabBarLayoutElement = _tabBarPanel.gameObject.AddComponent<LayoutElement>();
            tabBarLayoutElement.preferredHeight = 110;
            tabBarLayoutElement.minHeight = 110;

            var tabBarLayout = _tabBarPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabBarLayout.padding = new RectOffset(12, 12, 12, 12);
            tabBarLayout.spacing = 12;
            tabBarLayout.childControlWidth = true;
            tabBarLayout.childForceExpandWidth = true;
            tabBarLayout.childControlHeight = true;
            tabBarLayout.childForceExpandHeight = true;

            var marketplaceTabButton = UIFactory.CreateButton(_tabBarPanel, "Marktplatz", new Color(0.25f, 0.3f, 0.4f), Color.white, 30);
            var garageTabButton = UIFactory.CreateButton(_tabBarPanel, "Garage", new Color(0.25f, 0.3f, 0.4f), Color.white, 30);

            var bodyContainer = UIFactory.CreateContainer(root, "Body");
            bodyContainer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            _marketplacePanel = UIFactory.CreatePanel(bodyContainer, "MarketplacePanel", new Color(0.12f, 0.13f, 0.16f, 1f));
            UIFactory.StretchFull(_marketplacePanel);
            _marketplacePanel.gameObject.AddComponent<MarketplaceView>().Build(_marketplacePanel);

            _garagePanel = UIFactory.CreatePanel(bodyContainer, "GaragePanel", new Color(0.12f, 0.13f, 0.16f, 1f));
            UIFactory.StretchFull(_garagePanel);
            _garagePanel.gameObject.AddComponent<GarageView>().Build(_garagePanel, OpenCarDetail);

            _carDetailPanel = UIFactory.CreatePanel(bodyContainer, "CarDetailPanel", new Color(0.12f, 0.13f, 0.16f, 1f));
            UIFactory.StretchFull(_carDetailPanel);
            _carDetailView = _carDetailPanel.gameObject.AddComponent<CarDetailView>();
            _carDetailView.Build(_carDetailPanel, CloseCarDetail);
            _carDetailPanel.gameObject.SetActive(false);

            marketplaceTabButton.onClick.AddListener(() => ShowTab(true));
            garageTabButton.onClick.AddListener(() => ShowTab(false));

            ShowTab(true);
        }

        private void OpenCarDetail(string carInstanceId)
        {
            _marketplacePanel.gameObject.SetActive(false);
            _garagePanel.gameObject.SetActive(false);
            _tabBarPanel.gameObject.SetActive(false);
            _carDetailPanel.gameObject.SetActive(true);
            _carDetailView.Open(carInstanceId);
        }

        private void CloseCarDetail()
        {
            _carDetailPanel.gameObject.SetActive(false);
            _tabBarPanel.gameObject.SetActive(true);
            ShowTab(false);
        }

        private void ShowTab(bool showMarketplace)
        {
            _marketplacePanel.gameObject.SetActive(showMarketplace);
            _garagePanel.gameObject.SetActive(!showMarketplace);
        }
    }
}
