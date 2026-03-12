# Tutorial System

A level-grouped tutorial system for Unity. Configure via ScriptableObjects, no code changes per game.

## Quick Start

1. Create config: `Assets > Create > Sorolla > Tutorial > Tutorial Config`
2. Create steps: `Assets > Create > Sorolla > Tutorial > Tutorial Step`
3. In TutorialConfig, add Level Groups and assign steps
4. Assign config to TutorialController's `_config` field
5. Call `NotifyLevelPlay(levelIndex)` when a level starts

## TutorialConfig

Groups steps by level. Each `LevelTutorialGroup` has:
- `LevelIndex` - Which level this group belongs to
- `Steps` - List of TutorialStepBase assets

```
Level Groups:
  [0] LevelIndex: 1, Steps: [WelcomeStep, MoveStep]
  [1] LevelIndex: 2, Steps: [PowerUpStep]
  [2] LevelIndex: 5, Steps: [BossIntroStep]
```

## TutorialStepBase

| Property | Description |
|----------|-------------|
| `Id` | Unique identifier (for gates/events/highlights) |
| `InstructionText` | Message shown to player |
| `CompletionMode` | Manual, Event, or Timed |
| `EntryMode` | Immediate or Gate |
| `EntryDelay` | Delay in seconds before step shows |
| `AutoCompleteDelay` | For Timed mode |
| `PauseGameplayDuringStep` | Pause game while step is active |
| `FreezePlayer` | Freeze player movement |
| `PanelPrefab` | UI panel to instantiate |
| `ShowArrow` | Show directional arrow |

## TutorialStepPanel

Base class for tutorial step UI panels. Provides a button that completes the step when clicked.

```csharp
// Create a game-specific panel by inheriting:
public class MyTutorialPanel : TutorialStepPanel { }
```

In the prefab, assign the OK/Continue button to `_completeButton`. The base class auto-wires it to call `TutorialController.Complete()`.

## TutorialController API

```csharp
// Call when level starts
tutorialController.NotifyLevelPlay(int levelIndex);

// Check completion
bool done = tutorialController.IsLevelTutorialCompleted(int levelIndex);

// Reset all progress
tutorialController.ResetTutorial();

// Runtime config (alternative to inspector)
tutorialController.ConfigureLevelSteps(Dictionary<int, List<TutorialStepBase>> levelSteps);
```

### Static Methods (call from anywhere)

```csharp
TutorialController.Complete();              // Complete current step (Manual mode)
TutorialController.CompleteStep("step_id"); // Complete by event ID
TutorialController.TriggerGate("gate_id");  // Trigger gate entry
```

### Events

```csharp
// Fired before step changes (level, stepInLevel) — used by TutorialObjectsHider
TutorialController.OnTutorialStepChanged += (int level, int step) => { };

// Fired after entry delay completes (level, stepInLevel, stepId) — used by HighlightManager
TutorialController.OnTutorialStepEntered += (int level, int step, string stepId) => { };
```

### Properties

```csharp
tutorialController.IsRunning;          // Tutorial currently active
tutorialController.CurrentLevel;       // Current level index
tutorialController.CurrentStepInLevel; // Current step within level
```

### Gameplay Hooks

```csharp
tutorialController.SetGameplayPaused = (bool paused) => { /* pause/resume */ };
tutorialController.SetFreezePlayer = (bool frozen) => { /* freeze/unfreeze */ };
```

## TutorialObjectsHider

Shows/hides objects based on tutorial progress. Add to any GameObject.

`HideEntry` fields:
- `Object` - GameObject to show/hide
- `RevealLevel` - Show when playing this level or higher
- `RevealStepInLevel` - Show at this step index (0 = level start)

Objects reveal when: `currentLevel > RevealLevel` OR `(currentLevel == RevealLevel AND step >= RevealStepInLevel)`

## GateTriggerCollider

Triggers a gate-waiting step when player enters collider.

- `_stepId` - Must match the step's `Id`
- `_triggerOnce` - Disable after first trigger
- `_requiredTag` - Only trigger for objects with this tag

## Persistence

Completed levels saved as comma-separated string under key `"tutorial_completed_levels"`.

Requires `IPersistenceService` via ServiceLocator or manual assignment.

---

# Highlight System

Camera-based highlighting for tutorial steps. Moves objects to a separate layer rendered by an overlay camera on top of a dark overlay, creating a spotlight effect.

## Architecture

```
Main Camera ─── renders scene (minus highlighted items) + dark overlay
                         │
Highlight Camera ─── URP overlay, renders only highlighted items on top
                         │
Tutorial Panel ─── Screen-space overlay UI, renders above everything
```

## Setup

### 1. Create the "TutorialHighlight" layer

In `Project Settings > Tags & Layers`, add a layer named `TutorialHighlight` (or customize via `_highlightLayerName`).

### 2. Create a HighlightManager subclass

The base `HighlightManager` is abstract. Create a game-specific subclass:

```csharp
public class MyHighlightManager : HighlightManager, IHighlightableProvider
{
    [SerializeField] private HighlightConfig[] _highlightConfigs;

    private void Start()
    {
        // Wire dependencies
        _highlightableProvider = this;
        _inputLayerOverride = FindAnyObjectByType<MyInputHandler>() as IInputLayerOverride;

        // Register step configs
        foreach (var config in _highlightConfigs)
            RegisterConfig(config.StepId, config);
    }

    public IEnumerable<IHighlightable> FindHighlightables(string[] typeIds)
    {
        var typeSet = new HashSet<string>(typeIds);
        foreach (var item in FindObjectsByType<MyItem>(FindObjectsSortMode.None))
        {
            if (item is IHighlightable h && h.CanBeHighlighted && typeSet.Contains(h.HighlightTypeId))
                yield return h;
        }
    }
}
```

### 3. Implement IHighlightable on objects

```csharp
public class MyItem : MonoBehaviour, IHighlightable
{
    public string HighlightTypeId => "my_item";
    public bool CanBeHighlighted => gameObject.activeSelf;
    GameObject IHighlightable.GameObject => gameObject;
    public void SetHighlighted(bool highlighted) { /* optional visual feedback */ }
}
```

### 4. Implement IInputLayerOverride on input handler

```csharp
public class MyInputHandler : MonoBehaviour, IInputLayerOverride
{
    [SerializeField] private LayerMask _defaultLayer;

    public LayerMask LayerMaskOverride { get; set; }

    private void TryInteract(Vector2 screenPos)
    {
        var ray = _camera.ScreenPointToRay(screenPos);
        var mask = LayerMaskOverride != 0 ? LayerMaskOverride : _defaultLayer;
        if (Physics.Raycast(ray, out var hit, 100f, mask))
        {
            // handle hit
        }
    }
}
```

### 5. Scene setup

Add a GameObject with your `HighlightManager` subclass. Assign:

| Field | Value |
|-------|-------|
| `_overlay` | A `HighlightOverlayBase` component (see Overlay Options below) |
| `_mainCamera` | Main gameplay camera |
| `_highlightCamera` | Overlay camera (child or sibling, see below) |
| `_highlightLayerName` | `TutorialHighlight` (default) |

**Highlight Camera setup:**
- Create a Camera as a separate GameObject (NOT the same one as the HighlightManager — it gets disabled at startup)
- The script auto-configures it as a URP overlay camera culling only the highlight layer

**Important:** The HighlightManager must NOT be on the same GameObject as the highlight camera, because the camera is disabled on startup which would also disable the manager.

### 6. Configure HighlightConfig

In the inspector, add entries to the highlight configs array:

| Field | Description |
|-------|-------------|
| `StepId` | Must match the tutorial step's `Id` |
| `HighlightTypeIds` | Array of type IDs to highlight (matched against `IHighlightable.HighlightTypeId`) |

## Overlay Options

Two overlay types are provided. Both extend `HighlightOverlayBase`.

### HighlightOverlay (World-Space Quad)

A world-space dark quad positioned between the camera and the scene. Best for top-down or fixed-angle cameras.

| Field | Description |
|-------|-------------|
| `_overlayMaterial` | Semi-transparent dark material (URP Unlit, Surface: Transparent, alpha ~0.7) |
| `_overlayY` | Y position in world space |
| `_quadSize` | Width/height of the quad |

### HighlightOverlayUI (Screen-Space UI)

A full-screen dark UI Image. More reliable for guaranteed full-screen coverage.

Uses `ScreenSpaceCamera` render mode (NOT overlay) so that the highlight camera renders items ON TOP of the dark image. Tutorial panels using `ScreenSpaceOverlay` render above everything.

| Field | Description |
|-------|-------------|
| `_overlayColor` | Color and alpha of the overlay (default: black, 70% opacity) |
| `_camera` | Main camera reference (auto-falls back to Camera.main) |

**Rendering order:**
1. Main camera: 3D scene + dark UI overlay (ScreenSpaceCamera)
2. Highlight camera: highlighted items rendered on top
3. Tutorial panel: ScreenSpaceOverlay canvas, above everything

**Note:** Any gameplay UI using `ScreenSpaceOverlay` will render on top of the dark overlay. Hide those canvases during highlighting (override `ActivateHighlight`/`ClearHighlights` in your subclass).

## Runtime Flow

```
Level starts
→ TutorialController.NotifyLevelPlay(levelIndex)
→ Step triggers after EntryDelay
→ OnTutorialStepEntered fires with stepId
→ HighlightManager looks up HighlightConfig for stepId
→ FindHighlightables() finds matching items
→ Items moved to TutorialHighlight layer
→ Dark overlay shown, highlight camera enabled
→ Input restricted to TutorialHighlight layer
→ Player interacts / taps OK
→ TutorialController.Complete() → step advances
→ OnTutorialStepChanged fires → ClearHighlights()
→ Items restored to original layers, overlay hidden
→ Gameplay resumes
```

## Subclass Extension Points

```csharp
// Add visual effects (e.g., outline shader) when item is highlighted
protected override void OnItemHighlighted(IHighlightable item) { }

// Remove visual effects when unhighlighted
protected override void OnItemUnhighlighted(IHighlightable item) { }

// Custom highlight activation logic
protected override void ActivateHighlight(HighlightConfig config)
{
    base.ActivateHighlight(config);
    // hide gameplay UI, play sound, etc.
}

// Custom clear logic
protected override void ClearHighlights()
{
    bool wasActive = _isHighlightActive;
    base.ClearHighlights();
    if (wasActive) { /* restore gameplay UI, etc. */ }
}
```
