using UnityEngine;

namespace CarFlipTycoon.Data
{
    /// <summary>
    /// Vorlage für ein Optik-Tuning-Teil. Teile innerhalb derselben Kategorie sind echte
    /// Varianten (z. B. verschiedene Felgengrößen/-designs), keine linearen Upgrade-Stufen –
    /// der Spieler wählt zwischen ihnen statt einfach "aufzuleveln".
    /// </summary>
    [CreateAssetMenu(fileName = "NewCosmeticPart", menuName = "Car Flip Tycoon/Cosmetic Part")]
    public class CosmeticPart : ScriptableObject
    {
        [Tooltip("Stabile ID, unabhängig vom Asset-Namen. Wird in Spielständen referenziert.")]
        [SerializeField] private string partId;

        [Header("Identität")]
        public string partName;
        public CosmeticPartCategory category;
        [TextArea] public string description;

        [Header("Handel")]
        public int price;
        [Tooltip("Multiplikator auf den geschätzten Fahrzeugwert, wenn dieses Teil verbaut ist (1.0 = keine Änderung).")]
        public float valueFactor = 1.05f;

        [Header("Stil")]
        [Tooltip("Ein Teil kann mehreren Stilen zugeordnet sein.")]
        public StyleTag[] styleTags;

        [Header("Nur Kategorie Felgen")]
        [Tooltip("Felgengröße in Zoll (z. B. 15-22), wird mit dem Design zum Anzeigenamen kombiniert.")]
        public int wheelSizeInches;

        [Header("Nur Kategorie Höhe")]
        public RideHeightTier heightTier;

        [Header("Grafik / Vorschau")]
        public Sprite icon;
        [Tooltip("Sprite-Layer, der beim Sprite-Layering über die Basis-Fahrzeuggrafik gelegt wird.")]
        public Sprite previewOverlaySprite;
        [Tooltip("Sortierreihenfolge der Vorschau-Layer (höher = weiter oben in der Stapelreihenfolge).")]
        public int previewSortOrder;

        [Header("Einbau")]
        [Tooltip("Sekunden für den 'Wird eingebaut'-Timer nach dem Kauf.")]
        public float installDurationSeconds = 45f;

        /// <summary>Stabile ID für Saves; fällt auf den Asset-Namen zurück, falls nicht gesetzt.</summary>
        public string PartId => string.IsNullOrEmpty(partId) ? name : partId;

        public bool HasStyle(StyleTag style)
        {
            if (styleTags == null)
            {
                return false;
            }

            for (int i = 0; i < styleTags.Length; i++)
            {
                if (styleTags[i] == style)
                {
                    return true;
                }
            }

            return false;
        }

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
