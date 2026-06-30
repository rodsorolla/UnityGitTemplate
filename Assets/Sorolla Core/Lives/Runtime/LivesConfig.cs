namespace Sorolla.Lives
{
    /// <summary>
    /// Static accessors for the lives system's Remote Config keys.
    /// Resolves IRemoteConfigProvider via ServiceLocator on each call;
    /// falls back to in-app defaults when no provider is registered (e.g.,
    /// Sorolla Palette package not installed).
    /// </summary>
    public static class LivesConfig
    {
        public const string KeyMaxLives = "lives_max";
        public const string KeyRegenIntervalSeconds = "lives_regen_interval_seconds";
        public const string KeyLivesSystemMinLevel = "lives_system_min_level";
        public const string KeyBoosterDefaultDurationSeconds = "lives_booster_default_duration_seconds";
        public const string KeyRefillCoinsCost = "lives_refill_coins_cost";

        public const int DefaultMaxLives = 5;
        public const int DefaultRegenIntervalSeconds = 1800;
        public const int DefaultLivesSystemMinLevel = 5;
        public const int DefaultBoosterDefaultDurationSeconds = 1800;
        public const int DefaultRefillCoinsCost = 900;

        private static Sorolla.IRemoteConfigProvider Rc =>
            ServiceLocator.Instance.TryResolve<Sorolla.IRemoteConfigProvider>()
            ?? Sorolla.DefaultRemoteConfigProvider.Instance;

        public static int MaxLives => Rc.GetInt(KeyMaxLives, DefaultMaxLives);
        public static int RegenIntervalSeconds => Rc.GetInt(KeyRegenIntervalSeconds, DefaultRegenIntervalSeconds);
        public static int LivesSystemMinLevel => Rc.GetInt(KeyLivesSystemMinLevel, DefaultLivesSystemMinLevel);
        public static int BoosterDefaultDurationSeconds => Rc.GetInt(KeyBoosterDefaultDurationSeconds, DefaultBoosterDefaultDurationSeconds);
        public static int RefillCoinsCost => Rc.GetInt(KeyRefillCoinsCost, DefaultRefillCoinsCost);
    }
}
