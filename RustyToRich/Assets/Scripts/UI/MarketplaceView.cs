using RustyToRich.Core;
using RustyToRich.Data;
using UnityEngine;
using UnityEngine.UI;

namespace RustyToRich.UI
{
    /// <summary>
    /// Marktplatz-Ansicht: Liste der aktuell wechselnden Auto-Angebote (Modell, Fahrzeugklasse,
    /// Zustand, Preis) inkl. Kauf-Button. Baut sich bei jeder Angebots-Änderung neu auf.
    /// </summary>
    public class MarketplaceView : MonoBehaviour
    {
        private RectTransform _content;

        public void Build(RectTransform panel)
        {
            var rootLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandHeight = true;

            var refreshBar = UIFactory.CreatePanel(panel, "RefreshBar", new Color(0f, 0f, 0f, 0.2f));
            var refreshBarLayoutElement = refreshBar.gameObject.AddComponent<LayoutElement>();
            refreshBarLayoutElement.preferredHeight = 96;
            refreshBarLayoutElement.minHeight = 96;

            var refreshButtonHolder = UIFactory.CreateContainer(refreshBar, "RefreshButtonHolder");
            refreshButtonHolder.anchorMin = new Vector2(0f, 0f);
            refreshButtonHolder.anchorMax = new Vector2(1f, 1f);
            refreshButtonHolder.offsetMin = new Vector2(16f, 10f);
            refreshButtonHolder.offsetMax = new Vector2(-16f, -10f);

            var refreshButton = UIFactory.CreateIconButton(refreshButtonHolder, IconFactory.RefreshIcon(Color.white),
                "Angebote auffrischen (Ad)", new Color(0.42f, 0.32f, 0.58f), Color.white, 24, 34, 20);
            UIFactory.StretchFull(refreshButton.GetComponent<RectTransform>());
            refreshButton.onClick.AddListener(RefreshOffersNow);

            var listContainer = UIFactory.CreateContainer(panel, "ListContainer");
            listContainer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            UIFactory.CreateScrollList(listContainer, out _content);

            MarketplaceManager.Instance.OnOffersChanged += Rebuild;
            Rebuild();
        }

        private void RefreshOffersNow()
        {
            AdManager.Instance.ShowRewardedAd(RewardedAdPurpose.RefreshMarketplaceNow, success =>
            {
                if (success)
                {
                    MarketplaceManager.Instance.RefreshAllOffersNow();
                }
            });
        }

        private void OnDestroy()
        {
            if (MarketplaceManager.Instance != null)
            {
                MarketplaceManager.Instance.OnOffersChanged -= Rebuild;
            }
        }

        private void Rebuild()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.GetChild(i).gameObject);
            }

            var offers = MarketplaceManager.Instance.CurrentOffers;
            for (int i = 0; i < offers.Count; i++)
            {
                CreateOfferRow(offers[i]);
            }
        }

        private void CreateOfferRow(MarketOffer offer)
        {
            var carType = GameManager.Instance.GetCarType(offer.carTypeId);
            string modelName = carType != null ? carType.modelName : offer.carTypeId;
            string className = carType != null ? carType.carClass.GetDisplayName() : "-";

            var row = UIFactory.CreateCard(_content, "Offer_" + offer.offerId, UIFactory.PanelColor, 22);
            var rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 176;
            rowLayoutElement.minHeight = 176;

            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(18, 18, 14, 14);
            rowLayout.spacing = 16;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandHeight = true;

            // Farbiger Avatar-Kreis mit Auto-Icon statt reinem Text – gibt jeder Zeile sofort
            // eine visuelle Ankerform, an der sich das Auge orientieren kann.
            var avatar = UIFactory.CreateRoundedPanel(row, "Avatar",
                new Color(UIFactory.RustColor.r, UIFactory.RustColor.g, UIFactory.RustColor.b, 0.22f), 30);
            avatar.gameObject.AddComponent<LayoutElement>().preferredWidth = 92;
            var avatarIcon = UIFactory.CreateIcon(avatar, IconFactory.CarIcon(UIFactory.RustBrightColor), 56f);
            avatarIcon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            avatarIcon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            avatarIcon.rectTransform.anchoredPosition = Vector2.zero;

            var infoColumn = UIFactory.CreateContainer(row, "Info");
            var infoLayoutElement = infoColumn.gameObject.AddComponent<LayoutElement>();
            infoLayoutElement.flexibleWidth = 1;

            var infoLayout = infoColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.childControlWidth = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandHeight = false;
            infoLayout.spacing = 6;

            var modelText = UIFactory.CreateText(infoColumn, modelName, 32, UIFactory.TextColor);
            modelText.fontStyle = FontStyle.Bold;
            modelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;

            var classText = UIFactory.CreateText(infoColumn, className, 22, UIFactory.TextDimColor);
            classText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;

            var badgeRow = UIFactory.CreateContainer(infoColumn, "BadgeRow");
            badgeRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;
            var badgeRowLayout = badgeRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            badgeRowLayout.childControlWidth = true;
            badgeRowLayout.childForceExpandWidth = false;
            badgeRowLayout.childControlHeight = true;
            badgeRowLayout.childForceExpandHeight = true;

            var conditionBadge = UIFactory.CreateBadge(badgeRow, offer.condition.GetDisplayName(),
                new Color(1f, 1f, 1f, 0.08f), UIFactory.GetConditionColor(offer.condition), 20, 10);
            conditionBadge.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;

            var priceBadge = UIFactory.CreateBadge(row, $"{offer.price:N0}", UIFactory.GoldColor,
                new Color(0.12f, 0.09f, 0.02f), 26, 16);
            priceBadge.gameObject.AddComponent<LayoutElement>().preferredWidth = 150;
            var priceButton = priceBadge.gameObject.AddComponent<Button>();
            priceButton.targetGraphic = priceBadge.GetComponent<Image>();
            string offerId = offer.offerId;
            priceButton.onClick.AddListener(() => Buy(offerId));
        }

        private void Buy(string offerId)
        {
            if (!MarketplaceManager.Instance.TryBuyOffer(offerId, out string error))
            {
                Debug.LogWarning($"[MarketplaceView] Kauf fehlgeschlagen: {error}");
            }
        }
    }
}
