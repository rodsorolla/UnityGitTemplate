---
name: sorolla-core
description: Sorolla Core Unity framework guidance. Use when creating, editing, or reading files under `Assets/Sorolla Core/`, working with any Sorolla or Sorolla.* namespace, or using ServiceLocator, SorollaManager, LevelFlowManager, UIManager, UIPanel, SaveSystem, CurrencyService, Pool, HapticsService, AudioManager, FakeTouchCursor, or TutorialController in Unity C# projects.
---

# Sorolla Core Framework

Reusable Unity framework for mobile games. Provides DI, level flow, UI, persistence, currency, haptics, audio, tutorials, and pooling.

**For detailed module guides**: Read `Assets/Sorolla Core/README.md`

## Architecture

### Initialization Flow
```
GameInitializer (Bootstrap scene)
  → GameManager.InitializeAsync()
    → SaveSystem.Initialize()
    → AudioManager, TutorialController
    → _gameManagers[] (your managers)
  → Load Game scene (additive)
  → UIManager ready
  → Ready to play
```

### Service Infrastructure
```csharp
// Register (in Awake or Initialize)
ServiceLocator.Instance.Register<IMyService>(this);

// Resolve (required — throws if missing)
var levelFlow = ServiceLocator.Instance.Resolve<ILevelFlowManager>();

// TryResolve (optional — returns null if missing)
var haptics = ServiceLocator.Instance.TryResolve<IHapticsService>();
```

### SorollaManager Base Class
```csharp
public class MyManager : SorollaManager
{
    protected override void Initialize()
    {
        // Resolve services, subscribe to events, create pools
    }

    private void OnDestroy()
    {
        // ALWAYS unsubscribe events here
    }
}
```
Add to GameManager's `_gameManagers` array in Inspector.

## Module Quick Reference

### LevelFlow (`Sorolla.LevelFlow`)
```csharp
// Extend for your game
public class MyLevelManager : LevelFlowManager
{
    protected override int GetTotalLevelCount() => _levels.Length;
    protected override void OnLevelSetup(int levelIndex) { /* spawn, configure */ }
    protected override void OnLevelCleanup() { /* destroy, reset */ }
    // Optional: protected override WorldConfig[] GetWorldConfigs() => _worlds;
}

// Usage from anywhere
var lf = ServiceLocator.Instance.Resolve<ILevelFlowManager>();
lf.StartLevel(1);
lf.WinLevel();                           // Auto-saves, shows LevelComplete panel
lf.LoseLevel(LevelEndReason.TimeUp);     // Shows GameOver panel

// Events
lf.OnLevelSetupRequested += (actualIndex) => { };  // ACTUAL level (after modulo)
lf.OnLevelStarted += (progressiveIndex) => { };    // PROGRESSIVE level number
lf.OnLevelEnded += (reason) => { };
lf.OnLevelCleanupRequested += () => { };
```

### UI (`Sorolla.UI`)
```csharp
// UIPanel — override ShowAsync/HideAsync, call RaiseOpened/RaiseClosed
public class MyPanel : UIPanel
{
    public override Task ShowAsync(object args = null)
    {
        gameObject.SetActive(true);
        RaiseOpened();
        return Task.CompletedTask;
    }
    public override Task HideAsync()
    {
        gameObject.SetActive(false);
        RaiseClosed();
        return Task.CompletedTask;
    }
}

// Open/close panels
await UIManager.Instance.OpenPanelAsync(UIPanelId.Settings);
await UIManager.Instance.ClosePanelAsync(panel);

// Enum ranges: 0-99 = Sorolla Core, 100+ = game-specific
```

### SaveSystem (`Sorolla.PersistentData`)
```csharp
// Define data
[Serializable]
public class MyData : ISaveData
{
    public int Version => 1;
    public int score;
}

// Save/Load (static API)
SaveSystem.Save(data, "my_data");
var loaded = SaveSystem.Load<MyData>("my_data");  // Returns new instance if not found

// Extend GameDataServiceBase for automatic save management
public class GameDataService : GameDataServiceBase
{
    public override async Task LoadAllAsync() { _data = SaveSystem.Load<MyData>("game"); }
    public override void SaveAll() { SaveSystem.Save(_data, "game"); }
}
```

### Currency (`Sorolla.Currency`)
```csharp
var cs = ServiceLocator.Instance.Resolve<ICurrencyService>();
cs.Add(CurrencyIds.Coins, 100);
if (cs.TrySpend(CurrencyIds.Gems, 50)) { /* purchased */ }
int balance = cs.GetBalance(CurrencyIds.Coins);
cs.OnCurrencyChanged += (args) => { /* update UI */ };
// Pre-defined: CurrencyIds.Coins, .Gems, .Energy
```

### Pool (`Sorolla`)
```csharp
// Create
_pool = new Pool(prefab, "MyPool", parentTransform);
_pool.CreatePoolObjects(20);  // Pre-warm

// Get
var item = _pool.GetPooledComponent<MyComponent>();

// Return — just deactivate
item.gameObject.SetActive(false);

// Return all
_pool.ReturnToPoolEverything();
```

### Haptics (`Sorolla`)
```csharp
var h = ServiceLocator.Instance.TryResolve<IHapticsService>();
h?.PlayImpact(HapticsIntensity.Light);       // Light/Medium/Heavy
h?.PlaySelection();                           // UI tap
h?.PlayNotification(HapticsType.Success);     // Success/Warning/Error
```

### Audio (`Sorolla`)
```csharp
var audio = ServiceLocator.Instance.TryResolve<AudioManager>();
audio?.PlaySFX("Match");
audio?.PlayMusic("MainTheme");
audio?.StopMusic();
```

### Tutorial (`Sorolla.Tutorial`)
```csharp
// Extend TutorialStepBase for custom steps
// TutorialController manages progression and persistence
// Events: OnTutorialStepEntered, OnTutorialStepChanged, OnGateTriggered
```

### FakeTouchCursor (`Sorolla` — Editor-only)
Cursor overlay for recording App Store videos. Shows a hand sprite following the mouse with tap animation and optional particle FX. Lives in `Assets/Sorolla Core/Utils/FakeTouchCursor.cs`. Uses Input System (`Mouse.current`). Wrapped in `#if UNITY_EDITOR`.

## Common Tasks

### New SorollaManager
1. Create class extending `SorollaManager`
2. Override `Initialize()` — resolve services, subscribe events
3. Override `OnDestroy()` — unsubscribe events
4. Add GameObject to GameManager's `_gameManagers` array

### New UI Panel
1. Create prefab in `Assets/_Game/Prefabs/UI/`
2. Create component extending `UIPanel`, override `ShowAsync`/`HideAsync`
3. Add enum value to `UIPanelId` (use 100+)
4. Register in `UIRegistry` ScriptableObject
5. Open with `UIManager.Instance.OpenPanelAsync(panelId)`

### New Save Data
1. Create `[Serializable]` class implementing `ISaveData`
2. Use `SaveSystem.Save(data, filename)` / `SaveSystem.Load<T>(filename)`
3. Or extend `GameDataServiceBase` for auto-save on pause/quit

## Gotchas

- **Level index semantics**: `OnLevelSetupRequested` passes the **actual** level index (after modulo wrapping). `OnLevelStarted` passes the **progressive** level number. These differ once players loop past the last level.
- **Always unsubscribe** events in `OnDestroy()` to prevent null reference errors
- **iOS disk I/O**: Never call `Save()`/`PlayerPrefs.Save()` per-frame or per-item. Batch saves at level end. iOS stutters severely from hot-path disk writes that Android handles fine.
- **Initialization order**: SaveSystem → Core managers → Game managers → Scene load → UIManager. Don't resolve services before they're registered.
- **UI enum ranges**: 0-99 reserved for Sorolla Core. Game-specific panels/screens use 100+.
- **GameDataService registers in Awake()**, not Initialize() — it must be available before other managers init.
- **Pool return**: Deactivate the GameObject (`SetActive(false)`) to return it to the pool. No explicit Return() call needed.

## Namespaces
| Namespace | Module |
|---|---|
| `Sorolla` | Core infra, Pool, Haptics, Audio |
| `Sorolla.LevelFlow` | Level progression |
| `Sorolla.PersistentData` | Save/load |
| `Sorolla.Currency` | Currency system |
| `Sorolla.UI` | UI core |
| `Sorolla.UI.Transitions` | DOTween transitions |
| `Sorolla.UI.Dialogs` | Toast, Confirm, Alert |
| `Sorolla.UI.Celebrations` | Unlock/celebration panels |
| `Sorolla.UI.Effects` | Floating text |
| `Sorolla.UI.Config` | Config-driven panels |
| `Sorolla.Tutorial` | Tutorial system |

## Maintenance

When modifying Sorolla Core (adding/removing modules, changing APIs, renaming classes, altering initialization flow), **update `Assets/Sorolla Core/README.md`** to reflect the changes. The README is the single source of truth for detailed module documentation — this skill references it via progressive disclosure rather than duplicating it.
