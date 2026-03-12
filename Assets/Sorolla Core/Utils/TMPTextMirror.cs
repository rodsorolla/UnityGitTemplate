using TMPro;
using UnityEngine;

namespace Sorolla.Utils
{
    /// <summary>
    /// Mirrors text from a source TMP_Text to this component's TMP_Text.
    /// Works with both TextMeshProUGUI (Canvas) and TextMeshPro (3D world).
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TMPTextMirror : MonoBehaviour
    {
        [SerializeField] private TMP_Text _source;

        private TMP_Text _target;
        private string _lastText;

        private void Awake()
        {
            _target = GetComponent<TMP_Text>();
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
