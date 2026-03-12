using Sorolla.LevelFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.DevTools
{
    public class SorollaDebugMenu : MonoBehaviour
    {
        private const int TapsToOpen = 4;

        [Header("References")]
        [SerializeField] private GameObject _menu;
        [SerializeField] private Button _openMenuButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_InputField _setLevelInput;
        [SerializeField] private Button _setLevelButton;

        private int _tapCount;
        private ILevelFlowManager _levelFlow;

        private void Start()
        {
            _menu.SetActive(false);

            _openMenuButton.onClick.AddListener(OnOpenMenuTapped);
            _closeButton.onClick.AddListener(CloseMenu);
            _setLevelButton.onClick.AddListener(OnSetLevelClicked);
        }

        private void OnDestroy()
        {
            _openMenuButton.onClick.RemoveListener(OnOpenMenuTapped);
            _closeButton.onClick.RemoveListener(CloseMenu);
            _setLevelButton.onClick.RemoveListener(OnSetLevelClicked);
        }

        private void OnOpenMenuTapped()
        {
            _tapCount++;
            if (_tapCount >= TapsToOpen)
            {
                _tapCount = 0;
                _menu.SetActive(true);
            }
        }

        private void CloseMenu()
        {
            _tapCount = 0;
            _menu.SetActive(false);
        }

        private void OnSetLevelClicked()
        {
            if (string.IsNullOrWhiteSpace(_setLevelInput.text))
                return;

            if (!int.TryParse(_setLevelInput.text, out int level))
                return;

            _levelFlow ??= ServiceLocator.Instance.Resolve<ILevelFlowManager>();
            if (_levelFlow == null)
            {
                Debug.LogWarning("[SorollaDebugMenu] ILevelFlowManager not found");
                return;
            }

            Debug.Log($"[SorollaDebugMenu] Starting level {level}");
            _levelFlow.StartLevel(level);
            CloseMenu();
        }
    }
}