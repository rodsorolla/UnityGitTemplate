using Cysharp.Threading.Tasks;
using UnityEngine;
using Sorolla;

namespace Sorolla.PersistentData.Samples
{
    /// <summary>
    /// Example implementation of GameDataServiceBase.
    /// Copy and modify this for your game.
    ///
    /// Setup:
    /// 1. Create a GameObject in your boot scene
    /// 2. Attach this component
    /// 3. It auto-registers with ServiceLocator
    ///
    /// Access from anywhere:
    ///   var gameData = ServiceLocator.Instance.Resolve&lt;IGameDataService&gt;() as ExampleGameDataService;
    ///   gameData.Player.coins += 100;
    /// </summary>
    public class ExampleGameDataService : GameDataServiceBase
    {
        // Your game data - exposed as properties
        public ExamplePlayerData Player { get; private set; }
        public ExampleSettingsData Settings { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            // Register with ServiceLocator
            ServiceLocator.Instance.Register<IGameDataService>(this);
        }

        public override async UniTask LoadAllAsync()
        {
            // Register any migrations before loading
            RegisterMigrations();

            // Load all data
            Player = await SaveSystem.LoadAsync<ExamplePlayerData>("player");
            Settings = await SaveSystem.LoadAsync<ExampleSettingsData>("settings");

            Debug.Log($"[GameData] Loaded - Coins: {Player.coins}, Level: {Player.level}");

            await base.LoadAllAsync();
        }

        public override async UniTask SaveAllAsync()
        {
            await SaveSystem.SaveAsync(Player, "player");
            await SaveSystem.SaveAsync(Settings, "settings");
        }

        public override void SaveAll()
        {
            // Synchronous save for OnApplicationQuit
            SaveSystem.Save(Player, "player");
            SaveSystem.Save(Settings, "settings");
        }

        public override void DeleteAll()
        {
            SaveSystem.Delete("player", deleteBackups: true);
            SaveSystem.Delete("settings", deleteBackups: true);

            // Reset to defaults
            Player = new ExamplePlayerData();
            Settings = new ExampleSettingsData();

            base.DeleteAll();
        }

        private void RegisterMigrations()
        {
            // Example: Migration from v1 to v2
            // SaveSystem.Migrations.Register<ExamplePlayerData>(1, 2, json => { ... });
        }
    }

    /// <summary>
    /// Example settings data. Separate from player progress.
    /// </summary>
    [System.Serializable]
    public class ExampleSettingsData : ISaveData
    {
        public int Version => 1;

        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool vibrationEnabled = true;
    }
}
