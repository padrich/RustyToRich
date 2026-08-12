namespace RustyToRich.Core
{
    /// <summary>
    /// Alle Stellen im Spiel, an denen ein Rewarded Video Ad angeboten wird. Wird an
    /// <see cref="AdManager.ShowRewardedAd"/> übergeben, damit Logging/Analytics und ggf.
    /// unterschiedliche Ad-Platzierungen sauber unterschieden werden können.
    /// </summary>
    public enum RewardedAdPurpose
    {
        SkipTuningInstall,
        SkipDynoTest,
        FinishAuctionNow,
        AuctionBidderBoost,
        UnlockExclusivePart,
        RefreshMarketplaceNow,
        DoubleDailyReward
    }
}
