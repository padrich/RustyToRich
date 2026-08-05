using System.Collections.Generic;
using CarFlipTycoon.Data;
using UnityEngine;

namespace CarFlipTycoon.UI
{
    /// <summary>
    /// Zeichnet ein Liniendiagramm (PS und Nm über der Drehzahl) rein mit uGUI-Elementen:
    /// jedes Kurvensegment wird als schmales, rotiertes Image zwischen zwei Punkten gezeichnet
    /// (kein LineRenderer/kein externes Chart-Plugin nötig). PS und Nm nutzen jeweils eine
    /// eigene, unabhängig skalierte Achse innerhalb derselben Fläche – wie bei echten
    /// Dyno-Diagrammen mit zwei Skalen –, damit beide Kurven trotz unterschiedlicher Einheiten
    /// gut lesbar bleiben. Optional wird eine zweite, blasser dargestellte "Vorher"-Kurve
    /// überlagert (Vorher/Nachher-Vergleich).
    /// </summary>
    public class DynoChartView : MonoBehaviour
    {
        private static readonly Color HorsePowerColor = new Color(0.95f, 0.55f, 0.15f);
        private static readonly Color TorqueColor = new Color(0.3f, 0.65f, 0.95f);

        private RectTransform _plotArea;

        public void Build(RectTransform panel)
        {
            var background = UIFactory.CreatePanel(panel, "ChartBackground", new Color(0f, 0f, 0f, 0.25f));
            UIFactory.StretchFull(background);

            _plotArea = UIFactory.CreateContainer(background, "PlotArea");
            _plotArea.anchorMin = new Vector2(0.03f, 0.06f);
            _plotArea.anchorMax = new Vector2(0.97f, 0.94f);
            _plotArea.offsetMin = Vector2.zero;
            _plotArea.offsetMax = Vector2.zero;
        }

        /// <summary>Setzt die aktuelle Kurve (und optional eine blasser dargestellte Vergleichskurve) neu.</summary>
        public void SetCurves(List<EnginePoint> currentCurve, List<EnginePoint> previousCurve)
        {
            for (int i = _plotArea.childCount - 1; i >= 0; i--)
            {
                Destroy(_plotArea.GetChild(i).gameObject);
            }

            if (currentCurve == null || currentCurve.Count < 2)
            {
                return;
            }

            // Erzwingt eine sofortige Neuberechnung der Anchor-basierten RectTransform-Maße,
            // damit _plotArea.rect.width/height direkt nach dem Aufbau bereits stimmen.
            Canvas.ForceUpdateCanvases();

            int minRpm = currentCurve[0].rpm;
            int maxRpm = currentCurve[currentCurve.Count - 1].rpm;
            float hpAxisMax = Mathf.Max(1f, MaxOf(currentCurve, true) * 1.15f);
            float nmAxisMax = Mathf.Max(1f, MaxOf(currentCurve, false) * 1.15f);

            bool hasPrevious = previousCurve != null && previousCurve.Count >= 2;
            if (hasPrevious)
            {
                minRpm = Mathf.Min(minRpm, previousCurve[0].rpm);
                maxRpm = Mathf.Max(maxRpm, previousCurve[previousCurve.Count - 1].rpm);
                hpAxisMax = Mathf.Max(hpAxisMax, MaxOf(previousCurve, true) * 1.15f);
                nmAxisMax = Mathf.Max(nmAxisMax, MaxOf(previousCurve, false) * 1.15f);
            }

            if (hasPrevious)
            {
                DrawCurve(previousCurve, minRpm, maxRpm, hpAxisMax, nmAxisMax, 0.35f);
            }

            DrawCurve(currentCurve, minRpm, maxRpm, hpAxisMax, nmAxisMax, 1f);
        }

        private static float MaxOf(List<EnginePoint> curve, bool horsePower)
        {
            float max = 0f;
            for (int i = 0; i < curve.Count; i++)
            {
                max = Mathf.Max(max, horsePower ? curve[i].horsePower : curve[i].torqueNm);
            }

            return max;
        }

        private void DrawCurve(List<EnginePoint> curve, int minRpm, int maxRpm, float hpAxisMax, float nmAxisMax, float alpha)
        {
            for (int i = 0; i < curve.Count - 1; i++)
            {
                DrawSegment(curve[i].rpm, curve[i].horsePower, curve[i + 1].rpm, curve[i + 1].horsePower,
                    minRpm, maxRpm, hpAxisMax, WithAlpha(HorsePowerColor, alpha));
                DrawSegment(curve[i].rpm, curve[i].torqueNm, curve[i + 1].rpm, curve[i + 1].torqueNm,
                    minRpm, maxRpm, nmAxisMax, WithAlpha(TorqueColor, alpha));
            }
        }

        private void DrawSegment(int rpmA, float valueA, int rpmB, float valueB, int minRpm, int maxRpm, float axisMax, Color color)
        {
            Vector2 a = ToPlotPoint(rpmA, valueA, minRpm, maxRpm, axisMax);
            Vector2 b = ToPlotPoint(rpmB, valueB, minRpm, maxRpm, axisMax);

            var segment = UIFactory.CreatePanel(_plotArea, "Segment", color);
            float length = Mathf.Max(Vector2.Distance(a, b), 1f);
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

            segment.anchorMin = Vector2.zero;
            segment.anchorMax = Vector2.zero;
            segment.pivot = new Vector2(0f, 0.5f);
            segment.sizeDelta = new Vector2(length, 5f);
            segment.anchoredPosition = a;
            segment.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private Vector2 ToPlotPoint(int rpm, float value, int minRpm, int maxRpm, float axisMax)
        {
            float width = _plotArea.rect.width;
            float height = _plotArea.rect.height;
            float tx = maxRpm > minRpm ? (rpm - minRpm) / (float)(maxRpm - minRpm) : 0f;
            float ty = Mathf.Clamp01(value / axisMax);
            return new Vector2(tx * width, ty * height);
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
