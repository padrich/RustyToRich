using CarFlipTycoon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CarFlipTycoon.UI
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
            layout.padding = new RectOffset(24, 24, 8, 8);
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;

            _coinsText = UIFactory.CreateText(panel, "Coins: 0", 36, Color.white);
            _coinsText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            _garageText = UIFactory.CreateText(panel, "Garage: 0 / 0", 32, Color.white, TextAnchor.MiddleRight);
            _garageText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            _dailyRewardButton = UIFactory.CreateButton(panel, "Tages-\nBonus 🎁", new Color(0.75f, 0.55f, 0.15f), Color.white, 18);
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
            _coinsText.text = $"Coins: {EconomyManager.Instance.Coins:N0}";
            _garageText.text = $"Garage: {GarageManager.Instance.UsedSlots} / {GarageManager.Instance.Capacity}";

            bool hasPending = DailyRewardManager.Instance.HasPendingReward;
            _dailyRewardButton.gameObject.SetActive(hasPending);
            if (hasPending)
            {
                _dailyRewardButton.GetComponentInChildren<Text>().text = $"+{DailyRewardManager.Instance.PreviewRewardAmount()}\nCoins 🎁";
            }
        }
    }
}
