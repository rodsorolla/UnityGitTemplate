using TMPro;
using UnityEngine;

namespace Sorolla.Utils
{
    /// <summary>
    /// Mirrors text from a source TMP_Text to this component's TMP_Text.
    /// Works with both TextMeshProUGUI (Canvas) and TextMeshPro (3D world).
    ///
    /// At runtime it syncs in LateUpdate (self-healing every frame). In the editor it
    /// also syncs on the editor tick so the mirror updates live while you type into the
    /// source. It deliberately does NOT hook TMP's TEXT_CHANGED_EVENT: writing TMP text
    /// from inside that broadcast is unsafe (re-entrant LinkedList iteration) and could
    /// leave the target desynced after a scene/level change.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public class TMPTextMirror : MonoBehaviour
    {
        [SerializeField] private TMP_Text _source;
        [Tooltip("Also copy the source's font size onto the target. Leave off to keep the target's own size.")]
        [SerializeField] private bool _mirrorFontSize;

        private TMP_Text _target;
        private string _lastText;
        private float _lastFontSize = -1f;

#if UNITY_EDITOR
        private void OnEnable()
        {
            // Live edit-mode updates without touching TMP's TEXT_CHANGED_EVENT.
            UnityEditor.EditorApplication.update += EditorSync;
            ForceSync();
        }

        private void OnDisable()
        {
            UnityEditor.EditorApplication.update -= EditorSync;
        }

        private void EditorSync()
        {
            if (Application.isPlaying) return; // play mode is driven by LateUpdate
            if (this == null) { UnityEditor.EditorApplication.update -= EditorSync; return; }
            Sync();
        }
#endif

        private void LateUpdate()
        {
            Sync();
        }

        /// <summary>
        /// Force immediate sync. Call this if you need the text synced before LateUpdate.
        /// </summary>
        public void ForceSync()
        {
            _lastText = null;
            _lastFontSize = -1f;
            Sync();
        }

        private void Sync()
        {
            _target ??= GetComponent<TMP_Text>();
            if (_source == null || _target == null) return;

            // Only update if it changed to avoid unnecessary assignments (and edit-mode dirtying).
            // When the source auto-sizes, fontSize holds the fitted value, so the mirror follows it.
            if (_source.text != _lastText)
            {
                _lastText = _source.text;
                _target.text = _lastText;
            }

            if (_mirrorFontSize && !Mathf.Approximately(_source.fontSize, _lastFontSize))
            {
                _lastFontSize = _source.fontSize;
                _target.fontSize = _lastFontSize;
            }
        }
    }
}
