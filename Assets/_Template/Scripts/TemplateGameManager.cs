using System;
using System.Collections.Generic;
using System.Linq;
using Sorolla;
using Sorolla.Cosmetics;
using Sorolla.PersistentData;
using Sorolla.UI;
using UnityEngine;

namespace Template
{
    /// <summary>
    /// Agnostic template GameManager — the single place a new game starts customizing
    /// service registration. Extends the Core <see cref="GameManager"/> with the two
    /// wirings the base cannot do generically:
    ///  - registers the non-MonoBehaviour <see cref="ISkinService"/> (built from a SkinCatalog),
    ///  - registers the <see cref="IIconResolver"/> under its interface.
    /// Every MonoBehaviour service is wired through the base _gameManagers[] array in the scene.
    /// </summary>
    public class TemplateGameManager : GameManager
    {
        [Header("Template Services")]
        [SerializeField] private TemplateIconResolver _iconResolver;
        [SerializeField] private SkinCatalog _skinCatalog;

        private const string CosmeticsSaveFile = "cosmetics";

        protected override void Init()
        {
            base.Init();
            RegisterIconResolver();
            RegisterSkinService();
        }

        private void RegisterIconResolver()
        {
            if (_iconResolver != null)
                ServiceLocator.Instance.Register<IIconResolver>(_iconResolver);
        }

        private void RegisterSkinService()
        {
            var allIds = _skinCatalog != null
                ? _skinCatalog.Skins.Select(s => s.Id)
                : Enumerable.Empty<string>();
            var defaultIds = _skinCatalog != null
                ? _skinCatalog.Skins.Where(s => s.UnlockType == SkinUnlockType.Default).Select(s => s.Id)
                : Enumerable.Empty<string>();

            var skins = new SkinService(allIds, defaultIds, LoadCosmeticString, SaveCosmeticString);
            ServiceLocator.Instance.Register<ISkinService>(skins);
        }

        private static string LoadCosmeticString(string key, string defaultValue)
        {
            var kv = SaveSystem.Load<KVSaveData>(CosmeticsSaveFile);
            return kv.Strings.TryGetValue(key, out var value) ? value : defaultValue;
        }

        private static void SaveCosmeticString(string key, string value)
        {
            var kv = SaveSystem.Load<KVSaveData>(CosmeticsSaveFile);
            kv.Strings[key] = value;
            SaveSystem.Save(kv, CosmeticsSaveFile);
        }
    }
}
