namespace Sorolla.Cosmetics
{
    /// <summary>How a skin is unlocked. Extend by adding cases.</summary>
    public enum SkinUnlockType
    {
        Default,      // Free / unlocked from the start
        ReachLevel,   // UnlockValue = level number
        CoinPurchase, // UnlockValue = coin cost
        IAP,          // Real-money purchase (handled later)
        Reward,       // Granted by some reward flow (handled later)
        ReachTier     // UnlockValue = tournament tier index (0 Bronze, 1 Silver, 2 Gold, 3 Sapphire, 4 Ruby)
    }
}
