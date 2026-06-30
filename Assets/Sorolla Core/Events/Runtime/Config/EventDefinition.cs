using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sorolla.Events
{
    /// <summary>
    /// Catalog entry describing one scheduled event instance. Produced by
    /// <see cref="IEventCatalogProvider"/>; consumed by <see cref="EventScheduler"/>
    /// and <see cref="EventManager"/>.
    /// </summary>
    [Serializable]
    public sealed class EventDefinition
    {
        /// <summary>Unique id, e.g. "goldenfruit_w19".</summary>
        public string EventId;

        /// <summary>Reserved for future view-model dispatch, e.g. "treasure_hunt", "butlers_gift".</summary>
        public string EventType;

        public string DisplayName;

        /// <summary>Theme tint applied to event UI (info-panel background, menu tile art).
        /// Defaults to white (neutral); only applied by UI when alpha > 0.</summary>
        public Color EventColor = Color.white;

        /// <summary>Long-form description shown in the event's info panel.
        /// Plain text; line breaks supported.</summary>
        public string Description;

        /// <summary>Game-side asset map resolves this to a Sprite/prefab.</summary>
        public string CollectibleAssetId;

        public string ThemeBundleId;

        /// <summary>UTC day of week the active window opens (00:00 UTC of this weekday). Inclusive.</summary>
        public DayOfWeek StartDayOfWeek = DayOfWeek.Monday;

        /// <summary>UTC day of week the active window closes. Inclusive — the event remains
        /// active through 23:59:59 UTC of this weekday and ends at the following 00:00 UTC.
        /// When <see cref="EndDayOfWeek"/> precedes <see cref="StartDayOfWeek"/> in the week,
        /// the window wraps through Sunday (e.g. Fri→Sun, Sat→Mon).</summary>
        public DayOfWeek EndDayOfWeek = DayOfWeek.Sunday;

        /// <summary>-1 = use EventConfigKeys.DefaultUnlockLevel.</summary>
        public int UnlockLevel = -1;

        /// <summary>Multiplier applied when game reports a hard-tier level.</summary>
        public float HardLevelMultiplier = 1f;

        public List<EventStep> Steps = new List<EventStep>();

        public EventReward GrandPrize;

        /// <summary>
        /// Populated for <c>EventType == "win_streak"</c> entries only.
        /// Exactly three entries describe the booster loadout at each tier.
        /// Order is ascending by <see cref="WinStreakTier.ThresholdWins"/>.
        /// </summary>
        public List<WinStreakTier> WinStreakTiers = new List<WinStreakTier>();

        /// <summary>
        /// For <c>EventType == "win_streak"</c>: whether quitting to menu mid-level
        /// counts as a streak reset. Defaults to true (matches spec). Ignored by other archetypes.
        /// </summary>
        public bool ResetOnQuit = true;

        /// <summary>
        /// Sum of <see cref="EventStep.Threshold"/> for steps [0..stepIndex].
        /// EventStep.Threshold is a per-step delta; this method converts that
        /// into the cumulative checkpoint where step <paramref name="stepIndex"/>
        /// becomes claimable.
        /// </summary>
        public int CumulativeThreshold(int stepIndex)
        {
            if (Steps == null || stepIndex < 0) return 0;
            int last = stepIndex < Steps.Count ? stepIndex : Steps.Count - 1;
            int sum = 0;
            for (int i = 0; i <= last; i++) sum += Steps[i].Threshold;
            return sum;
        }

        /// <summary>Total collectibles needed to claim every step (sum of all deltas).</summary>
        public int TotalThreshold()
        {
            if (Steps == null) return 0;
            int sum = 0;
            for (int i = 0; i < Steps.Count; i++) sum += Steps[i].Threshold;
            return sum;
        }
    }
}
