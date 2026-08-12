using RustyToRich.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RustyToRich.UI
{
    /// <summary>
    /// Hilfsmethoden zum rein programmatischen Aufbau der uGUI-Hierarchie zur Laufzeit.
    /// Damit ist keine handgepflegte Szenen-/Prefab-Verkabelung für die MVP-UI nötig –
    /// <see cref="GameUIBootstrap"/> ruft diese Methoden beim Start der Game-Szene auf.
    /// </summary>
    public static class UIFactory
    {
        public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas CreateCanvas(string name)
        {
            var canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            return canvas;
        }

        /// <summary>Reines Layout-Element ohne sichtbare Grafik (blockiert keine Klicks).</summary>
        public static RectTransform CreateContainer(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return rt;
        }

        public static Text CreateText(Transform parent, string text, int fontSize, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var t = go.GetComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = alignment;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;

            return t;
        }

        public static Button CreateButton(Transform parent, string label, Color backgroundColor, Color textColor,
            int fontSize = 28)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = backgroundColor;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(go.transform, label, fontSize, textColor, TextAnchor.MiddleCenter);
            StretchFull(text.rectTransform);

            return button;
        }

        /// <summary>Erzeugt eine scrollbare, sich vertikal automatisch füllende Liste (füllt <paramref name="parent"/> vollständig aus).</summary>
        public static ScrollRect CreateScrollList(Transform parent, out RectTransform content)
        {
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.SetParent(parent, false);
            StretchFull(scrollRt);

            var viewportRt = CreateContainer(scrollRt, "Viewport");
            StretchFull(viewportRt);
            viewportRt.gameObject.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content", typeof(RectTransform));
            content = (RectTransform)contentGo.transform;
            content.SetParent(viewportRt, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;

            var layoutGroup = contentGo.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(16, 16, 16, 16);
            layoutGroup.spacing = 12;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandWidth = true;
            // childControlHeight muss true sein, damit die Zeilen tatsächlich auf die Höhe
            // ihres eigenen LayoutElement.preferredHeight gesetzt werden (sonst behalten sie
            // ihre Default-Höhe, obwohl die Content-Gesamthöhe schon korrekt berechnet wird).
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandHeight = false;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRt;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            return scrollRect;
        }

        public static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Color GetConditionColor(CarCondition condition)
        {
            switch (condition)
            {
                case CarCondition.Schrottreif: return new Color(0.82f, 0.35f, 0.3f);
                case CarCondition.Gebraucht: return new Color(0.85f, 0.65f, 0.25f);
                case CarCondition.Gepflegt: return new Color(0.75f, 0.78f, 0.35f);
                case CarCondition.TopZustand: return new Color(0.45f, 0.75f, 0.4f);
                case CarCondition.Neuwertig: return new Color(0.35f, 0.75f, 0.85f);
                default: return Color.white;
            }
        }

        /// <summary>Deutliche Farbcodierung pro Auto-Status, z. B. für die Garage-Liste.</summary>
        public static Color GetStatusColor(CarStatus status)
        {
            switch (status)
            {
                case CarStatus.InGarage: return new Color(0.55f, 0.85f, 0.55f);
                case CarStatus.BeingTuned: return new Color(0.95f, 0.75f, 0.25f);
                case CarStatus.OnDyno: return new Color(0.4f, 0.75f, 0.95f);
                case CarStatus.InAuction: return new Color(0.85f, 0.45f, 0.85f);
                case CarStatus.Sold: return new Color(0.6f, 0.6f, 0.6f);
                default: return Color.white;
            }
        }
    }
}
