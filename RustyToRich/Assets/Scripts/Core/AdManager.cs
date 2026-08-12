using System;
using UnityEngine;

namespace RustyToRich.Core
{
    /// <summary>
    /// Anbindungspunkt für Google AdMob (Banner + Rewarded Video), aktuell als funktionierender
    /// Platzhalter ohne echtes SDK: Google Mobile Ads für Unity ist ein externes Plugin-Paket
    /// (natives Android/iOS-Binding), das sich ohne Unity-Editor nicht seriös einbinden lässt.
    /// Die komplette Aufrufstruktur (Zwecke, Test-Ad-Unit-IDs, Callback-Signaturen) ist aber
    /// bereits fertig, sodass beim späteren SDK-Import nur die Methodenkörper unten durch echte
    /// AdMob-Aufrufe ersetzt werden müssen – die Aufrufstellen im restlichen Code bleiben gleich.
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        // Offizielle Google-Test-Ad-Unit-IDs (https://developers.google.com/admob/unity/test-ads).
        // Für den Release durch echte, projekteigene IDs ersetzen – sobald das SDK eingebunden ist,
        // am besten über ein separates Config-Asset statt hart codierter Konstanten.
        private const string TestBannerAdUnitIdAndroid = "ca-app-pub-3940256099942544/6300978111";
        private const string TestBannerAdUnitIdIos = "ca-app-pub-3940256099942544/2934735716";
        private const string TestRewardedAdUnitIdAndroid = "ca-app-pub-3940256099942544/5224354917";
        private const string TestRewardedAdUnitIdIos = "ca-app-pub-3940256099942544/1712485313";
        private const string TestInterstitialAdUnitIdAndroid = "ca-app-pub-3940256099942544/1033173712";
        private const string TestInterstitialAdUnitIdIos = "ca-app-pub-3940256099942544/4411468910";

        public static AdManager Instance { get; private set; }

        /// <summary>
        /// Ob aktuell ein Rewarded Ad angezeigt werden könnte. Aufrufer MÜSSEN dies vor dem
        /// Anzeigen eines "Sofort fertig ⚡"/Boost-Buttons prüfen und den Button bei false
        /// deaktivieren oder ausblenden, statt <see cref="ShowRewardedAd"/> blind aufzurufen.
        /// Platzhalter: liefert immer true, damit sich die Belohnungs-Logik im ganzen Spiel ohne
        /// echtes SDK durchtesten lässt; ein echtes SDK würde hier den geladenen Ad-Zustand prüfen.
        /// </summary>
        public bool IsRewardedAdReady => true;

        public bool IsInterstitialReady => false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public string GetBannerAdUnitId()
        {
#if UNITY_IOS
            return TestBannerAdUnitIdIos;
#else
            return TestBannerAdUnitIdAndroid;
#endif
        }

        private string GetRewardedAdUnitId()
        {
#if UNITY_IOS
            return TestRewardedAdUnitIdIos;
#else
            return TestRewardedAdUnitIdAndroid;
#endif
        }

        private string GetInterstitialAdUnitId()
        {
#if UNITY_IOS
            return TestInterstitialAdUnitIdIos;
#else
            return TestInterstitialAdUnitIdAndroid;
#endif
        }

        /// <summary>
        /// Zeigt einen Rewarded Video Ad für den angegebenen Zweck (Timer-Skip, Dyno-Skip,
        /// Auktion sofort beenden, Bieter-Boost, Teile-Freischaltung, Marktplatz-Refresh).
        /// onResult(true) = Nutzer hat den Ad vollständig gesehen, Belohnung gewähren.
        /// onResult(false) = abgebrochen oder kein Ad verfügbar, keine Belohnung gewähren.
        /// </summary>
        public void ShowRewardedAd(RewardedAdPurpose purpose, Action<bool> onResult)
        {
            if (!IsRewardedAdReady)
            {
                Debug.LogWarning($"[AdManager] Rewarded Ad für '{purpose}' angefordert, aber aktuell nicht verfügbar.");
                onResult?.Invoke(false);
                return;
            }

            Debug.Log($"[AdManager] Platzhalter: Rewarded Ad für '{purpose}' würde hier angezeigt " +
                      $"(Ad-Unit: {GetRewardedAdUnitId()}). Belohnung wird simuliert gewährt.");
            onResult?.Invoke(true);
        }

        public void ShowInterstitial(Action onComplete = null)
        {
            Debug.Log($"[AdManager] Platzhalter: Interstitial-Ad würde hier angezeigt (Ad-Unit: {GetInterstitialAdUnitId()}).");
            onComplete?.Invoke();
        }
    }
}
