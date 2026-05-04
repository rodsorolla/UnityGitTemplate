using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Sorolla.LiveConfig
{
    /// <summary>
    /// Single-responsibility HTTP GET wrapper for the live-config endpoint.
    /// Returns the raw JSON string on success; throws on timeout, network error,
    /// or non-200 status so the bootstrapper can fall back to the cache.
    /// </summary>
    public static class LiveConfigFetcher
    {
        public static async UniTask<string> FetchAsync(string url, int timeoutSeconds, CancellationToken externalCt)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("url is null or empty", nameof(url));

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var req = UnityWebRequest.Get(url);
            req.timeout = timeoutSeconds; // also enforced by the CTS as a belt-and-suspenders

            try
            {
                await req.SendWebRequest().WithCancellation(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!externalCt.IsCancellationRequested)
            {
                throw new TimeoutException($"[LiveConfig] Fetch timed out after {timeoutSeconds}s: {url}");
            }

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"[LiveConfig] Fetch failed ({req.result}) — {req.error}");

            if (req.responseCode != 200)
                throw new Exception($"[LiveConfig] Fetch HTTP {req.responseCode}: {url}");

            return req.downloadHandler.text;
        }
    }
}
