using UnityEngine;
using UnityEngine.UI;

namespace RustyToRich.UI
{
    /// <summary>
    /// Baut die komplette Laufzeit-UI (Kopfzeile, Tab-Leiste, alle Haupt-Ansichten sowie die
    /// Auto-Detailseite) beim Start der Game-Szene rein aus Code auf – analog dazu, wie
    /// GameManager sich seine Core-Manager selbst erzeugt. Es ist keine manuell verkabelte
    /// Canvas-Hierarchie nötig.
    /// </summary>
    public class GameUIBootstrap : MonoBehaviour
    {
        private enum MainTab
        {
            Marketplace,
            Garage,
            Auctions,
            History
        }

        private static readonly Color TabIdleColor = new Color(0.25f, 0.3f, 0.4f);
        private static readonly Color TabSelectedColor = new Color(0.3f, 0.55f, 0.85f);

        private RectTransform _tabBarPanel;
        private RectTransform _marketplacePanel;
        private RectTransform _garagePanel;
        private RectTransform _auctionsPanel;
        private RectTransform _historyPanel;
        private RectTransform _carDetailPanel;
        private CarDetailView _carDetailView;

        private Button _marketplaceTabButton;
        private Button _garageTabButton;
        private Button _auctionsTabButton;
        private Button _historyTabButton;

        private MainTab _activeTab = MainTab.Marketplace;

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

            var bodyContainer = UIFactory.CreateContainer(root, "Body");
            bodyContainer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            _marketplacePanel = UIFactory.CreatePanel(bodyContainer, "MarketplacePanel", new Color(0.12f, 0.13f, 0.16f, 1f));
            UIFactory.StretchFull(_marketplacePanel);
            _marketplacePanel.gameObject.AddComponent<MarketplaceView>().Build(_marketplacePanel);

            _garagePanel = UIFactory.CreatePanel(bodyContainer, "GaragePanel", new Color(0.12f, 0.13f, 0.16f, 1f));
            UIFactory.StretchFull(_garagePanel);
            _garagePanel.gameObject.AddComponent<GarageView>().Build(_garagePanel, OpenCarDetail);

            _auctionsPanel = UIFactory.CreatePanel(bodyContainer, "AuctionsPanel", new Color(0.12f, 0.13f, 0.16f, 1f));
            UIFactory.StretchFull(_auctionsPanel);
            _auctionsPanel.gameObject.AddComponent<AuctionView>().Build(_auctionsPanel);

            _historyPanel = UIFactory.CreatePanel(bodyContainer, "HistoryPanel", new Color(0.12f, 0.13f, 0.16f, 1f));
            UIFactory.StretchFull(_historyPanel);
            _historyPanel.gameObject.AddComponent<HistoryView>().Build(_historyPanel);

            _carDetailPanel = UIFactory.CreatePanel(bodyContainer, "CarDetailPanel", new Color(0.12f, 0.13f, 0.16f, 1f));
            UIFactory.StretchFull(_carDetailPanel);
            _carDetailView = _carDetailPanel.gameObject.AddComponent<CarDetailView>();
            _carDetailView.Build(_carDetailPanel, CloseCarDetail);
            _carDetailPanel.gameObject.SetActive(false);

            // Tab-Leiste NACH den Panels aufgebaut, aber im Layout oberhalb von Body verankert
            // (unten im Screen, direkt über einer künftigen Banner-Ad-Zone) – daher als letztes
            // Kind von root hinzugefügt und per Transform-Reihenfolge nach unten sortiert.
            _tabBarPanel = UIFactory.CreatePanel(root, "TabBar", new Color(0.05f, 0.05f, 0.06f, 0.95f));
            var tabBarLayoutElement = _tabBarPanel.gameObject.AddComponent<LayoutElement>();
            tabBarLayoutElement.preferredHeight = 120;
            tabBarLayoutElement.minHeight = 120;

            var tabBarLayout = _tabBarPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabBarLayout.padding = new RectOffset(8, 8, 10, 10);
            tabBarLayout.spacing = 6;
            tabBarLayout.childControlWidth = true;
            tabBarLayout.childForceExpandWidth = true;
            tabBarLayout.childControlHeight = true;
            tabBarLayout.childForceExpandHeight = true;

            _marketplaceTabButton = UIFactory.CreateButton(_tabBarPanel, "Marktplatz", TabIdleColor, Color.white, 22);
            _garageTabButton = UIFactory.CreateButton(_tabBarPanel, "Garage", TabIdleColor, Color.white, 22);
            _auctionsTabButton = UIFactory.CreateButton(_tabBarPanel, "Auktionen", TabIdleColor, Color.white, 22);
            _historyTabButton = UIFactory.CreateButton(_tabBarPanel, "Historie", TabIdleColor, Color.white, 22);

            _marketplaceTabButton.onClick.AddListener(() => ShowTab(MainTab.Marketplace));
            _garageTabButton.onClick.AddListener(() => ShowTab(MainTab.Garage));
            _auctionsTabButton.onClick.AddListener(() => ShowTab(MainTab.Auctions));
            _historyTabButton.onClick.AddListener(() => ShowTab(MainTab.History));

            ShowTab(MainTab.Marketplace);
        }

        private void OpenCarDetail(string carInstanceId)
        {
            _marketplacePanel.gameObject.SetActive(false);
            _garagePanel.gameObject.SetActive(false);
            _auctionsPanel.gameObject.SetActive(false);
            _historyPanel.gameObject.SetActive(false);
            _tabBarPanel.gameObject.SetActive(false);
            _carDetailPanel.gameObject.SetActive(true);
            _carDetailView.Open(carInstanceId);
        }

        private void CloseCarDetail()
        {
            _carDetailPanel.gameObject.SetActive(false);
            _tabBarPanel.gameObject.SetActive(true);
            ShowTab(MainTab.Garage);
        }

        private void ShowTab(MainTab tab)
        {
            _activeTab = tab;
            _marketplacePanel.gameObject.SetActive(tab == MainTab.Marketplace);
            _garagePanel.gameObject.SetActive(tab == MainTab.Garage);
            _auctionsPanel.gameObject.SetActive(tab == MainTab.Auctions);
            _historyPanel.gameObject.SetActive(tab == MainTab.History);

            _marketplaceTabButton.GetComponent<Image>().color = tab == MainTab.Marketplace ? TabSelectedColor : TabIdleColor;
            _garageTabButton.GetComponent<Image>().color = tab == MainTab.Garage ? TabSelectedColor : TabIdleColor;
            _auctionsTabButton.GetComponent<Image>().color = tab == MainTab.Auctions ? TabSelectedColor : TabIdleColor;
            _historyTabButton.GetComponent<Image>().color = tab == MainTab.History ? TabSelectedColor : TabIdleColor;
        }
    }
}
