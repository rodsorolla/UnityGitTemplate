namespace Sorolla.UI
{
    /// <summary>
    /// Core UI Screen IDs. Games can define their own screens beyond these base values.
    /// Values 0-99 are reserved for Sorolla Core. Game-specific screens should use 100+.
    /// </summary>
    public enum UIScreenId
    {
        None = 0,
        Splash = 1,
        MainMenu = 2,
        Gameplay = 3,
        Settings = 4,
        Profile = 5,
        Tournament = 6,
        // Game-specific screens should start at 100
    }

    /// <summary>
    /// Core UI Panel IDs. Games can define their own panels beyond these base values.
    /// Values 0-99 are reserved for Sorolla Core. Game-specific panels should use 100+.
    /// </summary>
    public enum UIPanelId
    {
        None = 0,
        Pause = 1,
        ConfirmDialog = 2,
        Toast = 3,
        LevelComplete = 4,
        GameOver = 5,
        Continue = 13,
        Settings = 14,
        PowerUpUsed = 15,
        OutOfLives = 16,
        TournamentResults = 17,
        // Game-specific panels (Hungry Snake): preserved across Sorolla Core updates
        PreLevel = 113,
        BonusUnlocked = 114,
        BoosterTutorial = 115,
        WorldUnlocked = 116,
        BoosterOffer = 117,
        PurchaseCompleted = 118,
        NotEnoughCoins = 119,
        TreasureHuntSteps = 120,
        TreasureHuntInfo = 121,
        WinStreakInfo = 122,
        LoseStreak = 123,
        DailyReward = 124,
        StarterPackOffer = 125,
        FirstAd = 126,
        NoInternet = 127,
        RateGame = 128,
        Profile = 129,
        TournamentReward = 130,
        TournamentInfo = 131,
        RVCoins = 132,
        BuyAd = 133,
    }
}
