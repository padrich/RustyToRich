using System.Collections.Generic;
using UnityEngine;

namespace RustyToRich.Data
{
    /// <summary>Sammlung aller im Spiel verfügbaren Performance-Tuning-Teile.</summary>
    [CreateAssetMenu(fileName = "PerformancePartDatabase", menuName = "Car Flip Tycoon/Performance Part Database")]
    public class PerformancePartDatabase : ScriptableObject
    {
        public List<PerformancePart> parts = new List<PerformancePart>();

        public PerformancePart GetById(string partId)
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

        public List<PerformancePart> GetByCategory(PerformancePartCategory category)
        {
            var result = new List<PerformancePart>();
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
