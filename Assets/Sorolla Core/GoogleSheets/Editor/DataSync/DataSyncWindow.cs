using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Sorolla.GoogleSheets
{
    /// <summary>
    /// Editor window for bi-directional sync between ScriptableObject data and a Google Spreadsheet.
    /// Game-agnostic: discovers every concrete <see cref="IDataSyncTab"/> in loaded editor assemblies
    /// and lists one row per tab with Push / Diff / Pull buttons.
    ///
    /// To add a tab to the window, just create a class that implements <see cref="IDataSyncTab"/>
    /// (typically by deriving from <c>SingleAssetTab&lt;T&gt;</c> or <c>CollectionTab&lt;TBase&gt;</c>)
    /// and expose a parameterless constructor — no registration step.
    /// </summary>
    public class DataSyncWindow : EditorWindow
    {
        // ---- Foldout persistence keys ----
        private const string FoldConfigKey = "Sorolla.DataSync.Window.FoldConfig";
        private const string FoldAllKey = "Sorolla.DataSync.Window.FoldAll";
        private const string FoldTabsKey = "Sorolla.DataSync.Window.FoldTabs";
        private const string FoldLogKey = "Sorolla.DataSync.Window.FoldLog";
        private const string LogHeightKey = "Sorolla.DataSync.Window.LogHeight";

        // ---- Palette ----
        private static readonly Color SafeBanner = new Color(0.22f, 0.74f, 0.38f, 0.90f);   // green
        private static readonly Color DangerBanner = new Color(0.90f, 0.30f, 0.30f, 0.92f); // red
        private static readonly Color MissingBanner = new Color(0.75f, 0.75f, 0.30f, 0.90f);// yellow (config missing)
        private static readonly Color PushBtn = new Color(0.55f, 0.85f, 1.00f, 1f);         // cyan
        private static readonly Color DiffBtn = new Color(1.00f, 0.82f, 0.40f, 1f);         // amber
        private static readonly Color PullBtn = new Color(1.00f, 0.60f, 0.60f, 1f);         // red-ish
        private static readonly Color LogInfo = new Color(0.82f, 0.86f, 0.92f, 1f);
        private static readonly Color LogWarn = new Color(1.00f, 0.82f, 0.36f, 1f);
        private static readonly Color LogError = new Color(1.00f, 0.50f, 0.50f, 1f);
        private static readonly Color LogOk = new Color(0.55f, 0.92f, 0.60f, 1f);

        private DataSyncConfig _config;
        private Vector2 _scroll;
        private Vector2 _logScroll;
        private bool _isBusy;
        private IDataSyncTab[] _tabs;

        private bool _foldConfig;
        private bool _foldAll;
        private bool _foldTabs;
        private bool _foldLog;
        private float _logHeight;
        private bool _resizingLog;
        private bool _autoScrollLog = true;

        private readonly List<LogEntry> _log = new();
        private DateTime? _lastActionAt;
        private DateTime? _busyStartedAt;
        private string _busyLabel = string.Empty;

        private enum LogLevel { Info, Ok, Warn, Error }
        private struct LogEntry
        {
            public DateTime At;
            public LogLevel Level;
            public string Message;
        }

        [MenuItem("Tools/Sorolla Core/Data Sync")]
        public static void Open() => GetWindow<DataSyncWindow>("Data ↔ Sheet Sync").minSize = new Vector2(580, 680);

        private void OnEnable()
        {
            var guids = AssetDatabase.FindAssets("t:DataSyncConfig");
            if (guids.Length > 0)
                _config = AssetDatabase.LoadAssetAtPath<DataSyncConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
            _tabs = DiscoverTabs();

            _foldConfig = EditorPrefs.GetBool(FoldConfigKey, true);
            _foldAll = EditorPrefs.GetBool(FoldAllKey, true);
            _foldTabs = EditorPrefs.GetBool(FoldTabsKey, true);
            _foldLog = EditorPrefs.GetBool(FoldLogKey, true);
            _logHeight = Mathf.Clamp(EditorPrefs.GetFloat(LogHeightKey, 160f), 80f, 600f);
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool(FoldConfigKey, _foldConfig);
            EditorPrefs.SetBool(FoldAllKey, _foldAll);
            EditorPrefs.SetBool(FoldTabsKey, _foldTabs);
            EditorPrefs.SetBool(FoldLogKey, _foldLog);
            EditorPrefs.SetFloat(LogHeightKey, _logHeight);
        }

        private void Update()
        {
            // Repaint while busy so the elapsed timer ticks.
            if (_isBusy) Repaint();
        }

        private static IDataSyncTab[] DiscoverTabs()
        {
            var list = new List<IDataSyncTab>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t == null || t.IsAbstract || t.IsGenericTypeDefinition || t.IsInterface) continue;
                    if (!typeof(IDataSyncTab).IsAssignableFrom(t)) continue;
                    if (t.GetConstructor(Type.EmptyTypes) == null) continue;
                    try { list.Add((IDataSyncTab)Activator.CreateInstance(t)); }
                    catch (Exception e) { Debug.LogWarning($"[DataSyncWindow] Could not instantiate tab {t.FullName}: {e.Message}"); }
                }
            }
            return list.OrderBy(t => t.TabName, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private void OnGUI()
        {
            DrawSafetyBanner();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawSection("Config", ref _foldConfig, DrawConfigBlock);

            if (_config != null && HasRequiredConfig())
            {
                DrawSection("All Tabs", ref _foldAll, DrawSyncAllButtons);
                DrawSection($"Per-Tab ({_tabs.Length} discovered)", ref _foldTabs, DrawTabList);
            }

            EditorGUILayout.EndScrollView();

            // Log stays docked at the bottom outside the main scroll.
            DrawLogPanel();
        }

        // ---- Safety banner ----

        private void DrawSafetyBanner()
        {
            Color color;
            string title;
            string hint;

            if (_config == null || !HasRequiredConfig())
            {
                color = MissingBanner;
                title = "CONFIG INCOMPLETE  ·  Sync disabled";
                hint = "Fill in the DataSyncConfig below to enable Push / Diff / Pull.";
            }
            else if (_config.AllowDeletionsOnPull)
            {
                color = DangerBanner;
                title = "DELETIONS ENABLED  ·  Pulls can delete assets";
                hint = "\"Allow deletions on Pull\" is ON. Sheet removals will delete ScriptableObject files.";
            }
            else
            {
                color = SafeBanner;
                title = "SAFE MODE  ·  Pulls will not delete assets";
                hint = "Deletions are blocked on Pull. Toggle in Config if you need destructive sync.";
            }

            var rect = GUILayoutUtility.GetRect(1, 46, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, color);

            var inner = new Rect(rect.x + 10, rect.y + 6, rect.width - 20, rect.height - 12);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.black },
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            var hintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0f, 0f, 0f, 0.70f) },
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };
            GUI.Label(new Rect(inner.x, inner.y, inner.width, 18), title, titleStyle);
            GUI.Label(new Rect(inner.x, inner.y + 18, inner.width, 16), hint, hintStyle);

            // Subheader with status stamp / busy timer.
            using (new EditorGUILayout.HorizontalScope())
            {
                var stampStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
                string stamp;
                if (_isBusy && _busyStartedAt.HasValue)
                {
                    var elapsed = DateTime.Now - _busyStartedAt.Value;
                    stamp = $"Working: {_busyLabel}  ·  {elapsed.TotalSeconds:0.0}s";
                }
                else if (_lastActionAt.HasValue)
                {
                    stamp = $"Last action: {_lastActionAt.Value:HH:mm:ss}";
                }
                else
                {
                    stamp = "No actions yet";
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label(stamp, stampStyle);
            }
            EditorGUILayout.Space(2);
        }

        // ---- Section scaffold ----

        private delegate void SectionBody();

        private void DrawSection(string title, ref bool expanded, SectionBody body)
        {
            expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, title);
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (!expanded)
            {
                EditorGUILayout.Space(2);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                body();
            }
            EditorGUILayout.Space(4);
        }

        // ---- Sections ----

        private void DrawConfigBlock()
        {
            EditorGUI.BeginChangeCheck();
            _config = (DataSyncConfig)EditorGUILayout.ObjectField("Config", _config, typeof(DataSyncConfig), false);
            if (EditorGUI.EndChangeCheck() && _config != null) EditorUtility.SetDirty(_config);

            if (_config == null)
            {
                EditorGUILayout.HelpBox("No DataSyncConfig found. Create one via Assets → Create → Sorolla → Google Sheets → Data Sync Config.", MessageType.Warning);
                if (GUILayout.Button("Create DataSyncConfig")) CreateConfig();
                return;
            }

            EditorGUI.BeginChangeCheck();
            _config.SpreadsheetId = EditorGUILayout.TextField(new GUIContent("Spreadsheet ID", "The long token in the sheet URL between /d/ and /edit."), _config.SpreadsheetId);
            _config.CredentialsPath = EditorGUILayout.TextField(new GUIContent("Credentials Path", "Path to the Google service account credentials.json."), _config.CredentialsPath);
            _config.DefaultOutputFolder = EditorGUILayout.TextField(new GUIContent("New-Asset Folder", "Where Pull will create new ScriptableObject assets."), _config.DefaultOutputFolder);
            _config.AllowDeletionsOnPull = EditorGUILayout.ToggleLeft(new GUIContent("Allow deletions on Pull (dangerous)", "When on, rows missing from the sheet cause the matching Unity asset files to be deleted."), _config.AllowDeletionsOnPull);
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(_config);

            if (string.IsNullOrWhiteSpace(_config.SpreadsheetId))
                EditorGUILayout.HelpBox("Set the spreadsheet id (the long token in the sheet URL between /d/ and /edit).", MessageType.Warning);
            if (!File.Exists(_config.CredentialsPath))
                EditorGUILayout.HelpBox($"credentials.json not found at: {_config.CredentialsPath}\nSee the SheetsCredentials README for setup.", MessageType.Warning);

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_config.SpreadsheetId)))
                {
                    if (GUILayout.Button(new GUIContent("Open Sheet", "Open the spreadsheet in your browser."), GUILayout.Height(22)))
                        Application.OpenURL($"https://docs.google.com/spreadsheets/d/{_config.SpreadsheetId}/edit");
                    if (GUILayout.Button(new GUIContent("Copy Sheet ID", "Copy the spreadsheet id to the clipboard."), GUILayout.Height(22)))
                    {
                        EditorGUIUtility.systemCopyBuffer = _config.SpreadsheetId;
                        Log(LogLevel.Info, "Copied Spreadsheet ID to clipboard.");
                    }
                }
                if (GUILayout.Button(new GUIContent("Ping Config", "Reveal the DataSyncConfig asset in the Project window."), GUILayout.Width(100), GUILayout.Height(22)))
                    EditorGUIUtility.PingObject(_config);
            }
        }

        private bool HasRequiredConfig() =>
            _config != null
            && !string.IsNullOrWhiteSpace(_config.SpreadsheetId)
            && File.Exists(_config.CredentialsPath);

        private void DrawSyncAllButtons()
        {
            using (new EditorGUI.DisabledScope(_isBusy))
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new TintScope(PushBtn))
                    if (GUILayout.Button(new GUIContent("Push All →", "Write every tab's assets up to the sheet."), GUILayout.Height(28)))
                        RunAsync("Push All", PushAllAsync);

                using (new TintScope(DiffBtn))
                    if (GUILayout.Button(new GUIContent("Diff All", "Compare every tab's assets against the sheet without changing anything."), GUILayout.Height(28)))
                        RunAsync("Diff All", DiffAllAsync);

                using (new TintScope(PullBtn))
                    if (GUILayout.Button(new GUIContent("Pull All ←", "Read every tab from the sheet into Unity assets."), GUILayout.Height(28)))
                        RunAsync("Pull All", PullAllAsync);
            }
        }

        private void DrawTabList()
        {
            if (_tabs.Length == 0)
            {
                EditorGUILayout.HelpBox("No IDataSyncTab implementations found. Define classes implementing IDataSyncTab (or derive from SingleAssetTab<T> / CollectionTab<TBase>) and they will appear here automatically.", MessageType.Info);
                return;
            }
            foreach (var tab in _tabs)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(tab.TabName, EditorStyles.boldLabel, GUILayout.Width(180));
                    using (new EditorGUI.DisabledScope(_isBusy))
                    {
                        using (new TintScope(PushBtn))
                            if (GUILayout.Button(new GUIContent("Push →", $"Write {tab.TabName} assets up to the sheet."), GUILayout.Width(72)))
                                RunAsync($"Push {tab.TabName}", () => PushOneAsync(tab));
                        using (new TintScope(DiffBtn))
                            if (GUILayout.Button(new GUIContent("Diff", $"Compare {tab.TabName} assets vs sheet."), GUILayout.Width(62)))
                                RunAsync($"Diff {tab.TabName}", () => DiffOneAsync(tab));
                        using (new TintScope(PullBtn))
                            if (GUILayout.Button(new GUIContent("Pull ←", $"Read {tab.TabName} from the sheet into assets."), GUILayout.Width(72)))
                                RunAsync($"Pull {tab.TabName}", () => PullOneAsync(tab));
                    }
                }
            }
        }

        // ---- Log panel ----

        private void DrawLogPanel()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _foldLog = EditorGUILayout.BeginFoldoutHeaderGroup(_foldLog, $"Log ({_log.Count})");
                EditorGUILayout.EndFoldoutHeaderGroup();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Copy", "Copy all log entries to the clipboard."), EditorStyles.miniButton, GUILayout.Width(54)))
                    CopyLog();
                if (GUILayout.Button(new GUIContent("Clear", "Clear the log."), EditorStyles.miniButton, GUILayout.Width(54)))
                    _log.Clear();
            }

            if (!_foldLog) return;

            // Drag handle above the log — drag up to enlarge, down to shrink.
            var handle = GUILayoutUtility.GetRect(1, 6, GUILayout.ExpandWidth(true));
            EditorGUIUtility.AddCursorRect(handle, MouseCursor.ResizeVertical);
            EditorGUI.DrawRect(new Rect(handle.x, handle.y + 2, handle.width, 2), new Color(1f, 1f, 1f, 0.08f));
            var ev = Event.current;
            switch (ev.type)
            {
                case EventType.MouseDown:
                    if (ev.button == 0 && handle.Contains(ev.mousePosition))
                    {
                        _resizingLog = true;
                        ev.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (_resizingLog)
                    {
                        _logHeight = Mathf.Clamp(_logHeight - ev.delta.y, 80f, 600f);
                        Repaint();
                        ev.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (_resizingLog)
                    {
                        _resizingLog = false;
                        ev.Use();
                    }
                    break;
            }

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, EditorStyles.helpBox, GUILayout.Height(_logHeight));
            var entryStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true,
                fontSize = 11
            };
            if (_log.Count == 0)
            {
                GUILayout.Label("<i>No entries yet. Run Push / Diff / Pull to populate.</i>", entryStyle);
            }
            else
            {
                for (int i = 0; i < _log.Count; i++)
                {
                    var e = _log[i];
                    string hex = ColorUtility.ToHtmlStringRGB(LevelColor(e.Level));
                    string prefix = $"<color=#8aa0b0>[{e.At:HH:mm:ss}]</color> <b><color=#{hex}>{e.Level.ToString().ToUpperInvariant()}</color></b>";
                    GUILayout.Label($"{prefix}  {e.Message}", entryStyle);
                }
                if (_autoScrollLog && Event.current.type == EventType.Repaint)
                    _logScroll.y = Mathf.Max(0f, GUILayoutUtility.GetLastRect().yMax - _logHeight);
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                _autoScrollLog = GUILayout.Toggle(_autoScrollLog, new GUIContent("Auto-scroll", "Keep the log pinned to the newest entry."), EditorStyles.miniButton, GUILayout.Width(90));
            }
        }

        private static Color LevelColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Ok: return LogOk;
                case LogLevel.Warn: return LogWarn;
                case LogLevel.Error: return LogError;
                default: return LogInfo;
            }
        }

        private void Log(LogLevel level, string message)
        {
            _log.Add(new LogEntry { At = DateTime.Now, Level = level, Message = message });
            _lastActionAt = DateTime.Now;
            const int Cap = 500;
            if (_log.Count > Cap) _log.RemoveRange(0, _log.Count - Cap);
            Repaint();
        }

        private void CopyLog()
        {
            var sb = new StringBuilder();
            foreach (var e in _log)
                sb.AppendLine($"[{e.At:HH:mm:ss}] {e.Level.ToString().ToUpperInvariant()}  {e.Message}");
            EditorGUIUtility.systemCopyBuffer = sb.ToString();
            Log(LogLevel.Info, $"Copied {_log.Count} log entries to clipboard.");
        }

        // ---- Orchestration ----

        private SheetsClient NewClient() => new SheetsClient(_config.SpreadsheetId, _config.CredentialsPath);

        private async UniTask PushOneAsync(IDataSyncTab tab)
        {
            var client = NewClient();
            Log(LogLevel.Info, $"[{tab.TabName}] Push: preparing…");
            await client.EnsureTabExistsAsync(tab.TabName);
            var rows = tab.ReadFromAssets();
            await client.ClearRangeAsync($"{tab.TabName}!A1:ZZ");
            await client.WriteRangeAsync($"{tab.TabName}!A1", rows);
            Log(LogLevel.Ok, $"[{tab.TabName}] Push complete — {rows.Count - 1} row(s).");
        }

        private async UniTask DiffOneAsync(IDataSyncTab tab)
        {
            var client = NewClient();
            Log(LogLevel.Info, $"[{tab.TabName}] Diff: fetching…");
            var sheetRows = await client.ReadRangeAsync($"{tab.TabName}!A1:ZZ");
            var diff = tab.BuildDiff(sheetRows);
            if (diff.HasChanges)
            {
                Log(LogLevel.Warn, $"[{tab.TabName}] changes detected — see details below.");
                foreach (var line in diff.Details().Split('\n'))
                    if (!string.IsNullOrWhiteSpace(line)) Log(LogLevel.Info, line);
            }
            else
            {
                Log(LogLevel.Ok, $"[{tab.TabName}] no changes.");
            }
        }

        private async UniTask PullOneAsync(IDataSyncTab tab)
        {
            var client = NewClient();
            Log(LogLevel.Info, $"[{tab.TabName}] Pull: fetching…");
            var sheetRows = await client.ReadRangeAsync($"{tab.TabName}!A1:ZZ");

            var diff = tab.BuildDiff(sheetRows);
            if (!diff.HasChanges) { Log(LogLevel.Ok, $"[{tab.TabName}] no changes."); return; }

            foreach (var line in diff.Details().Split('\n'))
                if (!string.IsNullOrWhiteSpace(line)) Log(LogLevel.Info, line);

            bool needConfirm = diff.Adds.Count + diff.Deletes.Count > 0 || diff.Modifies.Count > 3;
            if (needConfirm)
            {
                var msg = $"[{tab.TabName}] About to apply:\n+{diff.Adds.Count} adds\n~{diff.Modifies.Count} mods\n-{diff.Deletes.Count} deletes\n\nAllowDeletionsOnPull = {_config.AllowDeletionsOnPull}";
                if (!EditorUtility.DisplayDialog("Confirm Pull", msg, "Apply", "Cancel"))
                {
                    Log(LogLevel.Warn, $"[{tab.TabName}] Pull cancelled by user.");
                    return;
                }
            }

            tab.WriteToAssets(sheetRows, _config.AllowDeletionsOnPull);
            Log(LogLevel.Ok, $"[{tab.TabName}] Pull applied — +{diff.Adds.Count} / ~{diff.Modifies.Count} / -{diff.Deletes.Count}.");
        }

        private async UniTask PushAllAsync() { foreach (var t in _tabs) await PushOneAsync(t); Log(LogLevel.Ok, "All tabs pushed."); }
        private async UniTask DiffAllAsync() { foreach (var t in _tabs) await DiffOneAsync(t); Log(LogLevel.Info, "Diff All complete."); }
        private async UniTask PullAllAsync() { foreach (var t in _tabs) await PullOneAsync(t); Log(LogLevel.Ok, "All tabs pulled."); }

        private void RunAsync(string label, Func<UniTask> op) => RunAsyncInternal(label, op).Forget();

        private async UniTaskVoid RunAsyncInternal(string label, Func<UniTask> op)
        {
            if (_isBusy) return;
            _isBusy = true;
            _busyStartedAt = DateTime.Now;
            _busyLabel = label;
            Repaint();
            try { await op(); }
            catch (Exception e) { Log(LogLevel.Error, $"[{label}] {e.Message}"); Debug.LogException(e); }
            finally
            {
                _isBusy = false;
                _busyStartedAt = null;
                _busyLabel = string.Empty;
                Repaint();
            }
        }

        private void CreateConfig()
        {
            var folder = Path.GetDirectoryName(_config != null ? AssetDatabase.GetAssetPath(_config) : null);
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            {
                folder = "Assets/Editor/SheetsCredentials";
                EnsureFolder(folder);
            }
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/DataSyncConfig.asset");
            var asset = ScriptableObject.CreateInstance<DataSyncConfig>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _config = asset;
            EditorGUIUtility.PingObject(asset);
            Log(LogLevel.Ok, $"Created {path}");
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // ---- Small RAII helper for GUI tint ----

        private readonly struct TintScope : IDisposable
        {
            private readonly Color _prev;
            public TintScope(Color color)
            {
                _prev = GUI.backgroundColor;
                GUI.backgroundColor = color;
            }
            public void Dispose() => GUI.backgroundColor = _prev;
        }
    }
}
