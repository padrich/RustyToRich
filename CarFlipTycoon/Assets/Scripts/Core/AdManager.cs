using System;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>
    /// Platzhalter für die spätere Anbindung eines Werbe-SDKs (z. B. AdMob/Unity Ads).
    /// Bietet bereits die Aufrufstruktur, die restliche Spiel-Logik gegen programmiert
    /// werden kann, ohne dass ein echtes SDK integriert sein muss.
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        public bool IsInterstitialReady => false;
        public bool IsRewardedAdReady => false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public void ShowInterstitial(Action onComplete = null)
        {
            Debug.Log("[AdManager] Platzhalter: Interstitial-Ad würde hier angezeigt.");
            onComplete?.Invoke();
        }

        public void ShowRewardedAd(Action<bool> onResult)
        {
            Debug.Log("[AdManager] Platzhalter: Rewarded-Ad würde hier angezeigt.");
            onResult?.Invoke(false);
        }
    }
}
