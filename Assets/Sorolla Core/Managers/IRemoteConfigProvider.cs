namespace Sorolla
{
    /// <summary>
    /// Abstraction over a remote-config source. Implementations read from
    /// vendor SDKs (Firebase, GameAnalytics, Palette, etc.) or stub values for tests.
    /// Sorolla Core code MUST resolve via ServiceLocator and never reference any
    /// vendor SDK directly. Falls back to <see cref="DefaultRemoteConfigProvider"/>
    /// when no implementation is registered.
    /// </summary>
    public interface IRemoteConfigProvider
    {
        int GetInt(string key, int defaultValue);
        long GetLong(string key, long defaultValue);
        float GetFloat(string key, float defaultValue);
        bool GetBool(string key, bool defaultValue);
        string GetString(string key, string defaultValue);
    }

    /// <summary>
    /// No-op provider that always returns the caller-supplied default.
    /// Used when no real provider has been registered (e.g., Palette package not present).
    /// </summary>
    public sealed class DefaultRemoteConfigProvider : IRemoteConfigProvider
    {
        public static readonly DefaultRemoteConfigProvider Instance = new DefaultRemoteConfigProvider();

        private DefaultRemoteConfigProvider() { }

        public int GetInt(string key, int defaultValue) => defaultValue;
        public long GetLong(string key, long defaultValue) => defaultValue;
        public float GetFloat(string key, float defaultValue) => defaultValue;
        public bool GetBool(string key, bool defaultValue) => defaultValue;
        public string GetString(string key, string defaultValue) => defaultValue;
    }
}
