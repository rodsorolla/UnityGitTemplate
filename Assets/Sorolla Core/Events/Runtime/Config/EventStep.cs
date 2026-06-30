using System;
using System.Collections.Generic;

namespace Sorolla.Events
{
    [Serializable]
    public sealed class EventStep
    {
        /// <summary>
        /// Collectibles needed to claim THIS step on its own (per-step delta,
        /// not cumulative). For total/cumulative checkpoint queries use
        /// <see cref="EventDefinition.CumulativeThreshold"/>.
        /// </summary>
        public int Threshold;

        /// <summary>Rewards granted when the threshold is crossed.</summary>
        public List<EventReward> Rewards = new List<EventReward>();
    }
}
