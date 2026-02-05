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
| `Id` | Unique identifier (for gates/events) |
| `InstructionText` | Message shown to player |
| `CompletionMode` | Manual, Event, or Timed |
| `RequiresGate` | Wait for GateTriggerCollider |
| `EntryDelay` | Delay before step shows |
| `AutoCompleteDelay` | For Timed mode |

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
// Fired when tutorial state changes (level, stepInLevel)
TutorialController.OnTutorialStepChanged += (int level, int step) => { };
```

### Properties

```csharp
tutorialController.IsRunning;        // Tutorial currently active
tutorialController.CurrentLevel;     // Current level index
tutorialController.CurrentStepInLevel; // Current step within level
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
