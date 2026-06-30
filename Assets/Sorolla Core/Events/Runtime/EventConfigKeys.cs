namespace Sorolla.Events
{
    /// <summary>
    /// Static accessors for the events module's Remote Config keys.
    /// Resolves <see cref="Sorolla.IRemoteConfigProvider"/> on each access;
    /// falls back to in-app defaults when no provider is registered.
    /// Same pattern as Sorolla.Lives.LivesConfig.
    /// </summary>
    public static class EventConfigKeys
    {
        public const string KeyEnabled = "events_enabled";
        public const string KeyDefaultUnlockLevel = "events_default_unlock_level";
        public const string KeyClockRollbackGraceSeconds = "events_clock_rollback_grace_seconds";

        public const bool DefaultEnabled = true;
        public const int DefaultUnlockLevel = 18;
        public const int DefaultClockRollbackGraceSeconds = 60;

        private static Sorolla.IRemoteConfigProvider Rc =>
            ServiceLocator.Instance.TryResolve<Sorolla.IRemoteConfigProvider>()
            ?? Sorolla.DefaultRemoteConfigProvider.Instance;

        public static bool Enabled => Rc.GetBool(KeyEnabled, DefaultEnabled);
        public static int DefaultUnlockLevelValue => Rc.GetInt(KeyDefaultUnlockLevel, DefaultUnlockLevel);
        public static int ClockRollbackGraceSeconds => Rc.GetInt(KeyClockRollbackGraceSeconds, DefaultClockRollbackGraceSeconds);
    }
}
