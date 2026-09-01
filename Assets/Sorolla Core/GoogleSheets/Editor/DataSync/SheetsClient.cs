using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace Sorolla.GoogleSheets
{
    /// <summary>
    /// Thin wrapper over the Google Sheets v4 REST API.
    /// Authenticates via <see cref="SheetsAuth"/> (service account).
    ///
    /// Supports exactly the operations the sync tool needs:
    ///  - ReadRangeAsync  → GET values
    ///  - WriteRangeAsync → PUT values (valueInputOption=RAW)
    ///  - ClearRangeAsync → POST values:clear
    ///  - EnsureTabExistsAsync → batchUpdate addSheet if missing
    /// </summary>
    public class SheetsClient
    {
        private readonly string _spreadsheetId;
        private readonly string _credentialsPath;

        public SheetsClient(string spreadsheetId, string credentialsPath)
        {
            if (string.IsNullOrEmpty(spreadsheetId)) throw new ArgumentException("spreadsheetId empty");
            if (string.IsNullOrEmpty(credentialsPath)) throw new ArgumentException("credentialsPath empty");
            _spreadsheetId = spreadsheetId;
            _credentialsPath = credentialsPath;
        }

        public async UniTask<List<List<string>>> ReadRangeAsync(string range)
        {
            var token = await SheetsAuth.GetAccessTokenAsync(_credentialsPath);
            // UNFORMATTED_VALUE: return underlying cell values, not locale/display-formatted text
            // ("2.50" displays as-is but the value is 2.5) — display formatting must never reach the diff.
            var url = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}/values/{UnityWebRequest.EscapeURL(range)}?valueRenderOption=UNFORMATTED_VALUE";
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Authorization", $"Bearer {token}");
            await req.SendWebRequest().ToUniTask();
            ThrowIfFailed(req, "ReadRange");

            var obj = JObject.Parse(req.downloadHandler.text);
            var values = obj["values"] as JArray;
            var rows = new List<List<string>>();
            if (values == null) return rows;
            foreach (var row in values)
            {
                var cells = new List<string>();
                // JValue.ToString() uses the current culture — force invariant so numbers
                // never come back as "0,5" on comma-decimal machines.
                foreach (var cell in (JArray)row)
                    cells.Add(cell is JValue v ? v.ToString(System.Globalization.CultureInfo.InvariantCulture) : cell?.ToString() ?? string.Empty);
                rows.Add(cells);
            }
            return rows;
        }

        public async UniTask WriteRangeAsync(string range, List<List<string>> rows)
        {
            var token = await SheetsAuth.GetAccessTokenAsync(_credentialsPath);
            var url = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}/values/{UnityWebRequest.EscapeURL(range)}?valueInputOption=RAW";
            var payload = JsonConvert.SerializeObject(new { range, majorDimension = "ROWS", values = rows });

            using var req = new UnityWebRequest(url, "PUT");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", $"Bearer {token}");
            req.SetRequestHeader("Content-Type", "application/json");

            await req.SendWebRequest().ToUniTask();
            ThrowIfFailed(req, "WriteRange");
        }

        public async UniTask ClearRangeAsync(string range)
        {
            var token = await SheetsAuth.GetAccessTokenAsync(_credentialsPath);
            var url = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}/values/{UnityWebRequest.EscapeURL(range)}:clear";

            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", $"Bearer {token}");
            req.SetRequestHeader("Content-Type", "application/json");

            await req.SendWebRequest().ToUniTask();
            ThrowIfFailed(req, "ClearRange");
        }

        /// <summary>
        /// Creates the tab if missing. Quietly succeeds if it already exists.
        /// </summary>
        public async UniTask EnsureTabExistsAsync(string tabName)
        {
            var token = await SheetsAuth.GetAccessTokenAsync(_credentialsPath);

            // Query existing sheets
            var metaUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}?fields=sheets.properties.title";
            using (var meta = UnityWebRequest.Get(metaUrl))
            {
                meta.SetRequestHeader("Authorization", $"Bearer {token}");
                await meta.SendWebRequest().ToUniTask();
                ThrowIfFailed(meta, "EnsureTabExists/meta");

                var obj = JObject.Parse(meta.downloadHandler.text);
                var sheets = obj["sheets"] as JArray;
                if (sheets != null)
                {
                    foreach (var s in sheets)
                    {
                        var title = s["properties"]?["title"]?.ToString();
                        if (title == tabName) return;
                    }
                }
            }

            // Add the tab
            var addUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}:batchUpdate";
            var payload = JsonConvert.SerializeObject(new
            {
                requests = new object[]
                {
                    new { addSheet = new { properties = new { title = tabName } } }
                }
            });
            using var add = new UnityWebRequest(addUrl, UnityWebRequest.kHttpVerbPOST);
            add.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            add.downloadHandler = new DownloadHandlerBuffer();
            add.SetRequestHeader("Authorization", $"Bearer {token}");
            add.SetRequestHeader("Content-Type", "application/json");
            await add.SendWebRequest().ToUniTask();
            ThrowIfFailed(add, "EnsureTabExists/add");
        }

        private static void ThrowIfFailed(UnityWebRequest req, string op)
        {
            if (req.result == UnityWebRequest.Result.Success) return;
            var body = req.downloadHandler != null ? req.downloadHandler.text : "(no body)";
            throw new Exception($"[SheetsClient.{op}] {req.responseCode} {req.error}: {body}");
        }
    }
}
