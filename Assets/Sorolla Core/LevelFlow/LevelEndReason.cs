namespace Sorolla.LevelFlow
{
    /// <summary>
    /// Reasons why a level ended (win or lose).
    /// Values 0-99 are reserved for Sorolla Core. Game-specific reasons should use 100+.
    /// </summary>
    public enum LevelEndReason
    {
        /// <summary>No reason specified or level hasn't ended.</summary>
        None = 0,

        // Win reasons (1-19)
        /// <summary>Win: All level objectives completed.</summary>
        AllGoalsComplete = 1,

        // Lose reasons (20-99)
        /// <summary>Lose: Ran out of time.</summary>
        TimeUp = 20,

        /// <summary>Lose: Ran out of moves.</summary>
        OutOfMoves = 21,

        /// <summary>Lose: Tray/queue overflow (match-3 style games).</summary>
        TrayFull = 22,

        /// <summary>Lose: Player chose to quit the level.</summary>
        PlayerQuit = 23,

        /// <summary>Lose: Player's health/lives depleted.</summary>
        OutOfLives = 24,

        // Game-specific reasons start at 100
        /// <summary>Game-specific reasons should use values >= 100.</summary>
        Custom = 100
    }
}
