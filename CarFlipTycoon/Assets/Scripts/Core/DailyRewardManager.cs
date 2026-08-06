using System;
using System.Globalization;
using CarFlipTycoon.Utility;
using UnityEngine;

namespace CarFlipTycoon.Core
{
    /// <summary>
    /// Tägliche Login-Belohnung mit Streak-Bonus (mehr Coins bei aufeinanderfolgenden Tagen,
    /// gedeckelt bei <see cref="MaxStreakBonusDays"/>). Lässt sich optional über einen
    /// Rewarded Ad verdoppeln.
    /// </summary>
    public class DailyRewardManager : MonoBehaviour
    {
        private const int BaseRewardPerStreakDay = 100;
        private const int MaxStreakBonusDays = 7;

        public static DailyRewardManager Instance { get; private set; }

        /// <summary>coinsAwarded, neuer Streak-Wert.</summary>
        public event Action<int, int> OnDailyRewardClaimed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public bool HasPendingReward
        {
            get
            {
                var save = SaveManager.Instance.CurrentSave;
                if (string.IsNullOrEmpty(save.lastDailyRewardDateUtc))
                {
                    return true;
                }

                if (!DateTime.TryParse(save.lastDailyRewardDateUtc, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var lastClaim))
                {
                    return true;
                }

                return GameClock.Instance.UtcNow.Date > lastClaim.ToUniversalTime().Date;
            }
        }

        /// <summary>Coin-Betrag, den ein Claim jetzt bringen würde (ohne bereits zu claimen).</summary>
        public int PreviewRewardAmount()
        {
            int streak = ComputeStreakIfClaimedNow();
            return BaseRewardPerStreakDay * Mathf.Clamp(streak, 1, MaxStreakBonusDays);
        }

        private int ComputeStreakIfClaimedNow()
        {
            var save = SaveManager.Instance.CurrentSave;
            if (string.IsNullOrEmpty(save.lastDailyRewardDateUtc))
            {
                return 1;
            }

            if (!DateTime.TryParse(save.lastDailyRewardDateUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var lastClaim))
            {
                return 1;
            }

            var lastClaimDate = lastClaim.ToUniversalTime().Date;
            var today = GameClock.Instance.UtcNow.Date;
            return lastClaimDate == today.AddDays(-1) ? save.dailyRewardStreak + 1 : 1;
        }

        private bool TryClaim(out int coinsAwarded, out int streak)
        {
            coinsAwarded = 0;
            streak = 0;
            if (!HasPendingReward)
            {
                return false;
            }

            streak = ComputeStreakIfClaimedNow();
            coinsAwarded = BaseRewardPerStreakDay * Mathf.Clamp(streak, 1, MaxStreakBonusDays);

            var save = SaveManager.Instance.CurrentSave;
            save.dailyRewardStreak = streak;
            save.lastDailyRewardDateUtc = IdFactory.NowUtcIso();

            EconomyManager.Instance.AddCoins(coinsAwarded);
            SaveManager.Instance.Save();

            OnDailyRewardClaimed?.Invoke(coinsAwarded, streak);
            return true;
        }

        /// <summary>Holt die tägliche Belohnung ab, optional per Rewarded Ad verdoppelt.</summary>
        public void Claim(bool doubleViaAd, Action<bool, int, int> onComplete)
        {
            if (!HasPendingReward)
            {
                onComplete?.Invoke(false, 0, 0);
                return;
            }

            if (!doubleViaAd)
            {
                TryClaim(out int coins, out int streak);
                onComplete?.Invoke(true, coins, streak);
                return;
            }

            AdManager.Instance.ShowRewardedAd(RewardedAdPurpose.DoubleDailyReward, success =>
            {
                if (!success)
                {
                    onComplete?.Invoke(false, 0, 0);
                    return;
                }

                TryClaim(out int coins, out int streak);
                EconomyManager.Instance.AddCoins(coins); // Verdoppelung durch den Ad
                onComplete?.Invoke(true, coins * 2, streak);
            });
        }
    }
}
