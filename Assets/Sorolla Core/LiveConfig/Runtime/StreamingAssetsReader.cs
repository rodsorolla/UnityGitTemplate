using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Sorolla.LiveConfig
{
    /// <summary>
    /// Cross-platform async text reader for files under <c>Application.streamingAssetsPath</c>.
    ///
    /// On Android (and WebGL) StreamingAssets live inside the APK/bundle — those need
    /// <see cref="UnityWebRequest"/>. On iOS/Desktop/Editor the path is a real filesystem
    /// location, so <see cref="File.ReadAllTextAsync"/> is enough.
    /// </summary>
    public static class StreamingAssetsReader
    {
        public static async UniTask<string> ReadTextAsync(string relativePath, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("relativePath is null or empty", nameof(relativePath));

            var full = Path.Combine(Application.streamingAssetsPath, relativePath);

#if UNITY_ANDROID && !UNITY_EDITOR
            return await ReadViaWebRequestAsync(full, ct);
#elif UNITY_WEBGL && !UNITY_EDITOR
            return await ReadViaWebRequestAsync(full, ct);
#else
            if (!File.Exists(full))
                throw new FileNotFoundException($"StreamingAssets file not found: {full}", full);
            return await File.ReadAllTextAsync(full, ct);
#endif
        }

        private static async UniTask<string> ReadViaWebRequestAsync(string uri, CancellationToken ct)
        {
            using var req = UnityWebRequest.Get(uri);
            await req.SendWebRequest().WithCancellation(ct);
            if (req.result != UnityWebRequest.Result.Success)
                throw new IOException($"StreamingAssets read failed ({req.result}): {uri} — {req.error}");
            return req.downloadHandler.text;
        }
    }
}
