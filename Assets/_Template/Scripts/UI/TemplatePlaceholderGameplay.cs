using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sorolla;
using Sorolla.LevelFlow;

namespace Template
{
    /// <summary>
    /// Agnostic stand-in for gameplay. Shows a WIN/LOSE overlay while a level is Playing so the
    /// loop is clickable. A real game DELETES this and instead builds content on
    /// LevelFlowManager.OnLevelSetupRequested and calls WinLevel/LoseLevel from its own gameplay.
    /// </summary>
    public class TemplatePlaceholderGameplay : MonoBehaviour
    {
        [SerializeField] private GameObject _overlay;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private Button _winButton;
        [SerializeField] private Button _loseButton;

        private ILevelFlowManager _flow;

        private void Start()
        {
            _flow = ServiceLocator.Instance.TryResolve<ILevelFlowManager>();
            if (_flow != null)
            {
                _flow.OnLevelStarted += HandleLevelStarted;
                _flow.OnLevelEnded += HandleLevelEnded;
            }
            if (_winButton != null) _winButton.onClick.AddListener(() => _flow?.WinLevel(LevelEndReason.AllGoalsComplete));
            if (_loseButton != null) _loseButton.onClick.AddListener(() => _flow?.LoseLevel(LevelEndReason.TimeUp));
            if (_overlay != null) _overlay.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_flow != null)
            {
                _flow.OnLevelStarted -= HandleLevelStarted;
                _flow.OnLevelEnded -= HandleLevelEnded;
            }
        }

        private void HandleLevelStarted(int levelIndex)
        {
            if (_levelLabel != null) _levelLabel.text = "Level " + levelIndex;
            if (_overlay != null) _overlay.SetActive(true);
        }

        private void HandleLevelEnded(LevelEndReason reason)
        {
            if (_overlay != null) _overlay.SetActive(false);
        }
    }
}
