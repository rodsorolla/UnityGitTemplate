using System;

namespace Sorolla.Events
{
    public sealed class EventNotificationPayload
    {
        public string EventId;
        public string Title;
        public string Body;
        public DateTime FireAtUtc;

        /// <summary>Stable id so duplicates can be cancelled/overwritten.</summary>
        public string NotificationId;
    }
}
