using System;
using System.Collections.Generic;

namespace Sorolla.Events
{
    /// <summary>
    /// Game-side adapter: supplies the scheduled event definitions. Typically
    /// reads from a remote-config source (e.g. LiveConfig).
    /// </summary>
    public interface IEventCatalogProvider
    {
        IReadOnlyList<EventDefinition> GetScheduledEvents();

        /// <summary>
        /// Fired when an underlying remote-config refresh changes the schedule.
        /// EventManager listens and re-ticks the scheduler.
        /// </summary>
        event Action OnCatalogChanged;
    }
}
