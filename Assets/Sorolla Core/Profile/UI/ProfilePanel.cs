using Cysharp.Threading.Tasks;
using Sorolla;
using Sorolla.Profile;
using Sorolla.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Profile.UI
{
    /// <summary>
    /// Profile panel: edit the display name and pick an avatar + country flag from generated grids.
    /// Each grid is built once by instantiating its button prefab per catalog entry into a
    /// scroll-view content; a tap calls SetAvatar(id) / SetFlag(code). The AVATAR / FLAG tab
    /// buttons toggle which section is visible. Name/avatar/flag persist immediately via
    /// IPlayerProfile. Visual layout/prefab is authored by the user; this script binds + populates.
    /// </summary>
    public class ProfilePanel : UIPanel
    {
        [Header("Current")]
        [Tooltip("The avatar badge showing the player's selected avatar.")]
        [SerializeField] private Image _currentAvatar;
        [Tooltip("The flag badge showing the player's selected country.")]
        [SerializeField] private Image _currentFlag;

        [Header("Name editing")]
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private TMP_Text _nameError;
        [Tooltip("Green SAVE button: validates + applies the typed name, then closes on success.")]
        [SerializeField] private Button _saveButton;

        [Header("Avatar grid")]
        [Tooltip("Scroll View Avatars content (with a Grid Layout Group) the avatar buttons go into.")]
        [SerializeField] private RectTransform _avatarGridContent;
        [Tooltip("AvatarButton prefab. Instantiated once per catalog avatar; its child Image named \"Avatar\" gets the sprite.")]
        [SerializeField] private Button _avatarButtonPrefab;

        [Header("Flag grid")]
        [Tooltip("Scroll View Flags content (with a Grid Layout Group) the flag buttons go into.")]
        [SerializeField] private RectTransform _flagGridContent;
        [Tooltip("FlagButton prefab (Button + root Image). Instantiated once per catalog flag.")]
        [SerializeField] private Button _flagButtonPrefab;

        [Header("Sections (Avatar / Flag tabs)")]
        [Tooltip("Root of the avatars section (e.g. Scroll View Avatars).")]
        [SerializeField] private GameObject _avatarSection;
        [Tooltip("Root of the flags section (e.g. Scroll View Flags).")]
        [SerializeField] private GameObject _flagSection;
        [Tooltip("AVATAR tab button: shows the avatars section.")]
        [SerializeField] private Button _avatarTabButton;
        [Tooltip("FLAG tab button: shows the flags section.")]
        [SerializeField] private Button _flagTabButton;

        [Header("Close")]
        [SerializeField] private Button _closeButton;

        [Header("Catalog (assign the same asset used by PlayerProfileService)")]
        [SerializeField] private ProfileCatalog _catalog;

        private IPlayerProfile _profile;    
        private bool _avatarGridBuilt;
        private bool _flagGridBuilt;

        public override UniTask ShowAsync(object args = null)
        {
            _profile ??= ServiceLocator.Instance.TryResolve<IPlayerProfile>();

            BuildAvatarGrid();
            BuildFlagGrid();
            Bind();
            RefreshCurrent();
            ShowAvatars(); // default to the avatar tab

            return base.ShowAsync(args);
        }

        private void Bind()
        {
            if (_nameInput != null && _profile != null)
                _nameInput.text = _profile.DisplayName;
            if (_nameError != null)
                _nameError.text = string.Empty;

            if (_saveButton != null)
            {
                _saveButton.onClick.RemoveListener(OnSaveName);
                _saveButton.onClick.AddListener(OnSaveName);
            }

            if (_avatarTabButton != null)
            {
                _avatarTabButton.onClick.RemoveListener(ShowAvatars);
                _avatarTabButton.onClick.AddListener(ShowAvatars);
            }

            if (_flagTabButton != null)
            {
                _flagTabButton.onClick.RemoveListener(ShowFlags);
                _flagTabButton.onClick.AddListener(ShowFlags);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnClose);
                _closeButton.onClick.AddListener(OnClose);
            }

            if (_profile != null)
            {
                _profile.OnProfileChanged -= RefreshCurrent;
                _profile.OnProfileChanged += RefreshCurrent;
            }
        }

        /// <summary>
        /// Instantiates one AvatarButton per catalog avatar into the grid content. The avatar
        /// art lives on a child Image named "Avatar" (the root Image is just the button hit-area).
        /// Idempotent — guarded by _avatarGridBuilt so re-showing never duplicates the grid.
        /// </summary>
        private void BuildAvatarGrid()
        {
            if (_avatarGridBuilt) return;
            if (_catalog == null || _avatarButtonPrefab == null || _avatarGridContent == null)
            {
                Debug.LogWarning("[ProfilePanel] Avatar grid not built: _catalog, _avatarButtonPrefab or _avatarGridContent unassigned.");
                return;
            }
            _avatarGridBuilt = true;

            int colorIndex = 0;
            foreach (var entry in _catalog.avatars)
            {
                if (entry == null) continue;
                string id = entry.id; // capture for the closure

                var button = Instantiate(_avatarButtonPrefab, _avatarGridContent);
                button.name = $"Avatar_{id}";

                var image = FindImageByName(button.transform, "Avatar");
                if (image != null) image.sprite = entry.sprite;

                // Give each tile a different soft background color, cycling the palette.
                var avatarButton = button.GetComponent<AvatarButton>();
                if (avatarButton != null) avatarButton.ApplyColor(colorIndex);
                colorIndex++;

                button.onClick.AddListener(() => OnAvatarSelected(id));
            }
        }

        /// <summary>
        /// Instantiates one FlagButton per catalog flag into the grid content. Idempotent —
        /// guarded by _flagGridBuilt so re-showing the panel never duplicates the grid.
        /// </summary>
        private void BuildFlagGrid()
        {
            if (_flagGridBuilt) return;
            if (_catalog == null || _flagButtonPrefab == null || _flagGridContent == null)
            {
                Debug.LogWarning("[ProfilePanel] Flag grid not built: _catalog, _flagButtonPrefab or _flagGridContent unassigned.");
                return;
            }
            _flagGridBuilt = true;

            foreach (var entry in _catalog.flags)
            {
                if (entry == null) continue;
                string code = entry.countryCode; // capture for the closure

                var button = Instantiate(_flagButtonPrefab, _flagGridContent);
                button.name = $"Flag_{code}";

                var image = button.GetComponent<Image>();
                if (image != null) image.sprite = entry.sprite;

                button.onClick.AddListener(() => OnFlagSelected(code));
            }
        }

        private void RefreshCurrent()
        {
            if (_profile == null || _catalog == null) return;
            if (_currentAvatar != null) _currentAvatar.sprite = _catalog.GetAvatarSprite(_profile.AvatarId);
            if (_currentFlag != null) _currentFlag.sprite = _catalog.GetFlagSprite(_profile.CountryCode);
        }

        // SAVE validates + applies the typed name. On success closes the panel; otherwise
        // surfaces the validation error. Avatar/flag selection applies immediately on tap.
        private void OnSaveName()
        {
            if (_profile == null || _nameInput == null) return;
            var result = _profile.SetName(_nameInput.text);
            if (_nameError != null)
                _nameError.text = result == NameValidationResult.Ok ? string.Empty : ErrorText(result);
            if (result == NameValidationResult.Ok) OnClose();
        }

        private void OnAvatarSelected(string avatarId) => _profile?.SetAvatar(avatarId);
        private void OnFlagSelected(string countryCode) => _profile?.SetFlag(countryCode);

        private void ShowAvatars() => SetSection(showAvatars: true);
        private void ShowFlags() => SetSection(showAvatars: false);

        private void SetSection(bool showAvatars)
        {
            if (_avatarSection != null) _avatarSection.SetActive(showAvatars);
            if (_flagSection != null) _flagSection.SetActive(!showAvatars);
        }

        // Finds the descendant Image whose GameObject is named `name` (inactive children
        // included). The AvatarButton's art lives on a child called "Avatar".
        private static Image FindImageByName(Transform root, string name)
        {
            foreach (var img in root.GetComponentsInChildren<Image>(true))
                if (img.gameObject.name == name) return img;
            return null;
        }

        private void OnClose()
        {
            var uiManager = UIManager.Instance;
            if (uiManager != null) uiManager.ClosePanelAsync(this).Forget();
            else gameObject.SetActive(false);
        }

        public override bool HandleBack()
        {
            OnClose();
            return true;
        }

        private static string ErrorText(NameValidationResult r)
        {
            switch (r)
            {
                case NameValidationResult.Empty: return "Enter a name.";
                case NameValidationResult.TooShort: return "Too short (min 3).";
                case NameValidationResult.TooLong: return "Too long (max 12).";
                case NameValidationResult.Blocked: return "Name not allowed.";
                case NameValidationResult.Invalid: return "Invalid characters.";
                default: return string.Empty;
            }
        }

        private void OnDestroy()
        {
            if (_profile != null) _profile.OnProfileChanged -= RefreshCurrent;
        }
    }
}
