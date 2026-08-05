using UnityEngine;

namespace CarFlipTycoon.Data
{
    /// <summary>
    /// Vorlage ("Auto-Typ") für ein Fahrzeugmodell. Definiert alles, was für alle
    /// Instanzen dieses Modells gleich ist (Basiswerte, Kurven, Sprites).
    /// Konkrete Fahrzeuge im Besitz des Spielers werden als <see cref="CarInstance"/> abgebildet.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCarType", menuName = "Car Flip Tycoon/Car Type")]
    public class CarType : ScriptableObject
    {
        [Tooltip("Stabile ID, unabhängig vom Asset-Namen. Wird in Spielständen referenziert.")]
        [SerializeField] private string carTypeId;

        [Header("Identität")]
        public string modelName;
        public CarClass carClass;
        [TextArea] public string description;

        [Header("Prüfstand-Basiswerte")]
        public EngineCurve baseEngineCurve = new EngineCurve();

        [Header("Handel")]
        public int basePriceMin;
        public int basePriceMax;

        [Header("Grafik")]
        public Sprite icon;
        public Sprite[] additionalSprites;

        /// <summary>Stabile ID für Saves; fällt auf den Asset-Namen zurück, falls nicht gesetzt.</summary>
        public string CarTypeId => string.IsNullOrEmpty(carTypeId) ? name : carTypeId;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(carTypeId))
            {
                carTypeId = System.Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (basePriceMax < basePriceMin)
            {
                basePriceMax = basePriceMin;
            }
        }
#endif
    }
}
