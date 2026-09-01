---
name: sorolla-core
description: Sorolla Core Unity framework guidance. Use when creating, editing, or reading files under `Assets/Sorolla Core/`, working with any Sorolla or Sorolla.* namespace, or using ServiceLocator, SorollaManager, LevelFlowManager, UIManager, UIPanel, SaveSystem, CurrencyService, Pool, HapticsService, AudioManager, FakeTouchCursor, TutorialController, SorollaTimer, SceneLoader, or SafeAreaHandler in Unity C# projects.
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
    → AudioManager, TutorialController, LevelFlowManager
    → _gameManagers[]:
        - IAsyncInitializable.InitializeAsync()  (awaited, exclusive of Init)
        - SorollaManager.Init()                  (sync path)
  → Load Game scene (additive)
  → GameManager.HandleSceneLoaded():
        - InitializeSceneServices() — auto-finds and Init()s any
          uninitialized SorollaManager components in the loaded scene
        - PlayMusic(_sceneLoadMusicKey) if set
  → UIManager ready
  → Ready to play
```

Implement `IAsyncInitializable` (namespace `Sorolla`) on a `_gameManagers` entry
when its boot needs to await something (network fetch, file I/O, addressables).
Async path is exclusive — the manager's sync `Init()` is **not** also called.

### Service Infrastructure
```csharp
// Register (in Awake or Initialize)
ServiceLocator.Instance.Register<IMyService>(this);

// Resolve (required — throws if missing)
var levelFlow = ServiceLocator.Instance.Resolve<ILevelFlowManager>();

// TryResolve (optional — returns null if missing)
var haptics = ServiceLocator.Instance.TryResolve<IHapticsService>();

// Debug (Editor/Dev builds only)
ServiceLocator.Instance.DEBUG_LogAll();
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
    public override UniTask ShowAsync(object args = null)
    {
        gameObject.SetActive(true);
        RaiseOpened();
        return UniTask.CompletedTask;
    }
    public override UniTask HideAsync()
    {
        gameObject.SetActive(false);
        RaiseClosed();
        return UniTask.CompletedTask;
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
    public int Version => 1;  // Reserved for future migration support
    public int score;
}

// Save/Load (static API). LocalFileStorage uses File.Replace for crash-safe writes.
SaveSystem.Save(data, "my_data");                     // Sync, creates a backup
await SaveSystem.SaveAsync(data, "my_data");          // Off main thread
var loaded = SaveSystem.Load<MyData>("my_data");      // new T() if missing
var loaded2 = SaveSystem.Load("my_data", 0, defaults); // Custom default
SaveSystem.DeleteAllData();         // Wipe all slots
SaveSystem.DeleteAllData(slot: 1);  // Wipe specific slot

// Persistence pattern for services that mutate often: flag _isDirty, flush
// in OnApplicationPause/OnApplicationQuit. Persist immediately for rare,
// load-bearing events (e.g. PowerUpService.UnlockPowerUp) so a scene change
// before the next dirty flush doesn't lose them.
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

// Looping SFX — pair Play/Stop, or call StopAllLoopingSFX on scene teardown.
var loop = audio?.PlayLoopingSFX("Engine");
audio?.StopLoopingSFX(loop);
audio?.StopAllLoopingSFX();
```
Settings flush to SaveSystem on pause/quit (deferred, always). Slider drags
don't hit disk per event — iOS-friendly. Call `SaveSettings()` to flush early.

### Tutorial (`Sorolla.Tutorial` / `Sorolla.Tutorial.Highlight`)
```csharp
// Extend TutorialStepBase for custom steps. TutorialController manages
// progression and persists completed levels via TutorialSaveData ("tutorial" file).
// Events: OnTutorialStepEntered, OnTutorialStepChanged, OnGateTriggered

// Highlight: drop TutorialHighlightTarget on a UI button or world sprite
// (adapter is auto-picked: UI vs Sprite). Use the HighlightTutorialStep SO
// + bundled HighlightTutorialStepPanel prefab to point at it. Run
// "Tools > Sorolla > Tutorial > Setup Highlight System" once per scene.
```

### LiveConfig (`Sorolla.LiveConfig`)
```csharp
// LiveConfigSettings (Resources SO) + LiveConfigFetcher (UWR fetch) +
// StreamingAssetsReader (baked fallback). Wire into a manager that
// implements IAsyncInitializable for boot-time fetch + persistent cache
// + baked-fallback flow. Game-side data shapes live in _Game/.
// See Assets/Sorolla Core/LiveConfig/README.md.
```

### SorollaTimer (`Sorolla`)
```csharp
// One-shot timer
var timer = SorollaTimer.StartTimer(3f, () => Debug.Log("Done!"));

// Looping timer
var loop = SorollaTimer.StartTimer(1f, () => Tick(), loop: true);

// Countdown with remaining-time tick
var cd = SorollaTimer.StartCountdown(10f, remaining => UpdateUI(remaining), () => TimeUp());

// Control: Pause(), Resume(), Cancel(), Restart()
// Properties: Elapsed, Remaining, Progress (0-1), IsRunning, IsComplete
// Unscaled time: SorollaTimer.StartTimer(1f, cb, useUnscaledTime: true);
// Cancel all: SorollaTimer.CancelAll();
```

### SceneLoader (`Sorolla`)
```csharp
await SceneLoader.LoadSceneAsync("GameScene");
await SceneLoader.LoadSceneAdditiveAsync("UI_Scene");
await SceneLoader.UnloadSceneAsync("UI_Scene");
await SceneLoader.ReloadCurrentSceneAsync();
// All accept optional Action<float> onProgress callback
```

### SafeAreaHandler (`Sorolla.UI`)
Component for RectTransform. Adjusts anchors to `Screen.safeArea`. Per-edge toggles: `_applyTop`, `_applyBottom`, `_applyLeft`, `_applyRight`. Auto-updates on orientation change.

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
3. For services that mutate frequently, flag `_isDirty` and flush in
   `OnApplicationPause`/`OnApplicationQuit` (see `AudioManager`, `PowerUpService`)

## Gotchas

- **Level index semantics**: `OnLevelSetupRequested` passes the **actual** level index (after modulo wrapping). `OnLevelStarted` passes the **progressive** level number. These differ once players loop past the last level.
- **Always unsubscribe** events in `OnDestroy()` to prevent null reference errors
- **iOS disk I/O**: Never call `Save()`/`PlayerPrefs.Save()` per-frame or per-item. Batch saves at level end or use the dirty-flag pattern. iOS stutters severely from hot-path disk writes that Android handles fine.
- **Initialization order**: SaveSystem → Core managers → Game managers (`IAsyncInitializable` awaited in order, others use sync `Init()`) → Scene load → `InitializeSceneServices` (auto-discovers in-scene SorollaManagers). Don't resolve services before they're registered.
- **UI enum ranges**: 0-99 reserved for Sorolla Core. Game-specific panels/screens use 100+.
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
| `Sorolla.Tutorial.Highlight` | Adapter-based highlight panels |
| `Sorolla.LiveConfig` | Server-pushed JSON tuning with fallback chain |
| `Sorolla.GoogleSheets` | `[SheetColumn]` attribute (runtime only) |

## Maintenance

When modifying Sorolla Core (adding/removing modules, changing APIs, renaming classes, altering initialization flow), **update `Assets/Sorolla Core/README.md`** to reflect the changes. The README is the single source of truth for detailed module documentation — this skill references it via progressive disclosure rather than duplicating it.
