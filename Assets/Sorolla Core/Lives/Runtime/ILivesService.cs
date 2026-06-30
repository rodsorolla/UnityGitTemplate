using System;
using Sorolla.LevelFlow;

namespace Sorolla.Lives
{
    /// <summary>
    /// Reusable lives/hearts service. Registered with ServiceLocator at game startup.
    /// All time-based logic operates in UTC and tolerates app-close gaps via catch-up
    /// in <see cref="Current"/> (which calls Tick internally on each access).
    /// </summary>
    public interface ILivesService
    {
        /// <summary>Current lives, clamped to [0, Max]. Reads run a Tick to advance regen lazily.</summary>
        int Current { get; }

        /// <summary>Max lives (read from RC each access).</summary>
        int Max { get; }

        /// <summary>True while an infinite-lives booster is active.</summary>
        bool IsBoosterActive { get; }

        /// <summary>True iff Current == Max.</summary>
        bool IsAtMax { get; }

        /// <summary>Time until the next single-life regen. TimeSpan.Zero when at max.</summary>
        TimeSpan TimeUntilNextLife { get; }

        /// <summary>Remaining duration of the active booster. TimeSpan.Zero when none.</summary>
        TimeSpan BoosterTimeRemaining { get; }

        /// <summary>True if backward-clock-jump cheat was detected this session.</summary>
        bool LastClockRollbackDetected { get; }

        /// <summary>
        /// True iff the most recent OnLevelEnded callback resulted in a life
        /// deduction. Consumers (e.g. paid-continue revival) read this to know
        /// whether they need to refund a life. Reset on every OnLevelEnded.
        /// </summary>
        bool LastLossConsumedLife { get; }

        /// <summary>
        /// True when the lives system should gate / decrement for this level.
        /// False below LivesConfig.LivesSystemMinLevel — used by UI and the level-start gate.
        /// </summary>
        bool IsActiveForLevel(int progressiveLevelIndex);

        /// <summary>
        /// Decrements a life if the rules allow (loss reason, level threshold, no booster, lives > 0).
        /// Returns true iff a life was actually deducted.
        /// </summary>
        bool TryConsumeLifeForLoss(LevelEndReason reason, int progressiveLevelIndex);

        /// <summary>Sets Current to Max. Used by ad rewards / coin refills / store grants.</summary>
        void RefillToMax();

        /// <summary>Adds N lives, capped at Max.</summary>
        void AddLives(int count);

        /// <summary>
        /// Activates / extends the infinite-lives booster. First activation refills to Max.
        /// Multi-purchase extends remaining time (10 min left + 30 min activation = 40 min).
        /// </summary>
        void ActivateInfiniteLivesBooster(TimeSpan duration);

        // ---- Events ----
        event Action<int> OnCurrentChanged;
        event Action OnRegenAdvanced;
        event Action<TimeSpan> OnBoosterActivated;
        event Action OnBoosterExpired;
        event Action OnClockRollbackDetected;
    }
}
