namespace Sorolla.Events
{
    /// <summary>
    /// Game-side adapter: schedules / cancels local push notifications.
    /// v1 game-side implementation is a logging stub; FCM wiring (via Palette)
    /// is phase-2.
    /// </summary>
    public interface IEventNotificationScheduler
    {
        void Schedule(EventNotificationPayload payload);
        void CancelAll();
    }
}
