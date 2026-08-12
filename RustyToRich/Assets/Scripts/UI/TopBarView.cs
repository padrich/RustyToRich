using RustyToRich.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RustyToRich.UI
{
    /// <summary>
    /// Kopfzeile: zeigt aktuellen Coin-Stand, Garagen-Belegung (belegt / maximale Kapazität)
    /// und – falls verfügbar – einen Button zum Abholen der täglichen Login-Belohnung.
    /// </summary>
    public class TopBarView : MonoBehaviour
    {
        private Text _coinsText;
        private Text _garageText;
        private Button _dailyRewardButton;

        public void Build(RectTransform panel)
        {
            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;

            var coinsGroup = UIFactory.CreateContainer(panel, "CoinsGroup");
            coinsGroup.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var coinsLayout = coinsGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
            coinsLayout.spacing = 8;
            coinsLayout.childAlignment = TextAnchor.MiddleLeft;
            coinsLayout.childControlWidth = false;
            coinsLayout.childControlHeight = true;
            coinsLayout.childForceExpandHeight = true;

            var coinIcon = UIFactory.CreateIcon(coinsGroup, IconFactory.CoinIcon(UIFactory.GoldColor,
                new Color(UIFactory.GoldColor.r * 0.7f, UIFactory.GoldColor.g * 0.7f, UIFactory.GoldColor.b * 0.7f)), 40f);
            coinIcon.gameObject.AddComponent<LayoutElement>().preferredWidth = 40;

            _coinsText = UIFactory.CreateText(coinsGroup, "0", 34, UIFactory.GoldColor);
            _coinsText.fontStyle = FontStyle.Bold;
            _coinsText.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;

            var garageGroup = UIFactory.CreateContainer(panel, "GarageGroup");
            garageGroup.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;
            var garageLayout = garageGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
            garageLayout.spacing = 6;
            garageLayout.childAlignment = TextAnchor.MiddleRight;
            garageLayout.childControlWidth = false;
            garageLayout.childControlHeight = true;
            garageLayout.childForceExpandHeight = true;

            var garageIcon = UIFactory.CreateIcon(garageGroup, IconFactory.CarIcon(UIFactory.TextDimColor), 32f);
            garageIcon.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;

            _garageText = UIFactory.CreateText(garageGroup, "0 / 0", 28, UIFactory.TextColor, TextAnchor.MiddleRight);
            _garageText.gameObject.AddComponent<LayoutElement>().preferredWidth = 90;

            _dailyRewardButton = UIFactory.CreateIconButton(panel,
                IconFactory.GiftIcon(UIFactory.GoldColor, new Color(0.95f, 0.95f, 0.95f)),
                "Bonus", UIFactory.RustColor, Color.white, 18, 30, 16);
            _dailyRewardButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 130;
            _dailyRewardButton.onClick.AddListener(ClaimDailyReward);

            EconomyManager.Instance.OnCoinsChanged += OnCoinsChanged;
            GarageManager.Instance.OnCapacityChanged += Refresh;
            GameManager.Instance.OnGarageChanged += Refresh;
            DailyRewardManager.Instance.OnDailyRewardClaimed += OnDailyRewardClaimed;

            Refresh();
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCoinsChanged -= OnCoinsChanged;
            }

            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnCapacityChanged -= Refresh;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGarageChanged -= Refresh;
            }

            if (DailyRewardManager.Instance != null)
            {
                DailyRewardManager.Instance.OnDailyRewardClaimed -= OnDailyRewardClaimed;
            }
        }

        private void OnCoinsChanged(long coins) => Refresh();

        private void OnDailyRewardClaimed(int coinsAwarded, int streak) => Refresh();

        private void ClaimDailyReward()
        {
            // Ohne Ad claimen; ein "verdoppeln per Ad"-Angebot ließe sich hier leicht als
            // zweiter Button ergänzen, sobald ein echtes Rewarded-Ad-SDK eingebunden ist.
            DailyRewardManager.Instance.Claim(false, (success, coins, streak) =>
            {
                if (success)
                {
                    Debug.Log($"[TopBarView] Tages-Bonus abgeholt: +{coins} Coins (Streak: {streak}).");
                }

                Refresh();
            });
        }

        private void Refresh()
        {
            _coinsText.text = $"{EconomyManager.Instance.Coins:N0}";
            _garageText.text = $"{GarageManager.Instance.UsedSlots} / {GarageManager.Instance.Capacity}";

            bool hasPending = DailyRewardManager.Instance.HasPendingReward;
            _dailyRewardButton.gameObject.SetActive(hasPending);
            if (hasPending)
            {
                _dailyRewardButton.GetComponentInChildren<Text>().text = $"+{DailyRewardManager.Instance.PreviewRewardAmount()}";
            }
        }
    }
}
