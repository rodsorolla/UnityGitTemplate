using System;
using System.Collections.Generic;

namespace Sorolla.Events
{
    /// <summary>
    /// One tier in a WinStreak event. Reached when the player's consecutive-win
    /// counter is >= <see cref="ThresholdWins"/>. The granted booster ids are
    /// preloaded into the snake at the start of the next level.
    /// </summary>
    [Serializable]
    public sealed class WinStreakTier
    {
        public int ThresholdWins;
        public List<string> BoosterIds = new List<string>();
    }
}
