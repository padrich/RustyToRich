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

        private Image _marketplaceTabIcon;
        private Image _garageTabIcon;
        private Image _auctionsTabIcon;
        private Image _historyTabIcon;

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

            var topBarPanel = UIFactory.CreatePanel(root, "TopBar", UIFactory.PanelColor);
            var topBarLayoutElement = topBarPanel.gameObject.AddComponent<LayoutElement>();
            topBarLayoutElement.preferredHeight = 120;
            topBarLayoutElement.minHeight = 120;
            topBarPanel.gameObject.AddComponent<TopBarView>().Build(topBarPanel);

            var bodyContainer = UIFactory.CreateContainer(root, "Body");
            bodyContainer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            _marketplacePanel = UIFactory.CreatePanel(bodyContainer, "MarketplacePanel", UIFactory.BackgroundColor);
            UIFactory.StretchFull(_marketplacePanel);
            _marketplacePanel.gameObject.AddComponent<MarketplaceView>().Build(_marketplacePanel);

            _garagePanel = UIFactory.CreatePanel(bodyContainer, "GaragePanel", UIFactory.BackgroundColor);
            UIFactory.StretchFull(_garagePanel);
            _garagePanel.gameObject.AddComponent<GarageView>().Build(_garagePanel, OpenCarDetail);

            _auctionsPanel = UIFactory.CreatePanel(bodyContainer, "AuctionsPanel", UIFactory.BackgroundColor);
            UIFactory.StretchFull(_auctionsPanel);
            _auctionsPanel.gameObject.AddComponent<AuctionView>().Build(_auctionsPanel);

            _historyPanel = UIFactory.CreatePanel(bodyContainer, "HistoryPanel", UIFactory.BackgroundColor);
            UIFactory.StretchFull(_historyPanel);
            _historyPanel.gameObject.AddComponent<HistoryView>().Build(_historyPanel);

            _carDetailPanel = UIFactory.CreatePanel(bodyContainer, "CarDetailPanel", UIFactory.BackgroundColor);
            UIFactory.StretchFull(_carDetailPanel);
            _carDetailView = _carDetailPanel.gameObject.AddComponent<CarDetailView>();
            _carDetailView.Build(_carDetailPanel, CloseCarDetail);
            _carDetailPanel.gameObject.SetActive(false);

            // Tab-Leiste NACH den Panels aufgebaut, aber im Layout oberhalb von Body verankert
            // (unten im Screen, direkt über einer künftigen Banner-Ad-Zone) – daher als letztes
            // Kind von root hinzugefügt und per Transform-Reihenfolge nach unten sortiert.
            _tabBarPanel = UIFactory.CreatePanel(root, "TabBar", new Color(0.047f, 0.051f, 0.059f, 0.98f));
            var tabBarLayoutElement = _tabBarPanel.gameObject.AddComponent<LayoutElement>();
            tabBarLayoutElement.preferredHeight = 128;
            tabBarLayoutElement.minHeight = 128;

            var tabBarTopLine = UIFactory.CreatePanel(_tabBarPanel, "TopLine", UIFactory.BorderColor);
            tabBarTopLine.anchorMin = new Vector2(0f, 1f);
            tabBarTopLine.anchorMax = new Vector2(1f, 1f);
            tabBarTopLine.pivot = new Vector2(0.5f, 1f);
            tabBarTopLine.sizeDelta = new Vector2(0f, 2f);
            tabBarTopLine.anchoredPosition = Vector2.zero;

            var tabBarLayout = _tabBarPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabBarLayout.padding = new RectOffset(10, 10, 8, 8);
            tabBarLayout.spacing = 4;
            tabBarLayout.childControlWidth = true;
            tabBarLayout.childForceExpandWidth = true;
            tabBarLayout.childControlHeight = true;
            tabBarLayout.childForceExpandHeight = true;

            _marketplaceTabButton = UIFactory.CreateTabButton(_tabBarPanel, IconFactory.TagIcon(Color.white),
                "Markt", out _marketplaceTabIcon);
            _garageTabButton = UIFactory.CreateTabButton(_tabBarPanel, IconFactory.CarIcon(Color.white),
                "Garage", out _garageTabIcon);
            _auctionsTabButton = UIFactory.CreateTabButton(_tabBarPanel, IconFactory.GavelIcon(Color.white),
                "Auktionen", out _auctionsTabIcon);
            _historyTabButton = UIFactory.CreateTabButton(_tabBarPanel, IconFactory.ClockIcon(Color.white),
                "Historie", out _historyTabIcon);

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

            SetTabState(_marketplaceTabButton, _marketplaceTabIcon, tab == MainTab.Marketplace);
            SetTabState(_garageTabButton, _garageTabIcon, tab == MainTab.Garage);
            SetTabState(_auctionsTabButton, _auctionsTabIcon, tab == MainTab.Auctions);
            SetTabState(_historyTabButton, _historyTabIcon, tab == MainTab.History);
        }

        private static void SetTabState(Button tabButton, Image tabIcon, bool isSelected)
        {
            tabButton.GetComponent<Image>().color = isSelected
                ? new Color(UIFactory.RustColor.r, UIFactory.RustColor.g, UIFactory.RustColor.b, 0.28f)
                : new Color(0f, 0f, 0f, 0f);
            tabIcon.color = isSelected ? UIFactory.GoldColor : UIFactory.TextDimColor;

            var label = tabButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = isSelected ? UIFactory.TextColor : UIFactory.TextDimColor;
                label.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
            }
        }
    }
}
