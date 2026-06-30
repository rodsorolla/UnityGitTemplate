using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Sorolla.Tournaments
{
    public interface ITournamentService
    {
        int CurrentTierIndex { get; }
        int PlayerTrophies { get; }
        DateTime WeekEndUtc { get; }

        /// Active tier list (remote-config override aware). Use with CurrentTierIndex
        /// to read the current league name/iconId; Count gives the tier-strip length.
        IReadOnlyList<TierDefinition> Tiers { get; }

        IReadOnlyList<LeaderboardEntry> GetLeaderboard();   // player + computed bots, sorted, ranked

        /// Award +1 trophy for a level win. Called by the game-side level-win adapter
        /// (Core cannot reference the game's LevelManager).
        void RecordLevelWin();

        /// True once the player has joined the tournament (i.e. crossed the unlock level).
        bool HasJoined { get; }

        /// First-join handshake: resets the player's trophies to 0 exactly once, discarding any
        /// accrual from before the tournament unlocked. Idempotent. Core is unlock-agnostic, so
        /// the game-side adapter decides WHEN to call this (at the unlock level).
        void EnsureJoined();

        bool HasPendingResult { get; }
        PendingResult GetPendingResult();
        UniTask ClaimPendingResultAsync();

        event Action OnTrophiesChanged;
        event Action OnTournamentRolledOver;
    }
}
