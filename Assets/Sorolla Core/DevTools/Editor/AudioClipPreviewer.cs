using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sorolla.DevTools
{
    /// <summary>
    /// Auto-plays the selected AudioClip in the Project window so sounds can be previewed
    /// by simply clicking them. Toggle via Tools/Audio/Auto-Preview On Select.
    ///
    /// Uses the internal UnityEditor.AudioUtil preview API, so no AudioSource or Play mode
    /// is required. Resolved via reflection because AudioUtil is internal and its method
    /// signatures have shifted across Unity versions.
    ///
    /// Also exposes Tools/Audio/Reset Editor Audio, which reinitializes the editor audio
    /// output device. On macOS the editor preview can go silent after another app grabs the
    /// audio device (e.g. switching to a browser and back); the reset recovers it without
    /// restarting the editor.
    /// </summary>
    [InitializeOnLoad]
    public static class AudioClipPreviewer
    {
        private const string EnabledPref = "Sorolla.AudioClipPreviewer.Enabled";
        private const string ToggleMenu = "Tools/Audio/Auto-Preview On Select";
        private const string StopMenu = "Tools/Audio/Stop Preview";
        private const string ResetMenu = "Tools/Audio/Reset Editor Audio";

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledPref, true);
            set => EditorPrefs.SetBool(EnabledPref, value);
        }

        static AudioClipPreviewer()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            if (!Enabled) return;
            if (Selection.activeObject is AudioClip clip)
                Play(clip);
        }

        [MenuItem(ToggleMenu)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            if (!Enabled) StopAll();
        }

        [MenuItem(ToggleMenu, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(ToggleMenu, Enabled);
            return true;
        }

        [MenuItem(StopMenu)]
        private static void StopAll()
        {
            Resolve();
            try { _stop?.Invoke(null, null); }
            catch { /* preview already stopped */ }
        }

        [MenuItem(ResetMenu)]
        private static void ResetEditorAudio()
        {
            StopAll();
            // Reacquire the audio output device after a macOS audio-focus/device loss.
            bool ok = AudioSettings.Reset(AudioSettings.GetConfiguration());
            Debug.Log($"[AudioClipPreviewer] Editor audio reset ({(ok ? "ok" : "failed")}). " +
                      "Select an AudioClip to confirm preview is back.");
        }

        // --- Reflection into the internal UnityEditor.AudioUtil ---

        private static MethodInfo _play;
        private static MethodInfo _stop;
        private static bool _resolved;

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            var type = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (type == null)
            {
                Debug.LogWarning("[AudioClipPreviewer] UnityEditor.AudioUtil not found; preview disabled.");
                return;
            }

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
            // Unity 6: PlayPreviewClip(AudioClip, int startSample, bool loop). Fall back to
            // older PlayClip overloads so the tool survives an engine downgrade.
            _play = type.GetMethod("PlayPreviewClip", flags, null,
                        new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                    ?? type.GetMethod("PlayClip", flags, null,
                        new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                    ?? type.GetMethod("PlayClip", flags, null,
                        new[] { typeof(AudioClip) }, null);

            _stop = type.GetMethod("StopAllPreviewClips", flags)
                    ?? type.GetMethod("StopAllClips", flags);
        }

        private static void Play(AudioClip clip)
        {
            Resolve();
            if (_play == null) return;

            StopAll();
            var args = _play.GetParameters().Length == 3
                ? new object[] { clip, 0, false }
                : new object[] { clip };

            try { _play.Invoke(null, args); }
            catch (Exception e) { Debug.LogWarning($"[AudioClipPreviewer] Play failed: {e.Message}"); }
        }
    }
}
