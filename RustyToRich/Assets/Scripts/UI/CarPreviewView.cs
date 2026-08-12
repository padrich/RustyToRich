using System.Collections.Generic;
using RustyToRich.Core;
using RustyToRich.Data;
using UnityEngine;
using UnityEngine.UI;

namespace RustyToRich.UI
{
    /// <summary>
    /// Große Fahrzeug-Vorschau mit Sprite-Layering: eine Basis-Fahrzeuggrafik plus je ein
    /// Overlay-Layer pro sichtbarer Optik-Kategorie (in fester, an previewSortOrder
    /// orientierter Stapelreihenfolge). <see cref="Refresh"/> tauscht die Sprites der
    /// betroffenen Layer aus (Sprite-Swapping) und aktualisiert damit die Vorschau in Echtzeit.
    /// </summary>
    public class CarPreviewView : MonoBehaviour
    {
        // Reihenfolge = Stapelreihenfolge von unten nach oben, passend zu CosmeticPart.previewSortOrder.
        private static readonly CosmeticPartCategory[] OverlayCategories =
        {
            CosmeticPartCategory.Lackierung,
            CosmeticPartCategory.Bodykit,
            CosmeticPartCategory.Felgen,
            CosmeticPartCategory.Spoiler,
            CosmeticPartCategory.Kaefig
        };

        private RectTransform _layerRoot;
        private Image _baseImage;
        private readonly Dictionary<CosmeticPartCategory, Image> _layers = new Dictionary<CosmeticPartCategory, Image>();

        public void Build(RectTransform panel)
        {
            _layerRoot = UIFactory.CreateContainer(panel, "PreviewLayers");
            _layerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _layerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _layerRoot.pivot = new Vector2(0.5f, 0.5f);
            _layerRoot.sizeDelta = new Vector2(520, 320);
            _layerRoot.anchoredPosition = Vector2.zero;

            var baseGo = new GameObject("Base", typeof(RectTransform), typeof(Image));
            baseGo.transform.SetParent(_layerRoot, false);
            _baseImage = baseGo.GetComponent<Image>();
            _baseImage.preserveAspect = true;
            _baseImage.raycastTarget = false;
            UIFactory.StretchFull((RectTransform)baseGo.transform);

            for (int i = 0; i < OverlayCategories.Length; i++)
            {
                var category = OverlayCategories[i];
                var layerGo = new GameObject("Layer_" + category, typeof(RectTransform), typeof(Image));
                layerGo.transform.SetParent(_layerRoot, false);

                var image = layerGo.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.sprite = null;
                image.color = new Color(1f, 1f, 1f, 0f);
                UIFactory.StretchFull((RectTransform)layerGo.transform);

                _layers[category] = image;
            }
        }

        /// <summary>Aktualisiert Basis-Grafik, alle Overlay-Layer und den Höhen-Versatz für das übergebene Auto.</summary>
        public void Refresh(CarInstance car)
        {
            if (car == null)
            {
                _baseImage.sprite = null;
                _baseImage.color = new Color(1f, 1f, 1f, 0.15f);
                foreach (var layer in _layers.Values)
                {
                    layer.sprite = null;
                    layer.color = new Color(1f, 1f, 1f, 0f);
                }

                return;
            }

            var carType = GameManager.Instance.GetCarType(car.carTypeId);
            _baseImage.sprite = carType != null ? carType.icon : null;
            _baseImage.color = _baseImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);

            for (int i = 0; i < OverlayCategories.Length; i++)
            {
                var category = OverlayCategories[i];
                var part = CosmeticTuningManager.Instance.GetInstalledPart(car, category);
                var image = _layers[category];
                image.sprite = part != null ? part.previewOverlaySprite : null;
                image.color = image.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            }

            // Solange keine echten Art-Assets vorhanden sind, macht ein kleiner vertikaler
            // Versatz die verbaute Höhen-Stufe trotzdem sichtbar (Lift = höher, Stance = tiefer).
            var heightPart = CosmeticTuningManager.Instance.GetInstalledPart(car, CosmeticPartCategory.Hoehe);
            _layerRoot.anchoredPosition = new Vector2(0f, HeightOffset(heightPart));
        }

        private static float HeightOffset(CosmeticPart heightPart)
        {
            if (heightPart == null)
            {
                return 0f;
            }

            switch (heightPart.heightTier)
            {
                case RideHeightTier.OffroadLift: return 20f;
                case RideHeightTier.Serienhoehe: return 0f;
                case RideHeightTier.LeichtTiefer: return -10f;
                case RideHeightTier.StarkTieferStance: return -20f;
                default: return 0f;
            }
        }
    }
}
