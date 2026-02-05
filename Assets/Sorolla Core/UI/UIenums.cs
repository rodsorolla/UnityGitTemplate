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
        // Game-specific screens should start at 100
        AtticFindsApp = 100,
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
        
        // Game-specific panels (100+)
        PreLevel = 113,
        BonusUnlocked = 114,
        BoosterTutorial = 115,
        WorldUnlocked = 116,
        PowerUpGlasses = 117,
        PowerUpTimer = 118,
        PowerUpUnlocked = 119,
        SpecialItemCollected = 120,
        AtticFinds = 121,
        Revive = 122,
    }
}