using CarFlipTycoon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CarFlipTycoon.UI
{
    /// <summary>Kopfzeile: zeigt aktuellen Coin-Stand und Garagen-Belegung (belegt / maximale Kapazität).</summary>
    public class TopBarView : MonoBehaviour
    {
        private Text _coinsText;
        private Text _garageText;

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

            _coinsText = UIFactory.CreateText(panel, "Coins: 0", 38, Color.white);
            _garageText = UIFactory.CreateText(panel, "Garage: 0 / 0", 38, Color.white, TextAnchor.MiddleRight);

            EconomyManager.Instance.OnCoinsChanged += OnCoinsChanged;
            GarageManager.Instance.OnCapacityChanged += Refresh;
            GameManager.Instance.OnGarageChanged += Refresh;

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
        }

        private void OnCoinsChanged(long coins) => Refresh();

        private void Refresh()
        {
            _coinsText.text = $"Coins: {EconomyManager.Instance.Coins:N0}";
            _garageText.text = $"Garage: {GarageManager.Instance.UsedSlots} / {GarageManager.Instance.Capacity}";
        }
    }
}
