using System;

namespace Sorolla.Events
{
    /// <summary>
    /// Persisted progress for a single touched event instance.
    /// One entry per eventId in <see cref="EventsSaveData.instances"/>.
    /// Removed when the event's window has fully passed (forfeit rule).
    /// </summary>
    [Serializable]
    public sealed class EventInstanceProgress
    {
        public string eventId;

        /// <summary>Total collectibles committed for this event.</summary>
        public int progress;

        /// <summary>Bit i = step i has been claimed. Caps at 64 steps.</summary>
        public ulong claimedStepBitset;

        public bool grandPrizeClaimed;

        public string firstSeenUtcIso;
    }
}
