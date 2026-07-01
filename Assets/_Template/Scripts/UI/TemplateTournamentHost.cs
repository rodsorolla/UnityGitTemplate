using UnityEngine;
using UnityEngine.UI;

namespace Template
{
    /// <summary>
    /// Hosts a scene-placed TournamentScreen (a plain MonoBehaviour that rebuilds in OnEnable).
    /// The MainMenu's Tournament button calls Show(); a Back button calls Hide().
    /// </summary>
    public class TemplateTournamentHost : MonoBehaviour
    {
        [SerializeField] private GameObject _content;
        [SerializeField] private Button _backButton;

        private void Awake()
        {
            if (_backButton != null) _backButton.onClick.AddListener(Hide);
            if (_content != null) _content.SetActive(false);
        }

        public void Show() { if (_content != null) _content.SetActive(true); }
        public void Hide() { if (_content != null) _content.SetActive(false); }
    }
}
