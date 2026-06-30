using System.Collections.Generic;

namespace Sorolla.Events.Tests.Helpers
{
    public sealed class FakeEventNotificationScheduler : IEventNotificationScheduler
    {
        public readonly List<EventNotificationPayload> Scheduled = new List<EventNotificationPayload>();
        public int CancelAllCount;

        public void Schedule(EventNotificationPayload payload) => Scheduled.Add(payload);
        public void CancelAll() => CancelAllCount++;
    }
}
