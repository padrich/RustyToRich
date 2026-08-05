using System;
using System.Collections.Generic;

namespace CarFlipTycoon.Data
{
    /// <summary>Ein Referenzpunkt der Motorkurve bei einer bestimmten Drehzahl.</summary>
    [Serializable]
    public class EnginePoint
    {
        public int rpm;
        public float horsePower;
        public float torqueNm;

        public EnginePoint() { }

        public EnginePoint(int rpm, float horsePower, float torqueNm)
        {
            this.rpm = rpm;
            this.horsePower = horsePower;
            this.torqueNm = torqueNm;
        }
    }

    /// <summary>
    /// Basis-Motorkurve eines Auto-Typs: Referenzwerte für PS/Nm über der Drehzahl.
    /// Dient als Grundlage für das spätere Prüfstand-Feature (Dyno).
    /// </summary>
    [Serializable]
    public class EngineCurve
    {
        public List<EnginePoint> points = new List<EnginePoint>();

        /// <summary>Lineare Interpolation von PS/Nm zwischen den definierten Referenzpunkten.</summary>
        public (float horsePower, float torqueNm) Evaluate(int atRpm)
        {
            if (points == null || points.Count == 0)
            {
                return (0f, 0f);
            }

            if (points.Count == 1 || atRpm <= points[0].rpm)
            {
                return (points[0].horsePower, points[0].torqueNm);
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                EnginePoint a = points[i];
                EnginePoint b = points[i + 1];

                if (atRpm >= a.rpm && atRpm <= b.rpm)
                {
                    float t = b.rpm == a.rpm ? 0f : (atRpm - a.rpm) / (float)(b.rpm - a.rpm);
                    float hp = a.horsePower + (b.horsePower - a.horsePower) * t;
                    float nm = a.torqueNm + (b.torqueNm - a.torqueNm) * t;
                    return (hp, nm);
                }
            }

            EnginePoint last = points[points.Count - 1];
            return (last.horsePower, last.torqueNm);
        }
    }
}
