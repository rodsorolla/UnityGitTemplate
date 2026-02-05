# Sorolla Persistent Data System

A reusable, project-agnostic save/load system for Unity with versioning, migration, backup, and editor visualization.

## Features

- **Generic Save/Load**: Works with any serializable data class
- **Version Migrations**: Automatically upgrade old saves to new formats
- **Backup System**: Timestamped backups before overwriting
- **Multiple Save Slots**: Support for multiple game profiles
- **Async Operations**: Non-blocking save/load for large files
- **Editor Tools**: View and edit saves during development
- **Build Safety**: Warns about editor-modified saves before building

## Requirements

- Unity 2021.3+
- Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`)

## Quick Start

### 1. Define Your Data Class

```csharp
using Sorolla.PersistentData;

[System.Serializable]
public class PlayerData : ISaveData
{
    public int Version => 1;  // Increment when making breaking changes

    public int coins;
    public int level = 1;
    public List<string> inventory = new();
}
```

### 2. Save Data

```csharp
var playerData = new PlayerData { coins = 100 };
SaveSystem.Save(playerData, "player");

// Or async
await SaveSystem.SaveAsync(playerData, "player");
```

### 3. Load Data

```csharp
var playerData = SaveSystem.Load<PlayerData>("player");

// Or async
var playerData = await SaveSystem.LoadAsync<PlayerData>("player");
```

## Multiple Save Slots

```csharp
// Save to slot 1
SaveSystem.Save(playerData, "player", slot: 1);

// Load from slot 2
var data = SaveSystem.Load<PlayerData>("player", slot: 2);
```

## Default Values with ScriptableObjects

Create a config asset that provides defaults:

```csharp
[CreateAssetMenu(menuName = "Game/Player Config")]
public class PlayerConfig : ScriptableObject, IDefaultsProvider<PlayerData>
{
    public int startingCoins = 100;

    public PlayerData CreateDefault() => new PlayerData
    {
        coins = startingCoins
    };
}
```

Then use it when loading:

```csharp
[SerializeField] private PlayerConfig _config;

var data = SaveSystem.Load("player", 0, _config);
```

## Version Migrations

Register migrations early (before loading):

```csharp
// Migrate from v1 to v2
SaveSystem.Migrations.Register<PlayerData>(1, 2, oldJson =>
{
    var obj = JObject.Parse(oldJson);

    // Example: rename 'gold' to 'coins'
    if (obj["gold"] != null)
    {
        obj["coins"] = obj["gold"];
        obj.Remove("gold");
    }

    return obj.ToString();
});
```

Migrations are chained automatically: v1 → v2 → v3.

## Backup System

```csharp
// Configure max backups (default: 3)
SaveSystem.Backups.MaxBackups = 5;

// List backups for a file
var backups = SaveSystem.Backups.GetBackups("player");

// Restore from latest backup
SaveSystem.Backups.RestoreLatestBackup("player", SaveSystem.GetFilePath("player"));

// Restore from specific backup
SaveSystem.Backups.RestoreFromBackup(backupPath, targetPath);
```

## Events

```csharp
SaveSystem.Events.OnBeforeSave += (file, slot) => { };
SaveSystem.Events.OnAfterSave += (file, slot) => { };
SaveSystem.Events.OnBeforeLoad += (file, slot) => { };
SaveSystem.Events.OnAfterLoad += (file, slot) => { };
SaveSystem.Events.OnSaveCorrupted += (file, slot, ex) => { };
SaveSystem.Events.OnMigrationApplied += (file, slot, from, to) => { };
```

## Editor Tools

### Save Data Editor (Tools > Sorolla > Save Data Editor)

- View all save files by slot
- Edit save data directly (marked as [EDITOR MODIFIED])
- Delete saves
- Clean editor markers before building

### Build Warning

The system warns if editor-modified saves exist when building.

## File Structure

Saves are stored in `Application.persistentDataPath`:

```
saves/
├── default/           # Slot 0
│   └── player.json
├── slot1/
│   └── player.json
├── slot2/
│   └── player.json
└── backups/
    ├── player_20240115_143022.json
    └── player_20240115_120000.json
```

## API Reference

### SaveSystem (Static)

| Method | Description |
|--------|-------------|
| `Save<T>(data, fileName, slot, createBackup)` | Save data synchronously |
| `SaveAsync<T>(...)` | Save data asynchronously |
| `Load<T>(fileName, slot)` | Load data (returns new T() if missing) |
| `Load<T>(fileName, slot, defaultValue)` | Load with custom default |
| `LoadAsync<T>(...)` | Load data asynchronously |
| `Exists(fileName, slot)` | Check if save exists |
| `Delete(fileName, slot, deleteBackups)` | Delete a save |
| `GetFilePath(fileName, slot)` | Get full path to save |
| `GetAllSaveFiles(slot)` | List all saves in slot |

### Properties

| Property | Description |
|----------|-------------|
| `Events` | Event handlers |
| `Migrations` | Migration pipeline |
| `Backups` | Backup manager |
| `Storage` | Storage provider |

## Custom Storage Provider

Implement `IStorageProvider` for cloud saves, encryption, etc.:

```csharp
public class CloudStorage : IStorageProvider
{
    public SaveResult Save(string json, string fileName, int slot = 0) { ... }
    public Task<SaveResult> SaveAsync(...) { ... }
    public string Load(string fileName, int slot = 0) { ... }
    // ... other methods
}

// Use custom storage
SaveSystem.Initialize(new CloudStorage());
```

## Performance Notes

- Use async methods for large saves
- Backups are created synchronously (consider disabling for frequent saves)
- JSON is pretty-printed in Editor, compact in builds
