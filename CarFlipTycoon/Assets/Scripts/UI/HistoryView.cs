using CarFlipTycoon.Core;
using CarFlipTycoon.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CarFlipTycoon.UI
{
    /// <summary>
    /// Verkaufshistorie-Tab: Statistik-Übersicht (Gesamtgewinn, bester Verkauf, Bestwerte,
    /// meistverkauftes Modell) sowie eine sortierbare, paginierte Liste aller abgeschlossenen
    /// Verkäufe. Liest aus <see cref="SalesHistoryManager"/>, das direkt auf die dauerhaft im
    /// Spielstand gespeicherte Historie zugreift.
    /// </summary>
    public class HistoryView : MonoBehaviour
    {
        private const int PageSize = 15;

        private static readonly Color TabIdleColor = new Color(0.2f, 0.22f, 0.28f);
        private static readonly Color TabSelectedColor = new Color(0.3f, 0.55f, 0.85f);

        private Text _statsText;
        private RectTransform _sortBar;
        private RectTransform _listContainer;
        private Text _pageText;
        private Button _prevPageButton;
        private Button _nextPageButton;

        private HistorySortMode _sortMode = HistorySortMode.Date;
        private bool _descending = true;
        private int _pageIndex;

        public void Build(RectTransform panel)
        {
            var rootLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(16, 16, 12, 12);
            rootLayout.spacing = 10;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandHeight = true;

            var statsPanel = UIFactory.CreatePanel(panel, "StatsPanel", new Color(0f, 0f, 0f, 0.2f));
            statsPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 190;
            _statsText = UIFactory.CreateText(statsPanel, string.Empty, 20, new Color(0.85f, 0.9f, 0.85f), TextAnchor.UpperLeft);
            var statsTextRect = _statsText.rectTransform;
            statsTextRect.anchorMin = Vector2.zero;
            statsTextRect.anchorMax = Vector2.one;
            statsTextRect.offsetMin = new Vector2(16f, 10f);
            statsTextRect.offsetMax = new Vector2(-16f, -10f);

            _sortBar = UIFactory.CreateContainer(panel, "SortBar");
            _sortBar.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            var sortBarLayout = _sortBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            sortBarLayout.spacing = 6;
            sortBarLayout.childControlWidth = true;
            sortBarLayout.childForceExpandWidth = true;
            sortBarLayout.childControlHeight = true;
            sortBarLayout.childForceExpandHeight = true;

            CreateSortButton(HistorySortMode.Date, "Datum");
            CreateSortButton(HistorySortMode.Profit, "Gewinn");
            CreateSortButton(HistorySortMode.Model, "Modell");
            CreateSortButton(HistorySortMode.MeasuredHorsePower, "PS");

            var listHolder = UIFactory.CreateContainer(panel, "ListHolder");
            listHolder.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            UIFactory.CreateScrollList(listHolder, out _listContainer);

            var pagerBar = UIFactory.CreateContainer(panel, "PagerBar");
            pagerBar.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;
            var pagerLayout = pagerBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            pagerLayout.spacing = 10;
            pagerLayout.childControlWidth = true;
            pagerLayout.childForceExpandWidth = false;
            pagerLayout.childControlHeight = true;
            pagerLayout.childForceExpandHeight = true;

            _prevPageButton = UIFactory.CreateButton(pagerBar, "< Zurück", TabIdleColor, Color.white, 20);
            _prevPageButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;
            _prevPageButton.onClick.AddListener(() => ChangePage(-1));

            _pageText = UIFactory.CreateText(pagerBar, string.Empty, 20, Color.white, TextAnchor.MiddleCenter);
            _pageText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            _nextPageButton = UIFactory.CreateButton(pagerBar, "Weiter >", TabIdleColor, Color.white, 20);
            _nextPageButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;
            _nextPageButton.onClick.AddListener(() => ChangePage(1));

            GameManager.Instance.OnGarageChanged += RefreshAll;
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGarageChanged -= RefreshAll;
            }
        }

        private void CreateSortButton(HistorySortMode mode, string label)
        {
            var button = UIFactory.CreateButton(_sortBar, label, TabIdleColor, Color.white, 20);
            button.onClick.AddListener(() =>
            {
                if (_sortMode == mode)
                {
                    _descending = !_descending;
                }
                else
                {
                    _sortMode = mode;
                    _descending = true;
                }

                _pageIndex = 0;
                RefreshHighlight();
                RefreshList();
            });
        }

        private void RefreshHighlight()
        {
            for (int i = 0; i < _sortBar.childCount; i++)
            {
                var child = _sortBar.GetChild(i);
                var button = child.GetComponent<Button>();
                var label = child.GetComponentInChildren<Text>();
                bool isActive = label != null && SortLabelMatches(label.text, _sortMode);
                var image = button.GetComponent<Image>();
                if (isActive)
                {
                    image.color = TabSelectedColor;
                    label.text = label.text.TrimEnd('▲', '▼', ' ') + (_descending ? " ▼" : " ▲");
                }
                else
                {
                    image.color = TabIdleColor;
                    label.text = label.text.TrimEnd('▲', '▼', ' ');
                }
            }
        }

        private static bool SortLabelMatches(string label, HistorySortMode mode)
        {
            string trimmed = label.TrimEnd('▲', '▼', ' ');
            switch (mode)
            {
                case HistorySortMode.Date: return trimmed == "Datum";
                case HistorySortMode.Profit: return trimmed == "Gewinn";
                case HistorySortMode.Model: return trimmed == "Modell";
                case HistorySortMode.MeasuredHorsePower: return trimmed == "PS";
                default: return false;
            }
        }

        private void RefreshAll()
        {
            RefreshStats();
            RefreshHighlight();
            RefreshList();
        }

        private void RefreshStats()
        {
            var stats = SalesHistoryManager.Instance.GetStatistics();
            if (stats.totalSalesCount == 0)
            {
                _statsText.text = "Noch keine Verkäufe.";
                return;
            }

            string bestSaleLine = stats.bestSale != null
                ? $"Bester Verkauf: {stats.bestSale.modelNameSnapshot} (+{stats.bestSale.Profit:N0} Coins)"
                : "-";

            _statsText.text =
                $"Verkaufte Autos: {stats.totalSalesCount}\n" +
                $"Gesamtgewinn: {stats.totalProfit:N0} Coins\n" +
                $"{bestSaleLine}\n" +
                $"Höchste gemessene Leistung: {stats.highestMeasuredHorsePower:0} PS / {stats.highestMeasuredTorqueNm:0} Nm\n" +
                $"Meistverkauftes Modell: {stats.mostSoldModelName ?? "-"} ({stats.mostSoldModelCount}x)";
        }

        private void ChangePage(int delta)
        {
            int pageCount = SalesHistoryManager.Instance.GetPageCount(PageSize);
            _pageIndex = Mathf.Clamp(_pageIndex + delta, 0, pageCount - 1);
            RefreshList();
        }

        private void RefreshList()
        {
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_listContainer.GetChild(i).gameObject);
            }

            int pageCount = SalesHistoryManager.Instance.GetPageCount(PageSize);
            _pageIndex = Mathf.Clamp(_pageIndex, 0, pageCount - 1);
            _pageText.text = $"Seite {_pageIndex + 1} / {pageCount}";
            _prevPageButton.interactable = _pageIndex > 0;
            _nextPageButton.interactable = _pageIndex < pageCount - 1;

            var page = SalesHistoryManager.Instance.GetSortedPage(_sortMode, _descending, _pageIndex, PageSize);
            for (int i = 0; i < page.Count; i++)
            {
                CreateRecordRow(page[i]);
            }

            if (page.Count == 0)
            {
                var emptyText = UIFactory.CreateText(_listContainer, "Noch keine Verkäufe in der Historie.", 20,
                    new Color(0.6f, 0.6f, 0.65f));
                emptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
            }
        }

        private void CreateRecordRow(SaleRecord record)
        {
            var row = UIFactory.CreatePanel(_listContainer, "Sale_" + record.id, new Color(1f, 1f, 1f, 0.06f));
            var rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 140;
            rowLayoutElement.minHeight = 140;

            var rowLayout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset(18, 18, 10, 10);
            rowLayout.spacing = 3;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandHeight = false;

            var nameText = UIFactory.CreateText(row, record.modelNameSnapshot, 26, Color.white);
            nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;

            string saleTypeLabel = record.saleType == SaleType.Auction ? "Auktion" : "Direktverkauf";
            var infoText = UIFactory.CreateText(row,
                $"{saleTypeLabel} · Kauf: {record.purchasePrice:N0} · Tuning: {record.cosmeticTuningCost + record.performanceTuningCost:N0} · Verkauf: {record.salePrice:N0} Coins",
                19, new Color(0.75f, 0.75f, 0.8f));
            infoText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;

            string dynoLabel = record.hasDynoResult
                ? $"Geprüfte Leistung: {record.dynoHorsePower:0} PS / {record.dynoTorqueNm:0} Nm"
                : "Keine Prüfstand-Messung";
            var dynoText = UIFactory.CreateText(row, dynoLabel, 19, new Color(0.7f, 0.85f, 1f));
            dynoText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26;

            Color profitColor = record.Profit >= 0 ? new Color(0.5f, 0.85f, 0.5f) : new Color(0.9f, 0.4f, 0.35f);
            var profitText = UIFactory.CreateText(row, $"Gewinn/Verlust: {(record.Profit >= 0 ? "+" : string.Empty)}{record.Profit:N0} Coins",
                21, profitColor);
            profitText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
        }
    }
}
