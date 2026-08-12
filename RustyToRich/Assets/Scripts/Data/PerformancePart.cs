using UnityEngine;

namespace RustyToRich.Data
{
    /// <summary>
    /// Vorlage für ein Performance-Tuning-Teil. Wirkt über <see cref="PerformanceCalculator"/>
    /// additiv/prozentual und ggf. drehzahlverschiebend auf die Basis-Motorkurve eines Auto-Typs.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPerformancePart", menuName = "Car Flip Tycoon/Performance Part")]
    public class PerformancePart : ScriptableObject
    {
        [Tooltip("Stabile ID, unabhängig vom Asset-Namen. Wird in Spielständen referenziert.")]
        [SerializeField] private string partId;

        [Header("Identität")]
        public string partName;
        public PerformancePartCategory category;
        [TextArea] public string description;
        [Tooltip("Ausbaustufe innerhalb der Kategorie, z. B. 1-3 bei Ansaugung/Auspuff/Chiptuning.")]
        public int stage = 1;

        [Header("Handel")]
        public int price;

        [Header("Effekt auf die Motorkurve")]
        [Tooltip("Fester PS-Bonus, wird vor dem prozentualen Bonus addiert.")]
        public float horsePowerBonusFlat;
        [Tooltip("Prozentualer PS-Bonus, z. B. 0.1 = +10%.")]
        public float horsePowerBonusPercent;
        [Tooltip("Fester Nm-Bonus, wird vor dem prozentualen Bonus addiert.")]
        public float torqueBonusFlat;
        [Tooltip("Prozentualer Nm-Bonus, z. B. 0.1 = +10%. Negative Werte sind zulässig (z. B. Nockenwellen im unteren Bereich).")]
        public float torqueBonusPercent;
        [Tooltip("Verschiebt die Referenzpunkte der Motorkurve um diesen RPM-Wert (positiv = Leistungsband nach oben).")]
        public int rpmShift;
        [Tooltip("Spielerischer Bonus auf die Prüfstand-Stabilität bei anhaltender Volllast (0-1), z. B. beim Ladeluftkühler.")]
        [Range(0f, 1f)]
        public float dynoStabilityBonus;

        [Header("Aufladungs-Interaktion")]
        public ForcedInductionRole forcedInductionRole = ForcedInductionRole.None;
        [Tooltip("Multiplikator auf die Effekt-Boni dieses Teils, abhängig von forcedInductionRole (StrongerWith.../StrongerWithout...). 1 = keine Änderung.")]
        public float forcedInductionBonusMultiplier = 1f;
        [TextArea]
        [Tooltip("Frei formulierter Kompatibilitäts-Hinweis für die UI, z. B. 'Schließt sich mit Kompressor aus'.")]
        public string compatibilityNote;

        [Header("Einbau")]
        [Tooltip("Sekunden für den 'Wird eingebaut'-Timer nach dem Kauf. Größere Umbauten dauern länger.")]
        public float installDurationSeconds = 60f;

        /// <summary>Stabile ID für Saves; fällt auf den Asset-Namen zurück, falls nicht gesetzt.</summary>
        public string PartId => string.IsNullOrEmpty(partId) ? name : partId;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(partId))
            {
                partId = System.Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
