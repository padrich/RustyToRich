using System.Collections.Generic;
using UnityEngine;

namespace CarFlipTycoon.Data
{
    /// <summary>Sammlung aller im Spiel verfügbaren Optik-Tuning-Teile.</summary>
    [CreateAssetMenu(fileName = "CosmeticPartDatabase", menuName = "Car Flip Tycoon/Cosmetic Part Database")]
    public class CosmeticPartDatabase : ScriptableObject
    {
        public List<CosmeticPart> parts = new List<CosmeticPart>();

        public CosmeticPart GetById(string partId)
        {
            if (string.IsNullOrEmpty(partId))
            {
                return null;
            }

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null && parts[i].PartId == partId)
                {
                    return parts[i];
                }
            }

            return null;
        }

        public List<CosmeticPart> GetByCategory(CosmeticPartCategory category)
        {
            var result = new List<CosmeticPart>();
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null && parts[i].category == category)
                {
                    result.Add(parts[i]);
                }
            }

            return result;
        }
    }
}
