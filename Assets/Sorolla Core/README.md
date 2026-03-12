# Sorolla Core Package

A reusable Unity framework for common game systems including UI management, level flow, persistence, currency, haptics, tutorials, and audio.

> **Claude Code Setup** — After importing this package, run from your project root:
> ```bash
> mkdir -p .claude/skills && ln -s "../../Assets/Sorolla Core/.claude/skills/sorolla-core" .claude/skills/sorolla-core
> ```

## Table of Contents

- [Quick Start](#quick-start)
- [Package Structure](#package-structure)
- [Core Principles](#core-principles)
- [Integration Guide](#integration-guide)
- [What Belongs Where](#what-belongs-where)
- [Migration Notes](#migration-notes)
- [Namespace Convention](#namespace-convention)
- **Module Guides**
  - [UI Module](#sorollaui-module-guide)
  - [LevelFlow Module](#sorollalevelflow-module-guide)
  - [PersistentData Module](#sorollapersistentdata-module-guide)
  - [Currency Module](#sorollacurrency-module-guide)
  - [Haptics Module](#sorollahaptics-module-guide)
  - [Pool Module](#pool-module-guide)

---

## Quick Start

Step-by-step guide to set up a new project with Sorolla Core.

### 1. Create Scenes

```
Assets/Scenes/
├── Bootstrap.unity    # First scene (index 0 in Build Settings)
└── Game.unity         # Main gameplay scene (index 1)
```

### 2. Bootstrap Scene Setup

```
Hierarchy:
├── GameInitializer (GameObject)
│   └── GameInitializer.cs
│       └── _gameSceneName → "Game"
│
└── GameManager (GameObject)
    └── GameManager.cs (or your subclass)
        ├── _tutorialController → (optional)
        ├── _audioManager → (optional)
        └── _gameManagers → (add your managers later)
```

> **Note**: GameManager must be in the Bootstrap scene because GameInitializer needs it before loading the Game scene. It auto-persists via DontDestroyOnLoad.

#### GameInitializer Flow

```
┌─────────────────────────────────────────────────────────────┐
│  BOOTSTRAP SCENE (index 0)                                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. Awake()                                                 │
│     └── Store reference to Bootstrap scene                  │
│                                                             │
│  2. Start()                                                 │
│     ├── Get GameManager.Instance                            │
│     │   └── MonoSingleton finds it in Bootstrap scene       │
│     │                                                       │
│     ├── await GameManager.InitializeAsync()                 │
│     │   ├── Registers services in ServiceLocator            │
│     │   ├── Initializes SaveSystem (persistence ready)      │
│     │   ├── Initializes AudioManager (loads saved prefs)    │
│     │   ├── Initializes TutorialController (loads progress) │
│     │   └── Initializes all _gameManagers                   │
│     │                                                       │
│     ├── Load Game scene (additive)                          │
│     │   └── await scene load complete                       │
│     │                                                       │
│     ├── Set Game scene as active                            │
│     │                                                       │
│     ├── Fire OnSceneLoaded event                            │
│     │   └── GameManager.HandleSceneLoaded() responds        │
│     │                                                       │
│     ├── Notify PreInitLoader (if exists)                    │
│     │   └── Hides loading screen                            │
│     │                                                       │
│     └── Unload Bootstrap scene                              │
│         └── GameInitializer destroyed                       │
│             (GameManager persists via DontDestroyOnLoad)    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  GAME SCENE (now active)                                    │
├─────────────────────────────────────────────────────────────┤
│  • UIManager ready                                          │
│  • Your game managers ready                                 │
│  • Ready to call levelFlow.StartLevel(1)                    │
└─────────────────────────────────────────────────────────────┘
```

### 3. Game Scene Setup

**UIManager**:
```
Hierarchy:
└── UIManager (GameObject)
    ├── UIManager.cs
    │   ├── _registry → (drag UIRegistry ScriptableObject)
    │   ├── _screensParent → ScreensParent
    │   ├── _panelsParent → PanelsParent
    │   └── _mainCanvas → Canvas
    └── Canvas
        ├── ScreensParent (empty RectTransform)
        └── PanelsParent (empty RectTransform)
```

Create UIRegistry: **Right-click > Create > Sorolla > UI > Registry**

### 4. Create Your LevelFlowManager

```csharp
using Sorolla.LevelFlow;
using UnityEngine;

public class MyLevelManager : LevelFlowManager
{
    [SerializeField] private ScriptableObject[] _levels; // Your level configs

    protected override int GetTotalLevelCount() => _levels.Length;

    protected override void OnLevelSetup(int levelIndex)
    {
        // Load level data, spawn objects, etc.
        Debug.Log($"Setting up level {levelIndex}");
    }

    protected override void OnLevelCleanup()
    {
        // Destroy spawned objects, reset state
        Debug.Log("Cleaning up level");
    }
}
```

### 5. Wire Everything Together

1. Add `MyLevelManager` component to a GameObject in Game scene
2. Select the **GameManager** GameObject
3. In the Inspector, expand **Game-Specific Managers**
4. Drag your `MyLevelManager` GameObject into the `_gameManagers` array

### 6. Create Basic UI Panels

Create prefabs for at minimum:
- `LevelCompletePanel` (for `UIPanelId.LevelComplete`)
- `GameOverPanel` (for `UIPanelId.GameOver`)

Register them in your **UIRegistry** ScriptableObject.

### 7. Build Settings

1. **File > Build Settings**
2. Add scenes in order:
   - `Bootstrap` (index 0)
   - `Game` (index 1)

### 8. Test It

```csharp
// From any script, start a level:
var levelFlow = ServiceLocator.Instance.Resolve<ILevelFlowManager>();
levelFlow.StartLevel(1);

// When player wins:
levelFlow.WinLevel();

// When player loses:
levelFlow.LoseLevel(LevelEndReason.TimeUp);
```

### Quick Checklist

- [ ] Create Bootstrap + Game scenes
- [ ] Add `GameInitializer` to Bootstrap scene
- [ ] Add `GameManager` to Bootstrap scene (persists via DontDestroyOnLoad)
- [ ] Add `UIManager` with Canvas + UIRegistry to Game scene
- [ ] Create `UIRegistry` ScriptableObject
- [ ] Create your `LevelFlowManager` subclass
- [ ] Add your manager to GameManager's `_gameManagers` array
- [ ] Create LevelComplete and GameOver panel prefabs
- [ ] Register panels in UIRegistry
- [ ] Set Build Settings scene order (Bootstrap = 0, Game = 1)

### Dependency Order

```
GameInitializer
    ↓
GameManager.InitializeAsync()
    ↓
SaveSystem (initialized first - persistence ready)
    ↓
AudioManager, TutorialController (can now load saved prefs)
    ↓
_gameManagers (LevelFlowManager, CurrencyService, etc.)
    ↓
Load Game Scene
    ↓
UIManager (in Game scene - needed for panels)
    ↓
Ready to play!
```

---

## Package Structure

```
Sorolla Core/
├── Managers/           # Core service infrastructure
│   ├── ServiceLocator.cs        # Dependency injection container
│   ├── SorollaManager.cs        # Base class for managers
│   ├── GameManager.cs           # Main game orchestrator
│   ├── GameInitializer.cs       # Scene initialization
│   ├── AudioManager.cs          # Audio system
│   ├── AudioLibrary.cs          # Audio asset registry
│   └── FXPoolService.cs         # Object pooling for VFX
│
├── LevelFlow/          # Level flow management system
│   ├── ILevelFlowManager.cs     # Level flow interface
│   ├── LevelFlowManager.cs      # Abstract base class
│   ├── LevelState.cs            # Level state enum
│   ├── LevelEndReason.cs        # Win/lose reason enum
│   ├── LevelProgressData.cs     # Persistence data
│   └── WorldConfig.cs           # Optional world/chapter config
│
├── PersistentData/     # Save/load system
│   ├── Runtime/
│   │   ├── Core/
│   │   │   ├── SaveSystem.cs        # Main static API
│   │   │   ├── ISaveData.cs         # Data interface
│   │   │   ├── IStorageProvider.cs  # Storage abstraction
│   │   │   ├── SaveResult.cs        # Operation result
│   │   │   └── SaveEvents.cs        # Save/load events
│   │   ├── Storage/
│   │   │   └── LocalFileStorage.cs  # File-based storage
│   │   ├── Migration/
│   │   │   ├── IMigrator.cs         # Migrator interface
│   │   │   └── MigrationPipeline.cs # Version migrations
│   │   ├── Backup/
│   │   │   └── BackupManager.cs     # Backup management
│   │   ├── Validation/
│   │   │   └── SaveValidator.cs     # JSON validation
│   │   └── Defaults/
│   │       └── IDefaultsProvider.cs # Default value provider
│   └── Editor/
│       ├── SaveDataEditorWindow.cs  # Save file viewer/editor
│       └── BuildPreprocessor.cs     # Build warnings
│
├── Currency/           # Currency management system
│   ├── Runtime/
│   │   ├── ICurrencyService.cs      # Service interface
│   │   ├── CurrencyService.cs       # MonoBehaviour implementation
│   │   ├── CurrencyData.cs          # Serializable data
│   │   ├── CurrencyIds.cs           # Pre-defined currency IDs
│   │   ├── CurrencyChangedEventArgs.cs
│   │   └── CurrencyChangeType.cs
│   └── Editor/
│       └── CurrencyServiceEditor.cs # Inspector with debug tools
│
├── Haptics/            # Cross-platform haptic feedback
│   └── Runtime/
│       ├── IHapticsService.cs       # Service interface
│       ├── HapticsService.cs        # iOS/Android implementation
│       ├── HapticsData.cs           # Persistence data
│       ├── HapticsIntensity.cs      # Light/Medium/Heavy
│       └── HapticsType.cs           # Selection/Success/Warning/Error
│
├── Pool/               # Object pooling
│   ├── Pool.cs                  # Generic pool
│   └── PoolManager.cs           # Pool management
│
├── UI/                 # Modular UI management system
│   ├── UIManager.cs             # Panel/screen management
│   ├── UIRegistry.cs            # ScriptableObject UI registry
│   ├── UIScreen.cs              # Base screen/panel classes
│   ├── UIenums.cs               # Screen/panel ID enums
│   ├── IGameplayUI.cs           # Gameplay UI abstraction
│   ├── Core/
│   │   └── IUITransition.cs     # Transition animation interface
│   ├── Transitions/             # DOTween-based transitions
│   │   ├── UITransitionBase.cs
│   │   ├── FadeTransition.cs
│   │   ├── ScaleTransition.cs
│   │   ├── SlideTransition.cs
│   │   └── UIOverlay.cs
│   ├── Dialogs/
│   │   ├── ConfirmDialog.cs
│   │   ├── AlertDialog.cs
│   │   ├── ToastPanel.cs
│   │   └── ToastManager.cs
│   ├── Celebrations/
│   │   └── CelebrationPanel.cs
│   ├── Effects/
│   │   ├── FloatingTextManager.cs
│   │   └── FloatingTextPopup.cs
│   └── Config/
│       ├── PanelConfigBase.cs
│       └── ConfigurablePanel.cs
│
├── Tutorial/           # Tutorial system framework
│   ├── TutorialController.cs    # Tutorial orchestrator
│   ├── TutorialStepBase.cs      # Base class for tutorial steps
│   └── ... (arrow, gate, hider components)
│
├── Utils/              # Utility classes
│   ├── MonoSingleton.cs         # Singleton pattern
│   └── FakeTouchCursor.cs       # Editor-only cursor overlay for recording
│
└── URP Shaders/        # Shader utilities
```

## Core Principles

### 1. Agnostic Design
Sorolla Core should **never** reference game-specific code. All game-specific functionality should be accessed through:
- **Interfaces** defined in Core (e.g., `IGameplayUI`, `ILevelFlowManager`, `IPlayerProvider`)
- **ServiceLocator** for runtime dependency resolution
- **Events** for decoupled communication

### 2. Interface-Based Coupling
Games implement Core interfaces and register them via ServiceLocator:

```csharp
// Game registers its implementations
ServiceLocator.Instance.Register<IGameplayUI>(myLevelUI);

// LevelFlowManager auto-registers itself as ILevelFlowManager

// Core resolves them at runtime
var gameplayUI = ServiceLocator.Instance.TryResolve<IGameplayUI>();
gameplayUI?.ShowGameplayUI(true);

var levelFlow = ServiceLocator.Instance.Resolve<ILevelFlowManager>();
levelFlow.StartLevel(1);
```

### 3. Extensible Enums
UI enum values are structured:
- **0-99**: Reserved for Sorolla Core (generic screens/panels)
- **100+**: Available for game-specific screens/panels

Games can define their own values without modifying Core enums.

## Integration Guide

### Required Implementations
Games using Sorolla Core should implement:

1. **`IGameplayUI`** - Gameplay HUD management
   - Register in `Awake()` of your gameplay UI component

2. **`LevelFlowManager`** (recommended) - Level flow management
   - Extend this abstract class for your game
   - Handles states (Playing, Paused, Won, Lost), persistence, UI integration
   - Optional world/chapter system via `GetWorldConfigs()`
   - Auto-registers as `ILevelFlowManager`

3. **`IPersistenceService`** (optional) - Custom data persistence
   - Or use the built-in `SaveSystem` from PersistentData module

### Scene Setup

1. Create a **Bootstrap scene** with `GameInitializer` and `GameManager`
2. Create a **Game scene** with UIManager and your game implementation
3. GameManager auto-persists via DontDestroyOnLoad when accessed

### Extending GameManager

```csharp
public class MyGameManager : Sorolla.GameManager
{
    protected override void Init()
    {
        base.Init();
        // Register game-specific services
        RegisterMyServices();
    }

    protected override async Task HandleSceneLoaded()
    {
        // Build your game level
        await myLevelManager.BuildCurrentLevel();

        // Initialize Core UI (UIManager is a singleton)
        UIManager.Instance?.BuildGameUI();
    }
}
```

## What Belongs Where

### In Sorolla Core ✅
- Service infrastructure (ServiceLocator, SorollaManager, GameManager)
- Level flow base class (LevelFlowManager)
- Persistence system (SaveSystem, ISaveData)
- Currency system (CurrencyService)
- Haptics system (HapticsService)
- UI infrastructure (UIManager, UIScreen, UIPanel)
- Tutorial framework
- Audio system
- Object pooling
- Common utilities (MonoSingleton)

### In Game Project ❌
- Concrete LevelFlowManager subclass
- Game-specific save data classes
- Game-specific UI components
- Game-specific enums and data structures
- Level configurations
- Game logic and controllers

## Migration Notes

When extracting Sorolla Core as a standalone package:

1. Remove all `using HungrySnake.*` statements from Core files
2. Replace direct type references with interface resolutions
3. Move game-specific debug tools to game project
4. Update UIenums to use only generic values (games extend with their own)
5. Ensure all serialized fields reference only Core types or Unity built-ins

## Namespace Convention

- **`Sorolla`** - Core infrastructure (includes Haptics, Pool)
- **`Sorolla.LevelFlow`** - Level flow management system
- **`Sorolla.PersistentData`** - Save/load system
- **`Sorolla.Currency`** - Currency system
- **`Sorolla.UI`** - UI system core
- **`Sorolla.UI.Transitions`** - Transition animations
- **`Sorolla.UI.Dialogs`** - Common dialogs (Toast, Confirm, Alert)
- **`Sorolla.UI.Celebrations`** - Celebration/unlock panels
- **`Sorolla.UI.Effects`** - UI effects (floating text)
- **`Sorolla.UI.Config`** - Config-driven panels
- **`Sorolla.Tutorial`** - Tutorial system
- **`[GameName]`** - Game-specific implementation (e.g., `GrannysAttic`)

## Sorolla.UI Module Guide

### Core (Required)
The core UI system with UIManager, UIScreen, UIPanel, and UIRegistry.

### Transitions (Optional)
DOTween-based panel transitions. Create ScriptableObject assets:
```csharp
// Create transition asset: Right-click > Create > Sorolla > UI > Transitions > Scale
var scaleIn = Resources.Load<ScaleTransition>("UI/ScaleIn");
await uiManager.OpenPanelAsync(panelId, args, scaleIn);
```

### Dialogs (Optional)
Common dialog panels:
```csharp
// Toast notification
ToastManager.Instance.ShowToast("Achievement unlocked!");

// Confirm dialog
await uiManager.OpenPanelAsync(UIPanelId.ConfirmDialog, new ConfirmDialog.Data
{
    Title = "Confirm",
    Message = "Are you sure?",
    OnResult = (confirmed) => HandleResult(confirmed)
});
```

### Celebrations (Optional)
Template for unlock/celebration panels:
```csharp
public class MyUnlockPanel : CelebrationPanel<MyUnlockData>
{
    protected override void UpdateUI(MyUnlockData data)
    {
        // Update UI with unlock data
    }
}
```

### Effects (Optional)
Floating text effects for scores, damage numbers, etc:
```csharp
FloatingTextManager.Instance.ShowNumber(100, worldPosition, "+{0}", Color.gold);
```

### Config (Optional)
Data-driven panel configuration:
```csharp
// Define your config ScriptableObject
public class MyPanelConfig : PanelConfigBase<MyReasonEnum, MyVisualConfig> { }

// Create config-driven panel
public class MyPanel : ConfigurablePanel<MyReasonEnum, MyVisualConfig>
{
    protected override MyReasonEnum DefaultKey => MyReasonEnum.Default;
    protected override void ApplyConfig(MyVisualConfig config) { /* ... */ }
}
```

---

## Sorolla.LevelFlow Module Guide

Abstract base class for level flow management with optional world/chapter grouping.

### Setup
Extend `LevelFlowManager` and implement abstract methods:

```csharp
public class MyLevelManager : LevelFlowManager
{
    [SerializeField] private LevelConfig[] _levels;

    protected override int GetTotalLevelCount() => _levels.Length;

    protected override void OnLevelSetup(int levelIndex)
    {
        var config = _levels[levelIndex - 1];
        // Spawn items, setup goals, etc.
    }

    protected override void OnLevelCleanup()
    {
        // Destroy spawned objects
    }
}
```

### With World System (Optional)
```csharp
public class MyLevelManager : LevelFlowManager
{
    [SerializeField] private WorldConfig[] _worlds;  // Create via: Create > Sorolla > Level Flow > World Config

    protected override WorldConfig[] GetWorldConfigs() => _worlds;
    // ... rest same as above
}
```

### Usage
```csharp
var levelFlow = ServiceLocator.Instance.Resolve<ILevelFlowManager>();

// Start level
levelFlow.StartLevel(1);

// Control
levelFlow.PauseLevel();
levelFlow.ResumeLevel();
levelFlow.RestartLevel();

// End level
levelFlow.WinLevel();  // Auto-saves, shows LevelComplete panel
levelFlow.LoseLevel(LevelEndReason.TimeUp);  // Shows GameOver panel

// Events
levelFlow.OnLevelStarted += (levelIndex) => { };
levelFlow.OnLevelEnded += (reason) => { };
levelFlow.OnWorldCompleted += (worldIndex) => { };

// World queries (safe to call even without worlds)
if (levelFlow.UsesWorldSystem)
{
    int world = levelFlow.GetWorldForLevel(25);
    int localLevel = levelFlow.GetLevelIndexInWorld(25);
}
```

---

## Sorolla.PersistentData Module Guide

JSON-based save/load system with versioning, migrations, and backups.

### Define Save Data
```csharp
[Serializable]
public class PlayerData : ISaveData
{
    public int Version => 1;  // Increment when making breaking changes
    public int coins;
    public int highScore;
    public List<string> unlockedItems = new();
}
```

### Save & Load
```csharp
// Save
var data = new PlayerData { coins = 100 };
SaveSystem.Save(data, "player");

// Load (returns new instance if not found)
var loaded = SaveSystem.Load<PlayerData>("player");

// Async variants
await SaveSystem.SaveAsync(data, "player");
var loaded = await SaveSystem.LoadAsync<PlayerData>("player");

// With save slots
SaveSystem.Save(data, "player", slot: 1);
var slot1Data = SaveSystem.Load<PlayerData>("player", slot: 1);
```

### Version Migrations
```csharp
// Register migration from v1 to v2
SaveSystem.Migrations.Register<PlayerData>(1, 2, json => {
    var obj = JObject.Parse(json);
    obj["gems"] = 0;  // Add new field
    obj["version"] = 2;
    return obj.ToString();
});
```

### Events
```csharp
SaveSystem.Events.OnAfterSave += (fileName, slot) => Debug.Log($"Saved {fileName}");
SaveSystem.Events.OnSaveCorrupted += (fileName, slot, ex) => Debug.LogError("Corrupted!");
```

### Editor Window
Open via: **Tools > Sorolla > Save Data Editor**
- View all save files
- Edit JSON fields
- Reset to defaults
- Delete saves

---

## Sorolla.Currency Module Guide

Self-contained currency system with automatic persistence.

### Pre-defined Currencies
- `CurrencyIds.Coins` - Soft currency (default: 0)
- `CurrencyIds.Gems` - Hard currency (default: 0)
- `CurrencyIds.Energy` - Regenerating resource (default: 100)

### Usage
```csharp
var currency = ServiceLocator.Instance.Resolve<ICurrencyService>();

// Query
int coins = currency.GetBalance(CurrencyIds.Coins);
bool canBuy = currency.CanAfford(CurrencyIds.Gems, 50);

// Modify
currency.Add(CurrencyIds.Coins, 100);
if (currency.TrySpend(CurrencyIds.Gems, 50))
{
    // Purchase successful
}

// Events (for UI binding)
currency.OnCurrencyChanged += args => {
    Debug.Log($"{args.CurrencyId}: {args.PreviousBalance} → {args.NewBalance}");
};
```

### Debug Tools (Editor/Development)
```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
currency.DEBUG_SetBalance(CurrencyIds.Coins, 99999);
currency.DEBUG_ResetAll();
#endif
```

---

## Sorolla.Haptics Module Guide

Cross-platform haptic feedback for iOS and Android.

### Usage
```csharp
var haptics = ServiceLocator.Instance.Resolve<IHapticsService>();

// Toggle (persisted automatically)
haptics.IsEnabled = true;

// Impact feedback
haptics.PlayImpact(HapticsIntensity.Light);   // Soft tap
haptics.PlayImpact(HapticsIntensity.Medium);  // Standard tap
haptics.PlayImpact(HapticsIntensity.Heavy);   // Strong tap

// Notification feedback
haptics.PlaySelection();                       // UI selection
haptics.PlayNotification(HapticsType.Success); // Win/complete
haptics.PlayNotification(HapticsType.Warning); // Caution
haptics.PlayNotification(HapticsType.Error);   // Fail/error
```

### Platform Support
- **iOS**: Native UIFeedbackGenerator (iOS 10+)
- **Android**: Vibrator API with VibrationEffect (API 26+)
- **Editor**: Debug logs

---

## Pool Module Guide

Generic object pooling for performance.

```csharp
// Get from pool
var bullet = PoolManager.Instance.Get<Bullet>(bulletPrefab);

// Return to pool
PoolManager.Instance.Return(bullet);

// Pre-warm pool
PoolManager.Instance.Prewarm(bulletPrefab, count: 20);
```

---

## Utils

### FakeTouchCursor (Editor-only)

Displays a hand/finger sprite that follows the mouse cursor during play mode. Used for recording App Store preview videos with Unity Recorder where touch input would otherwise be invisible.

**Features:**
- Sprite follows cursor with pivot at the fingertip (for hand/arm sprites)
- Scale-down animation on tap
- Optional ParticleSystem burst on tap

**Setup:**
1. Create a Canvas (Screen Space - Overlay, Sort Order 999)
2. Add an Image child with your hand sprite — set the pivot to the index fingertip
3. Add `FakeTouchCursor` component, assign Canvas and Image
4. (Optional) Add a ParticleSystem (no loop, no play-on-awake), assign to `Tap FX`
5. Record with Unity Recorder, delete Canvas when done

Uses the new Input System (`Mouse.current`). Compiles to nothing in builds (`#if UNITY_EDITOR`).

