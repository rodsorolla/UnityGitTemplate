using Sorolla;
using Sorolla.Profile;
using Sorolla.Tournaments;
using Sorolla.UI;
using UnityEngine;

namespace Template
{
    /// <summary>
    /// Minimal app-shell bootstrap for the agnostic template: initialises and registers the Core
    /// services the Profile/Tournament UI resolves via <see cref="ServiceLocator"/>. Assign the
    /// service components (placed in the same scene) + the icon resolver in the inspector.
    ///
    /// Order matters: PlayerProfileService registers first because TournamentService.Initialize
    /// resolves IPlayerProfile. Each SorollaManager.Init() is idempotent.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TemplateBootstrap : MonoBehaviour
    {
        [SerializeField] private PlayerProfileService _playerProfile;
        [SerializeField] private TournamentService _tournament;
        [SerializeField] private TemplateIconResolver _iconResolver;

        private void Awake()
        {
            if (_iconResolver != null)
                ServiceLocator.Instance.Register<IIconResolver>(_iconResolver);

            if (_playerProfile != null)
            {
                _playerProfile.Init();
                ServiceLocator.Instance.Register<IPlayerProfile>(_playerProfile);
            }

            if (_tournament != null)
            {
                _tournament.Init();                       // resolves IPlayerProfile (registered above)
                ServiceLocator.Instance.Register<ITournamentService>(_tournament);
            }
        }
    }
}
