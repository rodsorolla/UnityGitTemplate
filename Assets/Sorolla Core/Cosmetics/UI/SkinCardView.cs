using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sorolla.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Cosmetics
{
    /// <summary>
    /// One skin card. Renders three states from the service:
    /// locked (show description), unlocked (show Use button), selected (show overlay).
    /// </summary>
    public class SkinCardView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _lockedDescription;
        [SerializeField] private Button _useButton;
        [SerializeField] private TMP_Text _useButtonLabel;
        [SerializeField] private GameObject _selectedOverlay;
        [Header("Tier unlock")]
        [SerializeField] private Image _trophyIcon;
        [Tooltip("Cup sprite per tournament tier, indexed by tier (0 Bronze ... 4 Ruby). Used for ReachTier skins.")]
        [SerializeField] private Sprite[] _tierIcons;

        [Header("Select bounce")]
        [Tooltip("Scale punch strength applied to the card when this skin is selected via USE.")]
        [SerializeField] private float _bounceStrength = 0.25f;
        [SerializeField] private float _bounceDuration = 0.35f;

        private const string UseLabel = "USE";
        private const string BuyLabel = "BUY";

        private SkinDefinition _definition;
        private ISkinService _service;
        private Tween _bounceTween;

        public void Initialize(SkinDefinition definition, ISkinService service)
        {
            _definition = definition;
            _service = service;

            _icon.sprite = definition.Icon;
            _nameText.text = definition.DisplayName;
            _lockedDescription.text = definition.LockedDescription;

            _useButton.onClick.RemoveListener(OnUseClicked);
            _useButton.onClick.AddListener(OnUseClicked);

            Refresh();
        }

        public void Refresh()
        {
            if (_definition == null || _service == null) return;

            bool unlocked = _service.IsUnlocked(_definition.Id);
            bool selected = unlocked && _service.SelectedSkinId == _definition.Id;
            bool buyable = !unlocked && _definition.UnlockType == SkinUnlockType.IAP;

            _selectedOverlay.SetActive(selected);

            // Button shows for an owned-but-unselected skin (USE) or an IAP-locked skin (BUY).
            _useButton.gameObject.SetActive((unlocked && !selected) || buyable);
            if (_useButtonLabel != null) _useButtonLabel.text = buyable ? BuyLabel : UseLabel;

            // Locked description shows only for non-IAP locked skins (BUY replaces it).
            _lockedDescription.gameObject.SetActive(!unlocked && !buyable);

            RefreshTrophy(unlocked);
        }

        // Tier-unlock skins show the league cup they require, while still locked.
        private void RefreshTrophy(bool unlocked)
        {
            if (_trophyIcon == null) return;

            bool showTrophy = !unlocked && _definition.UnlockType == SkinUnlockType.ReachTier;
            _trophyIcon.gameObject.SetActive(showTrophy);

            if (showTrophy && _tierIcons != null
                && _definition.UnlockValue >= 0 && _definition.UnlockValue < _tierIcons.Length)
            {
                _trophyIcon.sprite = _tierIcons[_definition.UnlockValue];
            }
        }

        private void OnUseClicked()
        {
            // IAP-locked skin: open the bundle offer panel that grants it. Otherwise select it.
            if (!_service.IsUnlocked(_definition.Id) && _definition.UnlockType == SkinUnlockType.IAP)
            {
                UIManager.Instance?.OpenPanelAsync(_definition.PurchasePanelId).Forget();
                return;
            }
            _service.Select(_definition.Id);
            Bounce();
        }

        // Quick scale punch on the card root to confirm the skin was equipped.
        private void Bounce()
        {
            _bounceTween?.Kill(complete: true);
            _bounceTween = transform.DOPunchScale(Vector3.one * _bounceStrength, _bounceDuration)
                .SetUpdate(true)   // menu may run at timeScale 0
                .SetLink(gameObject);
        }

        private void OnDestroy()
        {
            _bounceTween?.Kill();
            if (_useButton != null) _useButton.onClick.RemoveListener(OnUseClicked);
        }
    }
}
