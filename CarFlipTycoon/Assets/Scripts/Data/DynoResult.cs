using System;
using System.Collections.Generic;

namespace CarFlipTycoon.Data
{
    /// <summary>
    /// Ergebnis eines Prüfstand-Laufs (Dyno) für eine Auto-Instanz: Spitzenwerte inkl.
    /// der Drehzahl, bei der sie auftreten, sowie die volle gemessene Kurve für die
    /// Diagramm-Darstellung.
    /// </summary>
    [Serializable]
    public class DynoResult
    {
        public bool hasResult;

        public float horsePower;
        public int horsePowerRpm;

        public float torqueNm;
        public int torqueRpm;

        public string testDateUtc;

        public List<EnginePoint> curvePoints = new List<EnginePoint>();
    }
}
