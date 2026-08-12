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
            refreshBarLayoutElement.preferredHeight = 90;
            refreshBarLayoutElement.minHeight = 90;

            var refreshButtonHolder = UIFactory.CreateContainer(refreshBar, "RefreshButtonHolder");
            refreshButtonHolder.anchorMin = new Vector2(0f, 0f);
            refreshButtonHolder.anchorMax = new Vector2(1f, 1f);
            refreshButtonHolder.offsetMin = new Vector2(16f, 10f);
            refreshButtonHolder.offsetMax = new Vector2(-16f, -10f);

            var refreshButton = UIFactory.CreateButton(refreshButtonHolder, "Angebote auffrischen 🔄 (Ad)",
                new Color(0.35f, 0.3f, 0.6f), Color.white, 24);
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

            var row = UIFactory.CreatePanel(_content, "Offer_" + offer.offerId, new Color(1f, 1f, 1f, 0.06f));
            var rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 170;
            rowLayoutElement.minHeight = 170;

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

            var classText = UIFactory.CreateText(infoColumn, className, 24, new Color(0.8f, 0.8f, 0.8f));
            classText.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;

            var conditionText = UIFactory.CreateText(infoColumn, offer.condition.GetDisplayName(), 24,
                UIFactory.GetConditionColor(offer.condition));
            conditionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;

            var buyButton = UIFactory.CreateButton(row, $"{offer.price:N0}\nCoins", new Color(0.2f, 0.55f, 0.25f), Color.white, 26);
            buyButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;
            string offerId = offer.offerId;
            buyButton.onClick.AddListener(() => Buy(offerId));
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
