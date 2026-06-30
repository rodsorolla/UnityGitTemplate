using System.Collections.Generic;
using PaletteSdk = Sorolla.Palette.Palette;

namespace Sorolla.Events
{
    /// <summary>
    /// Analytics fan-out for the events module. Routes through Palette
    /// (CLAUDE.md and .claude/rules/palette-boundary.md require Palette for all
    /// analytics; no vendor SDK is referenced here).
    /// </summary>
    public static class EventTelemetry
    {
        public static void TrackEventStarted(string eventId)
            => PaletteSdk.TrackEvent($"event_started:{eventId}", null);

        public static void TrackEventEnded(string eventId, EventEndReason reason, int finalProgress, int stepsClaimed, bool grandPrizeClaimed)
        {
            var parameters = new Dictionary<string, object>
            {
                { "reason", reason.ToString() },
                { "final_progress", finalProgress },
                { "steps_claimed", stepsClaimed },
                { "grand_prize_claimed", grandPrizeClaimed }
            };
            PaletteSdk.TrackEvent($"event_ended:{eventId}", parameters);
        }

        public static void TrackProgress(string eventId, int delta, int newTotal)
        {
            var parameters = new Dictionary<string, object>
            {
                { "delta", delta },
                { "new_total", newTotal }
            };
            PaletteSdk.TrackEvent($"event_progress:{eventId}", parameters);
        }

        public static void TrackStepClaimed(string eventId, int stepIndex, int threshold)
        {
            var parameters = new Dictionary<string, object>
            {
                { "step_index", stepIndex },
                { "threshold", threshold }
            };
            PaletteSdk.TrackEvent($"event_step_claimed:{eventId}", parameters);
        }

        public static void TrackGrandPrizeClaimed(string eventId)
            => PaletteSdk.TrackEvent($"event_grand_prize_claimed:{eventId}", null);

        public static void TrackClockRollback()
            => PaletteSdk.TrackEvent("event_clock_rollback", null);
    }
}
