using UnityEngine;

namespace Sorolla.LiveConfig
{
    /// <summary>
    /// Runtime settings for the live-config fetcher. Place a single instance under
    /// <c>Assets/Resources/LiveConfigSettings.asset</c> so <see cref="Load"/> can
    /// find it at boot via <c>Resources.Load</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "LiveConfigSettings", menuName = "Sorolla/Live Config/Settings")]
    public class LiveConfigSettings : ScriptableObject
    {
        [Header("Endpoint")]
        [Tooltip("Google Apps Script Web App URL. Leave empty to disable network fetch and always use the baked/cached JSON.")]
        [SerializeField] private string _url;

        [Header("Timing")]
        [Tooltip("Max seconds to wait for the fetch before falling back to cache/baked. Apps Script cold starts are 1–4 seconds.")]
        [SerializeField, Min(1)] private int _timeoutSeconds = 8;

        [Header("Schema")]
        [Tooltip("Highest schemaVersion this client can parse. Server payloads above this are refused and the client falls back to cached/baked.")]
        [SerializeField, Min(1)] private int _maxSupportedSchemaVersion = 1;

        [Header("Path (relative to StreamingAssets / persistentDataPath)")]
        [Tooltip("Filename for the baked baseline inside StreamingAssets and for the cache inside persistentDataPath.")]
        [SerializeField] private string _fileName = "live_config.json";

        public string Url => _url;
        public int TimeoutSeconds => _timeoutSeconds;
        public int MaxSupportedSchemaVersion => _maxSupportedSchemaVersion;
        public string FileName => _fileName;

        public bool HasNetworkEndpoint => !string.IsNullOrWhiteSpace(_url);

        /// <summary>
        /// Load the project's settings from Resources. Returns null (and logs an error)
        /// if missing — the bootstrapper treats that as "network disabled, use baked only".
        /// </summary>
        public static LiveConfigSettings Load()
        {
            var s = Resources.Load<LiveConfigSettings>("LiveConfigSettings");
            if (s == null)
                Debug.LogError("[LiveConfig] No LiveConfigSettings found at Resources/LiveConfigSettings.asset — fetch disabled.");
            return s;
        }
    }
}
