using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using Sorolla.UI;

namespace Template
{
    /// <summary>
    /// Minimal end-of-level panel used for both LevelComplete and GameOver (different prefabs,
    /// different _title). Continue closes it; UIManager.ClosePanelAsync -> HideAsync -> OnClosed,
    /// which LevelFlowManager listens for to return to the menu.
    /// </summary>
    public class TemplateEndPanel : UIPanel
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private string _title = "Level Complete!";
        [SerializeField] private Button _continueButton;

        private void Awake()
        {
            if (_titleText != null) _titleText.text = _title;
            if (_continueButton != null) _continueButton.onClick.AddListener(Close);
        }

        private void Close()
        {
            UIManager.Instance.ClosePanelAsync(this).Forget();
        }
    }
}
