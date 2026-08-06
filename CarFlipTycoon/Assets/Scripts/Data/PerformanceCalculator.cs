using System.Collections.Generic;
using UnityEngine;

namespace CarFlipTycoon.Data
{
    /// <summary>
    /// Berechnet die effektive Motorkurve eines Autos aus seiner Basis-Motorkurve und den
    /// aktuell verbauten Performance-Teilen. Vereinfachtes, aber nachvollziehbares Modell:
    /// jeder Teil-Effekt (fest + prozentual + RPM-Verschiebung) wird nacheinander auf jeden
    /// Referenzpunkt der Basiskurve angewendet; Aufladungs-abhängige Boni werden über
    /// <see cref="ForcedInductionRole"/> generisch berücksichtigt.
    /// </summary>
    public static class PerformanceCalculator
    {
        public static EngineCurve ComputeEffectiveCurve(EngineCurve baseCurve, IReadOnlyList<PerformancePart> installedParts)
        {
            var result = new EngineCurve();
            if (baseCurve == null || baseCurve.points == null)
            {
                return result;
            }

            bool hasForcedInduction = HasForcedInduction(installedParts);

            for (int p = 0; p < baseCurve.points.Count; p++)
            {
                var basePoint = baseCurve.points[p];
                float hp = basePoint.horsePower;
                float nm = basePoint.torqueNm;
                int rpm = basePoint.rpm;

                if (installedParts != null)
                {
                    for (int i = 0; i < installedParts.Count; i++)
                    {
                        var part = installedParts[i];
                        if (part == null)
                        {
                            continue;
                        }

                        float multiplier = 1f;
                        if (part.forcedInductionRole == ForcedInductionRole.StrongerWithForcedInduction && hasForcedInduction)
                        {
                            multiplier = part.forcedInductionBonusMultiplier;
                        }
                        else if (part.forcedInductionRole == ForcedInductionRole.StrongerWithoutForcedInduction && !hasForcedInduction)
                        {
                            multiplier = part.forcedInductionBonusMultiplier;
                        }

                        hp += part.horsePowerBonusFlat * multiplier;
                        hp *= 1f + part.horsePowerBonusPercent * multiplier;
                        nm += part.torqueBonusFlat * multiplier;
                        nm *= 1f + part.torqueBonusPercent * multiplier;
                        rpm += part.rpmShift;
                    }
                }

                result.points.Add(new EnginePoint(Mathf.Max(0, rpm), Mathf.Max(0f, hp), Mathf.Max(0f, nm)));
            }

            result.points.Sort((a, b) => a.rpm.CompareTo(b.rpm));
            return result;
        }

        /// <summary>Grobe Spitzenwert-Schätzung: höchste PS und höchste Nm über alle Kurvenpunkte (nicht zwingend am selben Punkt).</summary>
        public static (float peakHorsePower, float peakTorqueNm) GetPeakOutput(EngineCurve curve)
        {
            if (curve == null || curve.points == null || curve.points.Count == 0)
            {
                return (0f, 0f);
            }

            float peakHp = 0f;
            float peakNm = 0f;
            for (int i = 0; i < curve.points.Count; i++)
            {
                peakHp = Mathf.Max(peakHp, curve.points[i].horsePower);
                peakNm = Mathf.Max(peakNm, curve.points[i].torqueNm);
            }

            return (peakHp, peakNm);
        }

        /// <summary>
        /// Vereinfachte, aber reale Faustformel für den Prüfstand: Leistung [kW] = Drehmoment [Nm] ×
        /// Drehzahl [1/min] ÷ 9550; PS = kW × 1,36. Damit hängen die am Prüfstand "gemessenen" PS
        /// direkt und konsistent von Nm und Drehzahl ab, statt von den frei geschätzten
        /// horsePower-Werten der Basiskurve.
        /// </summary>
        public static float TorqueAndRpmToHorsePower(float torqueNm, int rpm)
        {
            float kilowatts = torqueNm * rpm / 9550f;
            return kilowatts * 1.36f;
        }

        /// <summary>
        /// Baut aus einer Kurve die am Prüfstand "gemessene" Kurve: Drehzahl und Nm bleiben gleich,
        /// die PS-Werte werden konsistent aus <see cref="TorqueAndRpmToHorsePower"/> abgeleitet.
        /// </summary>
        public static EngineCurve BuildMeasuredCurve(EngineCurve curve)
        {
            var result = new EngineCurve();
            if (curve == null || curve.points == null)
            {
                return result;
            }

            for (int i = 0; i < curve.points.Count; i++)
            {
                var point = curve.points[i];
                result.points.Add(new EnginePoint(point.rpm, TorqueAndRpmToHorsePower(point.torqueNm, point.rpm), point.torqueNm));
            }

            return result;
        }

        /// <summary>Ob eine Liste installierter Teile ein Aufladungssystem (Turbolader/Kompressor) enthält.</summary>
        public static bool HasForcedInduction(IReadOnlyList<PerformancePart> installedParts)
        {
            if (installedParts == null)
            {
                return false;
            }

            for (int i = 0; i < installedParts.Count; i++)
            {
                if (installedParts[i] != null && installedParts[i].forcedInductionRole == ForcedInductionRole.ProvidesForcedInduction)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
