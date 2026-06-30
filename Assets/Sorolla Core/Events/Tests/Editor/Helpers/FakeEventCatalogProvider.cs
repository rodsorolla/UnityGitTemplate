using System;
using System.Collections.Generic;

namespace Sorolla.Events.Tests.Helpers
{
    public sealed class FakeEventCatalogProvider : IEventCatalogProvider
    {
        public List<EventDefinition> Catalog = new List<EventDefinition>();

        public IReadOnlyList<EventDefinition> GetScheduledEvents() => Catalog;
        public event Action OnCatalogChanged;
        public void FireChanged() => OnCatalogChanged?.Invoke();
    }
}
