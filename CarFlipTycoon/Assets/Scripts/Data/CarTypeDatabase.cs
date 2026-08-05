using System.Collections.Generic;
using UnityEngine;

namespace CarFlipTycoon.Data
{
    /// <summary>
    /// Sammlung aller im Spiel verfügbaren Auto-Typen (Vorlagen).
    /// Wird zur Laufzeit genutzt, um die gespeicherte carTypeId einer
    /// <see cref="CarInstance"/> wieder auf den zugehörigen <see cref="CarType"/> aufzulösen.
    /// </summary>
    [CreateAssetMenu(fileName = "CarTypeDatabase", menuName = "Car Flip Tycoon/Car Type Database")]
    public class CarTypeDatabase : ScriptableObject
    {
        public List<CarType> carTypes = new List<CarType>();

        public CarType GetById(string carTypeId)
        {
            if (string.IsNullOrEmpty(carTypeId))
            {
                return null;
            }

            for (int i = 0; i < carTypes.Count; i++)
            {
                if (carTypes[i] != null && carTypes[i].CarTypeId == carTypeId)
                {
                    return carTypes[i];
                }
            }

            return null;
        }
    }
}
