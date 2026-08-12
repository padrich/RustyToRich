using UnityEngine;

namespace RustyToRich.UI
{
    /// <summary>
    /// Erzeugt zur Laufzeit einfache, flache Vektor-Icons als <see cref="Sprite"/> – Münze,
    /// Auto (Seitenansicht), Preisschild, Auktionshammer, Uhr, Aktualisieren-Pfeil, Geschenk,
    /// Garage – ganz ohne externe Bild-Assets (also ohne jede Lizenzfrage). Jede Form wird
    /// analytisch (Abstandsfunktionen) mit 4x Supersampling gerastert, damit die Kanten trotz
    /// kleiner Auflösung weich statt treppig wirken.
    /// </summary>
    public static class IconFactory
    {
        private const int Size = 128;
        private const int Supersample = 4;

        public static Sprite CoinIcon(Color face, Color rim) => Build(uv => CoinShape(uv, face, rim));
        public static Sprite CarIcon(Color body) => Build(uv => CarShape(uv, body));
        public static Sprite TagIcon(Color body) => Build(uv => TagShape(uv, body));
        public static Sprite GavelIcon(Color body) => Build(uv => GavelShape(uv, body));
        public static Sprite ClockIcon(Color body) => Build(uv => ClockShape(uv, body));
        public static Sprite RefreshIcon(Color body) => Build(uv => RefreshShape(uv, body));
        public static Sprite GiftIcon(Color box, Color ribbon) => Build(uv => GiftShape(uv, box, ribbon));
        public static Sprite GarageIcon(Color body) => Build(uv => GarageShape(uv, body));

        private delegate Color? ShapeFn(Vector2 uv);

        // ---------------------------------------------------------------- Rasterung

        private static Sprite Build(ShapeFn shape)
        {
            int hi = Size * Supersample;
            var big = new Color[hi * hi];
            for (int y = 0; y < hi; y++)
            {
                float v = (y + 0.5f) / hi - 0.5f;
                for (int x = 0; x < hi; x++)
                {
                    float u = (x + 0.5f) / hi - 0.5f;
                    big[y * hi + x] = shape(new Vector2(u, v)) ?? new Color(0f, 0f, 0f, 0f);
                }
            }

            var pixels = new Color[Size * Size];
            int n = Supersample * Supersample;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float r = 0, g = 0, b = 0, a = 0;
                    for (int sy = 0; sy < Supersample; sy++)
                    {
                        int by = y * Supersample + sy;
                        for (int sx = 0; sx < Supersample; sx++)
                        {
                            var c = big[by * hi + (x * Supersample + sx)];
                            r += c.r * c.a;
                            g += c.g * c.a;
                            b += c.b * c.a;
                            a += c.a;
                        }
                    }
                    if (a > 0.001f)
                    {
                        r /= a;
                        g /= a;
                        b /= a;
                    }
                    pixels[y * Size + x] = new Color(r, g, b, a / n);
                }
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        // ---------------------------------------------------------------- Primitive (SDF-artig)

        private static float RoundedRectDist(Vector2 p, Vector2 center, Vector2 halfSize, float radius)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - (halfSize - new Vector2(radius, radius));
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - radius;
        }

        private static bool InRoundedRect(Vector2 p, Vector2 center, Vector2 halfSize, float radius) =>
            RoundedRectDist(p, center, halfSize, radius) <= 0f;

        private static bool InCircle(Vector2 p, Vector2 center, float radius) => (p - center).magnitude <= radius;

        private static bool InRing(Vector2 p, Vector2 center, float outerRadius, float innerRadius)
        {
            float d = (p - center).magnitude;
            return d <= outerRadius && d >= innerRadius;
        }

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p - a, b - a);
            float d2 = Cross(p - b, c - b);
            float d3 = Cross(p - c, a - c);
            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static Vector2 Rotate(Vector2 p, Vector2 pivot, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            Vector2 d = p - pivot;
            return new Vector2(d.x * cos - d.y * sin, d.x * sin + d.y * cos) + pivot;
        }

        // ---------------------------------------------------------------- Formen

        private static Color? CoinShape(Vector2 uv, Color face, Color rim)
        {
            float d = uv.magnitude;
            if (d > 0.46f) return null;
            if (d > 0.38f) return rim;
            return face;
        }

        private static Color? CarShape(Vector2 uv, Color body)
        {
            bool wheelL = InCircle(uv, new Vector2(-0.20f, -0.22f), 0.115f);
            bool wheelR = InCircle(uv, new Vector2(0.20f, -0.22f), 0.115f);
            if (wheelL || wheelR) return new Color(0.09f, 0.09f, 0.10f, 1f);

            bool hubL = InCircle(uv, new Vector2(-0.20f, -0.22f), 0.045f);
            bool hubR = InCircle(uv, new Vector2(0.20f, -0.22f), 0.045f);
            if (hubL || hubR) return new Color(0.55f, 0.55f, 0.58f, 1f);

            bool lowerBody = InRoundedRect(uv, new Vector2(0f, -0.06f), new Vector2(0.44f, 0.13f), 0.09f);
            bool cabin = InRoundedRect(uv, new Vector2(-0.03f, 0.11f), new Vector2(0.22f, 0.10f), 0.07f);
            if (lowerBody || cabin) return body;
            return null;
        }

        private static Color? GarageShape(Vector2 uv, Color body)
        {
            // Haus-/Garagensilhouette: Satteldach (Dreieck) über einem Rechteck-Baukörper.
            bool roof = InTriangle(uv, new Vector2(0f, 0.34f), new Vector2(-0.38f, 0.02f), new Vector2(0.38f, 0.02f));
            bool box = InRoundedRect(uv, new Vector2(0f, -0.16f), new Vector2(0.30f, 0.20f), 0.03f);
            bool door = InRoundedRect(uv, new Vector2(0f, -0.22f), new Vector2(0.16f, 0.12f), 0.02f);
            if (door) return new Color(body.r * 0.55f, body.g * 0.55f, body.b * 0.55f, 1f);
            if (roof || box) return body;
            return null;
        }

        private static Color? TagShape(Vector2 uv, Color body)
        {
            // Preisschild: Rechteck + Spitze nach links, mit kleinem Loch nahe der Spitze.
            bool rect = InRoundedRect(uv, new Vector2(0.08f, 0f), new Vector2(0.30f, 0.22f), 0.05f);
            bool point = InTriangle(uv, new Vector2(-0.22f, 0f), new Vector2(-0.02f, 0.22f), new Vector2(-0.02f, -0.22f));
            bool hole = InCircle(uv, new Vector2(-0.10f, 0f), 0.045f);
            if (hole) return null;
            if (rect || point) return body;
            return null;
        }

        private static Color? GavelShape(Vector2 uv, Color body)
        {
            // Hammerkopf (rotiertes, abgerundetes Rechteck) + diagonaler Griff.
            Vector2 headLocal = Rotate(uv, new Vector2(0.08f, 0.14f), -35f);
            bool head = InRoundedRect(headLocal, new Vector2(0.08f, 0.14f), new Vector2(0.20f, 0.10f), 0.04f);

            Vector2 handleLocal = Rotate(uv, new Vector2(-0.14f, -0.16f), -35f);
            bool handle = InRoundedRect(handleLocal, new Vector2(-0.14f, -0.16f), new Vector2(0.05f, 0.20f), 0.04f);

            bool baseBlock = InRoundedRect(uv, new Vector2(0f, -0.34f), new Vector2(0.22f, 0.05f), 0.02f);

            if (head || handle || baseBlock) return body;
            return null;
        }

        private static Color? ClockShape(Vector2 uv, Color body)
        {
            if (InRing(uv, Vector2.zero, 0.42f, 0.34f)) return body;

            // Zeiger: Minutenzeiger nach oben, Stundenzeiger schräg – beide als schmale Rundrechtecke.
            Vector2 minuteLocal = Rotate(uv, Vector2.zero, 0f);
            bool minuteHand = InRoundedRect(minuteLocal, new Vector2(0f, 0.14f), new Vector2(0.035f, 0.20f), 0.03f);

            Vector2 hourLocal = Rotate(uv, Vector2.zero, -60f);
            bool hourHand = InRoundedRect(hourLocal, new Vector2(0f, 0.10f), new Vector2(0.035f, 0.14f), 0.03f);

            bool pivot = InCircle(uv, Vector2.zero, 0.045f);

            if (minuteHand || hourHand || pivot) return body;
            return null;
        }

        private static Color? RefreshShape(Vector2 uv, Color body)
        {
            // 3/4-Ring plus kleine Pfeilspitze am offenen Ende.
            float angle = Mathf.Atan2(uv.y, uv.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            bool ring = InRing(uv, Vector2.zero, 0.40f, 0.28f) && angle >= 35f && angle <= 320f;

            bool arrow = InTriangle(uv,
                new Vector2(0.30f, 0.26f),
                new Vector2(0.42f, 0.10f),
                new Vector2(0.16f, 0.14f));

            if (ring || arrow) return body;
            return null;
        }

        private static Color? GiftShape(Vector2 uv, Color box, Color ribbon)
        {
            bool boxShape = InRoundedRect(uv, new Vector2(0f, -0.08f), new Vector2(0.34f, 0.26f), 0.04f);
            bool lid = InRoundedRect(uv, new Vector2(0f, 0.22f), new Vector2(0.38f, 0.08f), 0.03f);
            bool bowL = InCircle(uv, new Vector2(-0.10f, 0.32f), 0.075f);
            bool bowR = InCircle(uv, new Vector2(0.10f, 0.32f), 0.075f);

            bool ribbonV = InRoundedRect(uv, new Vector2(0f, -0.02f), new Vector2(0.045f, 0.32f), 0.02f);
            bool ribbonH = InRoundedRect(uv, new Vector2(0f, 0.22f), new Vector2(0.38f, 0.045f), 0.02f);

            if (ribbonV || ribbonH) return ribbon;
            if (boxShape || lid || bowL || bowR) return box;
            return null;
        }
    }
}
