using System.Collections.Generic;
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
    ///
    /// Enthält außerdem das gemeinsame "Rusty to Rich"-Design-System (Farbpalette, abgerundete
    /// Panels/Buttons, Icon-Unterstützung), damit alle Views optisch konsistent wirken statt
    /// aus reinen Farbrechtecken zu bestehen.
    /// </summary>
    public static class UIFactory
    {
        // -------------------------------------------------------------- Farbpalette
        // Angelehnt an das Browser-Prototyp-Design: dunkler Werkstatt-Hintergrund,
        // Rost-Orange als Primärakzent, Gold als "Reichtum"-Akzent für Werte/Preise.
        public static readonly Color BackgroundColor = new Color(0.078f, 0.086f, 0.102f);
        public static readonly Color PanelColor = new Color(0.114f, 0.125f, 0.145f);
        public static readonly Color PanelColorLight = new Color(0.149f, 0.163f, 0.188f);
        public static readonly Color BorderColor = new Color(0.204f, 0.220f, 0.243f);
        public static readonly Color RustColor = new Color(0.710f, 0.314f, 0.180f);
        public static readonly Color RustBrightColor = new Color(0.851f, 0.416f, 0.247f);
        public static readonly Color GoldColor = new Color(0.831f, 0.663f, 0.298f);
        public static readonly Color TextColor = new Color(0.910f, 0.894f, 0.863f);
        public static readonly Color TextDimColor = new Color(0.604f, 0.592f, 0.569f);
        public static readonly Color GoodColor = new Color(0.435f, 0.608f, 0.369f);
        public static readonly Color BadColor = new Color(0.690f, 0.290f, 0.290f);

        public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static readonly Dictionary<int, Sprite> RoundedSpriteCache = new Dictionary<int, Sprite>();

        /// <summary>
        /// Gemeinsam genutztes, abgerundetes 9-Slice-Sprite (weiß, einfärgbar über Image.color).
        /// Wird pro Eck-Radius genau einmal generiert und danach für beliebig große Panels/Buttons
        /// wiederverwendet, statt für jedes UI-Element eine eigene Textur anzulegen.
        /// </summary>
        public static Sprite RoundedSprite(int cornerRadiusPx = 24)
        {
            if (RoundedSpriteCache.TryGetValue(cornerRadiusPx, out var cached))
            {
                return cached;
            }

            const int pad = 4;
            int size = cornerRadiusPx * 2 + pad * 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size / 2f, size / 2f);
            Vector2 halfSize = new Vector2(size / 2f - pad, size / 2f - pad);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 d = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - (halfSize - new Vector2(cornerRadiusPx, cornerRadiusPx));
                    float dist = new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - cornerRadiusPx;
                    // Weicher Übergang von ~1.5px für glatte statt treppige Kanten.
                    float alpha = Mathf.Clamp01(0.5f - dist / 1.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            float border = cornerRadiusPx + pad;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            RoundedSpriteCache[cornerRadiusPx] = sprite;
            return sprite;
        }

        /// <summary>Erzeugt ein Icon-<see cref="Image"/> mit fixer quadratischer Größe, zentriert im Parent.</summary>
        public static Image CreateIcon(Transform parent, Sprite sprite, float sizePx)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(sizePx, sizePx);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

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

        /// <summary>Panel mit abgerundeten Ecken im Kartenstil, statt einer reinen Farbfläche.</summary>
        public static RectTransform CreateRoundedPanel(Transform parent, string name, Color color, int cornerRadiusPx = 20)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = RoundedSprite(cornerRadiusPx);
            image.type = Image.Type.Sliced;
            image.color = color;
            return rt;
        }

        /// <summary>Abgerundetes Panel im Kartenstil (Kurzform von <see cref="CreateRoundedPanel"/>).</summary>
        public static RectTransform CreateCard(Transform parent, string name, Color color, int cornerRadiusPx = 20) =>
            CreateRoundedPanel(parent, name, color, cornerRadiusPx);

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
            int fontSize = 28, int cornerRadiusPx = 18)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = RoundedSprite(cornerRadiusPx);
            image.type = Image.Type.Sliced;
            image.color = backgroundColor;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ApplyButtonColorTint(button, backgroundColor);

            var text = CreateText(go.transform, label, fontSize, textColor, TextAnchor.MiddleCenter);
            StretchFull(text.rectTransform);

            return button;
        }

        /// <summary>Rundes Icon-Bündel: Icon links, Label rechts, in einem abgerundeten Button-Hintergrund.</summary>
        public static Button CreateIconButton(Transform parent, Sprite icon, string label, Color backgroundColor,
            Color textColor, int fontSize = 26, float iconSizePx = 40, int cornerRadiusPx = 18)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = RoundedSprite(cornerRadiusPx);
            image.type = Image.Type.Sliced;
            image.color = backgroundColor;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ApplyButtonColorTint(button, backgroundColor);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 6, 6);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            // control=true, damit die LayoutGroup die Icon-/Text-Breiten (statt der von
            // CreateText geerbten Vollflächen-Verankerung) tatsächlich selbst festlegt.
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;

            var iconImage = CreateIcon(go.transform, icon, iconSizePx);
            iconImage.gameObject.AddComponent<LayoutElement>().preferredWidth = iconSizePx;

            var text = CreateText(go.transform, label, fontSize, textColor, TextAnchor.MiddleCenter);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            return button;
        }

        /// <summary>
        /// Tab-Button für Bottom-Navigation: Icon oben, Label darunter, wechselt Farbe zwischen
        /// aktivem/inaktivem Zustand. Gibt sowohl Button als auch das Icon-Image zurück, damit
        /// der Aufrufer bei Tab-Wechsel Hintergrund- <em>und</em> Icon-Farbe anpassen kann.
        /// </summary>
        public static Button CreateTabButton(Transform parent, Sprite icon, string label, out Image iconImage,
            int cornerRadiusPx = 16)
        {
            var go = new GameObject(label + "Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var bgImage = go.GetComponent<Image>();
            bgImage.sprite = RoundedSprite(cornerRadiusPx);
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0f, 0f, 0f, 0f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = bgImage;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.14f);
            button.colors = colors;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 10, 6);
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            iconImage = CreateIcon(go.transform, icon, 40f);

            var text = CreateText(go.transform, label, 18, TextDimColor, TextAnchor.MiddleCenter);
            text.rectTransform.sizeDelta = new Vector2(150f, 26f);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;

            return button;
        }

        /// <summary>Kleiner abgerundeter Badge (z. B. für Preise, Zustände, Status) mit einfärgbarem Text.</summary>
        public static RectTransform CreateBadge(Transform parent, string label, Color backgroundColor, Color textColor,
            int fontSize = 22, int cornerRadiusPx = 14)
        {
            var panel = CreateRoundedPanel(parent, "Badge_" + label, backgroundColor, cornerRadiusPx);
            var text = CreateText(panel, label, fontSize, textColor, TextAnchor.MiddleCenter);
            StretchFull(text.rectTransform);
            return panel;
        }

        /// <summary>Setzt die Standard-Button-Farbübergänge (hover/pressed) passend zur Grundfarbe.</summary>
        private static void ApplyButtonColorTint(Button button, Color baseColor)
        {
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            button.colors = colors;
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
