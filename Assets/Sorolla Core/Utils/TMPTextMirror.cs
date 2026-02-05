using TMPro;
using UnityEngine;

namespace Sorolla.Utils
{
    /// <summary>
    /// Mirrors text from a source TextMeshProUGUI to this component's TextMeshProUGUI.
    /// Attach to a GameObject with TextMeshProUGUI and assign the source to copy from.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMPTextMirror : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _source;

        private TextMeshProUGUI _target;
        private string _lastText;

        private void Awake()
        {
            _target = GetComponent<TextMeshProUGUI>();
        }

        private void LateUpdate()
        {
            if (_source == null || _target == null) return;

            // Only update if text changed to avoid unnecessary assignments
            if (_source.text != _lastText)
            {
                _lastText = _source.text;
                _target.text = _lastText;
            }
        }

        /// <summary>
        /// Force immediate sync. Call this if you need the text synced before LateUpdate.
        /// </summary>
        public void ForceSync()
        {
            if (_source == null || _target == null) return;
            _lastText = _source.text;
            _target.text = _lastText;
        }
    }
}
