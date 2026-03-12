# Sorolla Core

A reusable Unity framework: UI, level flow, persistence, currency, inventory, power-ups, haptics, tutorials, audio, and pooling.

> **Claude Code Setup** — After importing, run: `mkdir -p .claude/skills && ln -s "../../Assets/Sorolla Core/.claude/skills/sorolla-core" .claude/skills/sorolla-core`

---

## Table of Contents

- [Quick Start](#quick-start)
- [ServiceLocator](#servicelocator)
- [GameManager](#gamemanager)
- [LevelFlow](#levelflow)
- [UI](#ui)
- [SaveSystem](#savesystem)
- [Currency](#currency)
- [Inventory](#inventory)
- [PowerUps](#powerups)
- [Haptics](#haptics)
- [Tutorial & Highlight](#tutorial--highlight)
- [Audio](#audio)
- [Pool](#pool)
- [FTX (First-Time Experience)](#ftx-first-time-experience)
- [Utils](#utils)
- [Debug Tools](#debug-tools)
- [Namespaces](#namespaces)

---

## Quick Start

### 1. Scene Setup

```
Assets/Scenes/
├── Bootstrap.unity    # Build index 0
└── Game.unity         # Build index 1
```

**Bootstrap scene:**
```
├── GameInitializer     → _gameSceneName = "Game"
└── GameManager         → (or your subclass)
        ├── _tutorialController (optional)
        ├── _audioManager (optional)
        └── _gameManagers[] (your managers)
```

**Game scene:**
```
└── UIManager
    ├── _registry → UIRegistry ScriptableObject
    ├── _mainCanvas → Canvas
    └── Canvas
        ├── ScreensParent
        └── PanelsParent
```

### 2. Initialization Flow

```
GameInitializer.Start()
  → GameManager.InitializeAsync()
    → SaveSystem.Initialize()
    → AudioManager, TutorialController
    → _gameManagers[] (LevelFlowManager, CurrencyService, etc.)
  → Load Game scene (additive)
  → Set Game scene active
  → Fire OnSceneLoaded
  → Unload Bootstrap scene
```

GameManager persists via `DontDestroyOnLoad`.

### 3. Minimal Game Code

```csharp
public class MyLevelManager : LevelFlowManager
{
    [SerializeField] private ScriptableObject[] _levels;
    protected override int GetTotalLevelCount() => _levels.Length;
    protected override void OnLevelSetup(int levelIndex) { /* spawn level */ }
    protected override void OnLevelCleanup() { /* destroy level objects */ }
}
```

Add to GameManager's `_gameManagers` array. Then:

```csharp
var levelFlow = ServiceLocator.Instance.Resolve<ILevelFlowManager>();
levelFlow.StartLevel(1);
levelFlow.WinLevel();
levelFlow.LoseLevel(LevelEndReason.TimeUp);
```

### 4. Checklist

- [ ] Bootstrap + Game scenes in Build Settings (0, 1)
- [ ] GameInitializer + GameManager in Bootstrap
- [ ] UIManager + Canvas + UIRegistry in Game scene
- [ ] LevelFlowManager subclass in Game scene, added to `_gameManagers`
- [ ] LevelComplete + GameOver panel prefabs registered in UIRegistry

---

## ServiceLocator

Static dependency injection container. Namespace: `Sorolla`

| Method | Description |
|--------|-------------|
| `Register<T>(T service)` | Register a service |
| `Resolve<T>()` | Get service (logs error if missing) |
| `TryResolve<T>()` | Get service or null |
| `Has<T>()` | Check if registered |
| `Clear()` | Remove all services |
| `Reset()` | Destroy and recreate instance |
| `DEBUG_LogAll()` | Log all registered services (Editor/Dev only) |

```csharp
ServiceLocator.Instance.Register<IMyService>(myService);
var svc = ServiceLocator.Instance.Resolve<IMyService>();
```

---

## GameManager

Main orchestrator. Extends `MonoSingleton<GameManager>`. Namespace: `Sorolla`

| Member | Type | Description |
|--------|------|-------------|
| `IsInitialized` | Property | Init complete |
| `IsInitializing` | Property | Init in progress |
| `Audio` | Static property | AudioManager shortcut |
| `IsPaused` | Static property | Game paused state |
| `OnPauseStateChanged` | Static event | `Action<bool>` |
| `Pause()` | Static method | Pause game |
| `Resume()` | Static method | Resume game |
| `InitializeAsync(CancellationToken)` | Method | Full init sequence |

**Extend:**
```csharp
public class MyGameManager : GameManager
{
    protected override void Init() { base.Init(); /* register services */ }
    protected override async Task HandleSceneLoaded() { /* build level, init UI */ }
}
```

---

## LevelFlow

State machine for level progression with optional world/chapter grouping. Namespace: `Sorolla.LevelFlow`

### States
`Idle → Initializing → Playing ↔ Paused → Won/Lost → Idle`

### ILevelFlowManager API

| Method | Description |
|--------|-------------|
| `StartLevel(int index)` | Start a level |
| `RestartLevel()` | Restart current level |
| `PauseLevel()` / `ResumeLevel()` | Pause/resume |
| `WinLevel()` | Mark win, auto-save, show panel |
| `LoseLevel(LevelEndReason)` | Mark loss, show panel |
| `QuitLevel()` | Exit to menu |
| `AdvanceToNextLevel()` | Go to next level |
| `SaveProgress()` | Manual save |

| Property | Description |
|----------|-------------|
| `CurrentState` | `LevelState` enum |
| `CurrentLevelIndex` | 1-based level number |
| `HighestLevelReached` | Max level played |
| `IsLevelActive` | Playing or Paused |
| `GetActualLevelIndex()` | Content index (cycles if more levels than content) |

| Event | Signature |
|-------|-----------|
| `OnStateChanged` | `Action<LevelState>` |
| `OnLevelStarted` | `Action<int>` |
| `OnLevelEnded` | `Action<LevelEndReason>` |
| `OnLevelSetupRequested` | `Action<int>` |
| `OnLevelCleanupRequested` | `Action` |
| `OnWorldCompleted` | `Action<int>` |
| `OnWorldUnlocked` | `Action<int>` |

### World System (Optional)

```csharp
protected override WorldConfig[] GetWorldConfigs() => _worlds;
```

| Method | Description |
|--------|-------------|
| `UsesWorldSystem` | Whether worlds are configured |
| `GetWorldForLevel(int)` | World number for a global level |
| `GetLevelIndexInWorld(int)` | Local level index within world |
| `IsWorldUnlocked(int)` | Check world availability |

---

## UI

Screen/panel management with stack navigation and transitions. Namespace: `Sorolla.UI`

### UIManager API

| Method | Description |
|--------|-------------|
| `PushScreenAsync(UIScreenId, object args, bool clearStack)` | Show full-screen |
| `PopScreenAsync()` | Go back |
| `GetTopScreen()` | Current screen |
| `OpenPanelAsync(UIPanelId, object args)` | Show modal panel |
| `OpenPanelAsync(UIPanelId, object args, IUITransition)` | With custom transition |
| `ClosePanelAsync(UIPanel)` | Close panel |
| `ClosePanelsByIdAsync(UIPanelId)` | Close all matching |
| `BuildGameUI()` | Initialize gameplay HUD |
| `ShowGameUI(bool)` | Toggle gameplay HUD |
| `HandleBackAsync()` | Handle back button |

| Event | Signature |
|-------|-----------|
| `OnPanelOpened` | `Action<UIPanel>` |
| `OnPanelClosed` | `Action<UIPanel>` |

### Enum Ranges

- `UIScreenId`: 0-99 reserved for Core, 100+ for game
- `UIPanelId`: 0-99 reserved for Core, 100+ for game

### UIRegistry

Create: **Right-click > Create > Sorolla > UI > Registry**. Maps enum IDs to prefabs.

### Transitions

Create: **Right-click > Create > Sorolla > UI > Transitions > [Fade|Scale|Slide]**

```csharp
var scaleIn = Resources.Load<ScaleTransition>("UI/ScaleIn");
await uiManager.OpenPanelAsync(panelId, args, scaleIn);
```

### Dialogs

```csharp
// Toast
ToastManager.Instance.ShowToast("Achievement unlocked!");

// Confirm
await uiManager.OpenPanelAsync(UIPanelId.ConfirmDialog, new ConfirmDialog.Data {
    Title = "Confirm", Message = "Are you sure?",
    OnResult = (confirmed) => HandleResult(confirmed)
});
```

### Config-Driven Panels

```csharp
public class MyPanelConfig : PanelConfigBase<MyReasonEnum, MyVisualConfig> { }

public class MyPanel : ConfigurablePanel<MyReasonEnum, MyVisualConfig>
{
    protected override MyReasonEnum DefaultKey => MyReasonEnum.Default;
    protected override void ApplyConfig(MyVisualConfig config) { }
}
```

### Celebrations

```csharp
public class MyUnlockPanel : CelebrationPanel<MyUnlockData>
{
    protected override void UpdateUI(MyUnlockData data) { }
}
```

### Floating Text

```csharp
FloatingTextManager.Instance.ShowNumber(100, worldPosition, "+{0}", Color.gold);
```

### UI Prefab Templates

Template prefabs in `UI/Templates/`. Duplicate and customize for your game.

| Template | Component | Usage |
|----------|-----------|-------|
| BasePanel | - | Panel with background overlay + window |
| BaseButton | - | Button with optional icon + label |
| Toast | `ToastPanel` | Bottom notification |
| ConfirmDialog | `ConfirmDialog` | Two-button confirm |
| AlertDialog | `AlertDialog` | Single-button alert |

---

## SaveSystem

JSON-based persistence with versioning, migrations, and backups. Namespace: `Sorolla.PersistentData`

**Requires:** `com.unity.nuget.newtonsoft-json`

### API

| Method | Description |
|--------|-------------|
| `Save<T>(data, fileName, slot?, createBackup?)` | Save synchronously |
| `SaveAsync<T>(...)` | Save asynchronously |
| `Load<T>(fileName, slot?)` | Load (returns `new T()` if missing) |
| `Load<T>(fileName, slot?, defaultValue)` | Load with custom default |
| `LoadAsync<T>(...)` | Load asynchronously |
| `Exists(fileName, slot?)` | Check if file exists |
| `Delete(fileName, slot?, deleteBackups?)` | Delete save |
| `DeleteAllData(slot?)` | Delete all saves (slot=-1 for all) |
| `GetFilePath(fileName, slot?)` | Full path |
| `GetAllSaveFiles(slot?)` | List saves in slot |

| Property | Description |
|----------|-------------|
| `Events` | Save lifecycle callbacks |
| `Migrations` | Version migration pipeline |
| `Backups` | Backup manager |
| `Storage` | `IStorageProvider` instance |

### Define Save Data

```csharp
[Serializable]
public class PlayerData : ISaveData
{
    public int Version => 1;  // Increment for breaking changes
    public int coins;
    public List<string> inventory = new();
}
```

### Save Slots

```csharp
SaveSystem.Save(data, "player", slot: 1);
var data = SaveSystem.Load<PlayerData>("player", slot: 2);
```

### Defaults via ScriptableObject

```csharp
[CreateAssetMenu(menuName = "Game/Player Config")]
public class PlayerConfig : ScriptableObject, IDefaultsProvider<PlayerData>
{
    public int startingCoins = 100;
    public PlayerData CreateDefault() => new() { coins = startingCoins };
}

var data = SaveSystem.Load("player", 0, _config);
```

### Version Migrations

```csharp
SaveSystem.Migrations.Register<PlayerData>(1, 2, json => {
    var obj = JObject.Parse(json);
    obj["gems"] = 0;
    obj["version"] = 2;
    return obj.ToString();
});
```

Migrations chain automatically: v1 -> v2 -> v3.

### Backups

```csharp
SaveSystem.Backups.MaxBackups = 5;
var backups = SaveSystem.Backups.GetBackups("player");
SaveSystem.Backups.RestoreLatestBackup("player", SaveSystem.GetFilePath("player"));
```

### Events

```csharp
SaveSystem.Events.OnBeforeSave += (file, slot) => { };
SaveSystem.Events.OnAfterSave += (file, slot) => { };
SaveSystem.Events.OnBeforeLoad += (file, slot) => { };
SaveSystem.Events.OnAfterLoad += (file, slot) => { };
SaveSystem.Events.OnSaveCorrupted += (file, slot, ex) => { };
SaveSystem.Events.OnMigrationApplied += (file, slot, from, to) => { };
```

### Custom Storage

```csharp
public class CloudStorage : IStorageProvider { /* implement methods */ }
SaveSystem.Initialize(new CloudStorage());
```

### Editor Window

**Tools > Sorolla > Save Data Editor** - View, edit, delete save files.

### File Layout

```
Application.persistentDataPath/saves/
├── default/player.json
├── slot1/player.json
└── backups/player_20240115_143022.json
```

---

## Currency

Self-contained currency system with automatic persistence. Namespace: `Sorolla.Currency`

### Pre-defined IDs

`CurrencyIds.Coins`, `CurrencyIds.Gems`, `CurrencyIds.Energy`

### ICurrencyService API

| Method | Description |
|--------|-------------|
| `GetBalance(string id)` | Current amount |
| `CanAfford(string id, int amount)` | Check funds |
| `GetAllBalances()` | All currencies |
| `Add(string id, int amount)` | Add currency |
| `TrySpend(string id, int amount)` | Spend if affordable (returns bool) |
| `Set(string id, int amount)` | Set exact balance |

| Event | Signature |
|-------|-----------|
| `OnCurrencyChanged` | `Action<CurrencyChangedEventArgs>` |

`CurrencyChangedEventArgs`: `CurrencyId`, `PreviousBalance`, `NewBalance`, `ChangeType`, `Amount`

```csharp
var currency = ServiceLocator.Instance.Resolve<ICurrencyService>();
currency.Add(CurrencyIds.Coins, 100);
if (currency.TrySpend(CurrencyIds.Gems, 50)) { /* purchased */ }
```

### UI Binding

Add `CurrencyDisplay` component to a TextMeshProUGUI. Set `_currencyId` in inspector.

### Debug (Editor/Dev builds)

`DEBUG_SetBalance()`, `DEBUG_ResetAll()`

---

## Inventory

Generic item inventory with persistence. Namespace: `Sorolla.Inventory`

### IInventoryService API

| Method | Description |
|--------|-------------|
| `AddItem(string id, int count)` | Add items |
| `RemoveItem(string id, int count)` | Remove items |
| `HasItem(string id)` | Check ownership |
| `GetItemCount(string id)` | Item quantity |

| Event | Signature |
|-------|-----------|
| `OnInventoryChanged` | `Action<InventoryChangedEventArgs>` |

Extend `InventoryService` and override `SaveFileName` for game-specific persistence key.

---

## PowerUps

Power-up management with unlock progression, purchases, and quantity tracking. Namespace: `Sorolla.PowerUps`

### IPowerUpService API

| Method | Description |
|--------|-------------|
| `IsUnlocked(PowerUpIds id)` | Check availability |
| `GetQuantity(PowerUpIds id)` | Current stock |
| `TryUse(PowerUpIds id)` | Use one (returns bool) |
| `TryPurchase(PowerUpIds id)` | Buy with currency (returns bool) |
| `HasFirstUseFree(PowerUpIds id)` | Free use available |

| Event | Signature |
|-------|-----------|
| `OnQuantityChanged` | `Action<PowerUpIds, int>` |
| `OnPowerUpUnlocked` | `Action<PowerUpIds>` |
| `OnPowerUpUsed` | `Action<PowerUpIds>` |

### Setup

1. Create `PowerUpDefinitionBase` ScriptableObjects for each power-up
2. Create a `PowerUpRegistry` ScriptableObject, add definitions
3. Assign registry to `PowerUpService`

Unlocks trigger automatically based on `LevelFlowManager.CurrentLevelIndex`.

---

## Haptics

Cross-platform haptic feedback. Namespace: `Sorolla`

### IHapticsService API

| Member | Description |
|--------|-------------|
| `IsEnabled` | Get/set (persisted) |
| `IsSupported` | Device capability |
| `PlayImpact(HapticsIntensity)` | Light, Medium, Heavy |
| `PlaySelection()` | UI selection tap |
| `PlayNotification(HapticsType)` | Success, Warning, Error |

| Platform | Implementation |
|----------|----------------|
| iOS 10+ | UIFeedbackGenerator |
| Android API 26+ | VibrationEffect |
| Android < 26 | Basic vibration |
| Editor | Debug.Log |

```csharp
var haptics = ServiceLocator.Instance.Resolve<IHapticsService>();
haptics.PlayImpact(HapticsIntensity.Medium);
haptics.PlayNotification(HapticsType.Success);
```

---

## Tutorial & Highlight

Level-grouped tutorial system with camera-based highlighting. Namespace: `Sorolla.Tutorial`

### Setup

1. Create config: **Create > Sorolla > Tutorial > Tutorial Config**
2. Create steps: **Create > Sorolla > Tutorial > Tutorial Step**
3. Add Level Groups to config, assign steps
4. Assign config to TutorialController

### TutorialStepBase Properties

| Property | Description |
|----------|-------------|
| `Id` | Unique identifier |
| `InstructionText` | Player-facing message |
| `CompletionMode` | Manual, Event, Timed |
| `EntryMode` | Immediate, Gate |
| `EntryDelay` | Seconds before showing |
| `PauseGameplayDuringStep` | Pause game |
| `FreezePlayer` | Lock movement |
| `PanelPrefab` | UI to show |
| `ShowArrow` | Directional arrow |

### TutorialController API

| Method | Description |
|--------|-------------|
| `NotifyLevelPlay(int levelIndex)` | Call when level starts |
| `IsLevelTutorialCompleted(int)` | Check completion |
| `ResetTutorial()` | Reset all progress |
| `ConfigureLevelSteps(Dictionary)` | Runtime config |

**Static methods (call from anywhere):**

| Method | Description |
|--------|-------------|
| `Complete()` | Complete current step |
| `CompleteStep(string stepId)` | Complete by event ID |
| `TriggerGate(string stepId)` | Trigger gate entry |

| Event | Signature |
|-------|-----------|
| `OnTutorialStepChanged` | `Action<int level, int step>` |
| `OnTutorialStepEntered` | `Action<int level, int step, string stepId>` |

**Gameplay hooks:**
```csharp
tutorialController.SetGameplayPaused = (bool paused) => { };
tutorialController.SetFreezePlayer = (bool frozen) => { };
```

### TutorialObjectsHider

Shows/hides GameObjects by tutorial progress. Add component, configure `HideEntry` list:
- `Object`: GameObject to control
- `RevealLevel`: Show at this level or higher
- `RevealStepInLevel`: Show at this step index

### GateTriggerCollider

Triggers gate-waiting steps on collision. Set `_stepId` to match the step's `Id`.

### Highlight System

Camera-based spotlight effect: objects move to a separate layer rendered by an overlay camera on top of a dark overlay.

**Architecture:**
```
Main Camera → scene + dark overlay
Highlight Camera → URP overlay, highlighted items only
Tutorial Panel → Screen-space overlay, above everything
```

**Setup:**

1. Add layer `TutorialHighlight` in Project Settings
2. Create `HighlightManager` subclass implementing `IHighlightableProvider`:

```csharp
public class MyHighlightManager : HighlightManager, IHighlightableProvider
{
    [SerializeField] private HighlightConfig[] _configs;

    private void Start()
    {
        _highlightableProvider = this;
        foreach (var c in _configs) RegisterConfig(c.StepId, c);
    }

    public IEnumerable<IHighlightable> FindHighlightables(string[] typeIds)
    {
        var set = new HashSet<string>(typeIds);
        foreach (var item in FindObjectsByType<MyItem>(FindObjectsSortMode.None))
            if (item is IHighlightable h && h.CanBeHighlighted && set.Contains(h.HighlightTypeId))
                yield return h;
    }
}
```

3. Implement `IHighlightable` on target objects:
```csharp
public class MyItem : MonoBehaviour, IHighlightable
{
    public string HighlightTypeId => "my_item";
    public bool CanBeHighlighted => gameObject.activeSelf;
    GameObject IHighlightable.GameObject => gameObject;
    public void SetHighlighted(bool highlighted) { }
}
```

4. Implement `IInputLayerOverride` on your input handler to restrict raycasts during highlights

**Overlay options:** `HighlightOverlay` (world-space quad) or `HighlightOverlayUI` (screen-space Image).

**Extension points:** Override `OnItemHighlighted()`, `OnItemUnhighlighted()`, `ActivateHighlight()`, `ClearHighlights()`.

---

## Audio

Audio mixer management with per-channel control. Namespace: `Sorolla`

### AudioManager API

| Method | Description |
|--------|-------------|
| `PlayMusic(string key)` | Play music clip |
| `PlaySFX(string key)` | Play sound effect |
| `PlayUI(string key)` | Play UI sound |
| `SetVolume(Channel, float)` | Set channel volume (0-1) |
| `SetEnabled(Channel, bool)` | Mute/unmute channel |

| Property | Description |
|----------|-------------|
| `MasterVolume` / `MusicVolume` / `SFXVolume` / `UIVolume` | Float 0-1 |
| `MasterEnabled` / `MusicEnabled` / `SFXEnabled` / `UIEnabled` | Bool |

Channels: `Master`, `Music`, `SFX`, `UI`. Settings auto-persist via SaveSystem.

Setup: Create `AudioLibrary` ScriptableObject, map string keys to AudioClips.

---

## Pool

Generic object pooling. Namespace: `Sorolla`

### PoolManager API (Static)

| Method | Description |
|--------|-------------|
| `Register(Pool)` | Register a pool |
| `HasPool(string name)` | Check existence |
| `GetPoolByName(string name)` | Get pool |
| `ReturnAllToPool()` | Return all objects |
| `ClearAll()` | Destroy all pools |
| `Unregister(string name)` | Remove pool |

Create `Pool` components on GameObjects with prefab references. They auto-register.

---

## FTX (First-Time Experience)

Track one-time hints and first-use events. Namespace: `Sorolla.FTX`

### IFirstTimeExperienceService API

| Method | Description |
|--------|-------------|
| `HasSeen(string key)` | Already shown? |
| `MarkAsSeen(string key)` | Mark as shown |
| `CheckFirstTime(string key)` | Returns true on first call, marks seen |

Auto-persists via SaveSystem.

```csharp
var ftx = ServiceLocator.Instance.Resolve<IFirstTimeExperienceService>();
if (ftx.CheckFirstTime("shop_intro")) ShowShopTutorial();
```

---

## Utils

### SorollaTimer

Lightweight, pooling-free timer utility. No MonoBehaviour per-timer — driven by a single auto-created updater.

```csharp
// Simple timer
var timer = SorollaTimer.StartTimer(3f, () => Debug.Log("Done!"));

// Looping timer
var loop = SorollaTimer.StartTimer(1f, () => Debug.Log("Tick"), loop: true);

// Countdown with tick
var countdown = SorollaTimer.StartCountdown(10f,
    remaining => Debug.Log($"Time left: {remaining:F1}"),
    () => Debug.Log("Time's up!")
);

// Control
timer.Pause();
timer.Resume();
timer.Cancel();
timer.Restart();

// Properties
float progress = timer.Progress;  // 0-1
float remaining = timer.Remaining;
bool running = timer.IsRunning;

// Unscaled time (ignores Time.timeScale)
SorollaTimer.StartTimer(1f, callback, useUnscaledTime: true);

// Cancel all timers
SorollaTimer.CancelAll();
```

### SceneLoader

Static async wrapper around `SceneManager.LoadSceneAsync` with progress callbacks.

```csharp
// Load scene (replaces current)
await SceneLoader.LoadSceneAsync("GameScene");

// Load additively
await SceneLoader.LoadSceneAdditiveAsync("UI_Scene");

// With progress
await SceneLoader.LoadSceneAsync("Level1", progress => slider.value = progress);

// Unload
await SceneLoader.UnloadSceneAsync("UI_Scene");

// Reload current
await SceneLoader.ReloadCurrentSceneAsync();
```

### SafeAreaHandler

Component that adjusts a RectTransform to respect the device safe area (notches, status bars). Namespace: `Sorolla.UI`

Attach to any UI RectTransform. Per-edge toggles let you apply only to specific sides.

```
Inspector:
├── Apply Top ✓
├── Apply Bottom ✓
├── Apply Left ✓
└── Apply Right ✓
```

Updates automatically on orientation changes.

### MonoSingleton\<T\>

Generic singleton base for MonoBehaviours. Override `Init()` instead of `Awake()`. Auto-creates instance if needed, marks `DontDestroyOnLoad`.

### SorollaManager

Lightweight non-singleton manager base. Idempotent `Init()`, override `Initialize()` and `PostInitialize()`. Used by all services (Currency, Inventory, PowerUps, Haptics, FTX).

### FakeTouchCursor (Editor-only)

Finger sprite following the mouse for recording App Store videos. Uses new Input System. Compiles to nothing in builds.

Setup: Canvas (overlay, sort 999) > Image with hand sprite > `FakeTouchCursor` component.

### Other Utilities

| Script | Description |
|--------|-------------|
| `Billboard` | Face camera |
| `FitToCamera` | Aspect-ratio fitting |
| `ShapeGenerator` | Procedural shapes |
| `TMPCurveText` | Curved TextMeshPro |
| `TMPArcPopAnimator` | Arc pop animations |
| `TMPTextMirror` | Mirror TMP text |
| `InfinityMovement` | Wrap-around movement |
| `LevelObjectsHider` | Show/hide by level |
| `FlickerLight` | Light flicker effect |
| `UIGradient` | Gradient on UI elements |

---

## Debug Tools

### SorollaDebugMenu

Tap 4x on screen to open. Provides level selection for testing.

### Editor Tools

| Tool | Location |
|------|----------|
| Save Data Editor | Tools > Sorolla > Save Data Editor |
| Prefab Icon Generator | Tools > Sorolla > Prefab Icon Generator |
| Play Mode Start Scene | Auto-loads Bootstrap on Play |
| Texture Import Settings | Bulk texture settings |

---

## Namespaces

| Namespace | Scope |
|-----------|-------|
| `Sorolla` | Core (ServiceLocator, GameManager, Audio, Haptics, Pool, Utils) |
| `Sorolla.LevelFlow` | Level progression |
| `Sorolla.PersistentData` | Save/load system |
| `Sorolla.Currency` | Currency system |
| `Sorolla.Inventory` | Inventory system |
| `Sorolla.PowerUps` | Power-up system |
| `Sorolla.UI` | UI core |
| `Sorolla.UI.Transitions` | Panel transitions |
| `Sorolla.UI.Dialogs` | Toast, Confirm, Alert |
| `Sorolla.UI.Celebrations` | Celebration panels |
| `Sorolla.UI.Effects` | Floating text |
| `Sorolla.UI.Config` | Config-driven panels |
| `Sorolla.Tutorial` | Tutorial system |
| `Sorolla.FTX` | First-time experience |
