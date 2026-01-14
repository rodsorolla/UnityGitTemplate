# Unity Game Project

## CRITICAL RULES

1. First think through the problem, read the codebase for relevant files, and write a plan to tasks/todo.md.
2. The plan should have a list of todo items that you can check off as you complete them
3. Before you begin working, check in with me and I will verify the plan.
4. Then, begin working on the todo items, marking them as complete as you go.
5. Please every step of the way just give me a high level explanation of what changes you made
6. Make every task and code change you do as simple as possible. We want to avoid making any massive or complex changes. Every change should impact as little code as possible. Everything is about simplicity.
7. Finally, add a review section to the todo.md file with a summary of the changes you made and any other relevant information.
8. DO NOT BE LAZY. NEVER BE LAZY. IF THERE IS A BUG FIND THE ROOT CAUSE AND FIX IT. NO TEMPORARY FIXES. YOU ARE A SENIOR DEVELOPER. NEVER BE LAZY
9. MAKE ALL FIXES AND CODE CHANGES AS SIMPLE AS HUMANLY POSSIBLE. THEY SHOULD ONLY IMPACT NECESSARY CODE RELEVANT TO THE TASK AND NOTHING ELSE. IT SHOULD IMPACT AS LITTLE CODE AS POSSIBLE. YOUR GOAL IS TO NOT INTRODUCE ANY BUGS. IT'S ALL ABOUT SIMPLICITY

## Project Overview
To fill

## Tech Stack
- **Engine**: Unity (C#)
- **Animation**: DOTween
- **Version Control**: Git

## Coding Conventions

### Namespaces
- Core framework: `Sorolla`, `Sorolla.UI`, `Sorolla.Tutorial`

### Naming Conventions
- **Private fields**: `_camelCase` with underscore prefix
- **Public properties/methods**: `PascalCase`
- **Events**: `On` prefix (e.g., `OnSomething`)
- **Interfaces**: `I` prefix (e.g., `ILevelReadOnly`)
- **ScriptableObject assets**: Descriptive names (e.g., "LevelGoal 1.asset")

### Serialization
- Use `[SerializeField]` for private inspector fields
- Use `[Header("Category")]` for organization
- Use `[Min(value)]` for numeric constraints

### Documentation
- XML documentation comments (`///`) for public APIs
- Inline comments for complex logic
- Architecture notes in class headers (see `LevelFlowService.cs` for example)

## Important Patterns & Practices

### Null-Coalescing Assignment
```csharp
_controller ??= GetComponent<Controller>();
```

### Event-Driven Communication
```csharp
public event System.Action<int> OnSomething;
OnSomething?.Invoke(itemValue);
```

### Async/Await for UI
```csharp
await _uiManager.OpenPanelAsync(panelId);
await Task.Delay(ms);
```

### Component Caching
Cache component references in `Start()` or `Awake()` to avoid repeated `GetComponent()` calls.

### Static Animator Hashing
```csharp
private static readonly int EatTrigger = Animator.StringToHash("Eat");
```

## Common Tasks

### Adding a New Tutorial Step
1. Create a new `TutorialStepBase` subclass in `Assets/_Game/Data/Tutorial Steps/`
2. Implement `Initialize()` and completion logic
3. Add the step to the tutorial sequence in the `TutorialController`
4. Create the ScriptableObject asset

### Creating a New UI Panel
1. Create prefab in `Assets/_Game/Prefabs/UI/`
2. Add to `UIRegistry` ScriptableObject
3. Create `UIScreen` component if custom logic needed
4. Use `UIManager.OpenPanelAsync()` to show

### Adding a Level End Reason
1. Add to `LevelEndReason` enum in `Assets/_Game/Scripts/Data/LevelEndReason.cs`
2. Update `LevelTransitionController` logic if needed
3. Add configuration to `EndGamePanelConfig` ScriptableObject:
   - For win scenarios: add to Level Complete Configurations
   - For lose scenarios: add to Game Over Configurations
4. The dynamic panels (`DynamicLevelCompletePanel`, `DynamicGameOverPanel`) will automatically use the new config

## Testing & Debugging

### Debug Tools
- Editor tools in `Assets/_Game/Scripts/Editor/`
- Console logging with component tags: `Debug.Log($"[ComponentName] message")`

## Performance Considerations
- Use object pooling for frequently instantiated objects (VFX, items)
- Cache component references
- Use static readonly for Animator parameter hashes
- Prefer `CompareTag()` over string comparison
- Use async/await for non-blocking operations

### iOS-Specific
- **Disk I/O in gameplay loops causes severe stuttering on iOS** - Never call Save()/Write operations per-frame or per-item. Batch saves at level end or use dirty flags.
- iOS is more sensitive than Android to: GC allocations, disk I/O, and UniTask async overhead
- When debugging iOS-only stutters: check for disk writes, string allocations in hot paths, and persistence calls before investigating animations/visual feedback

## Git Workflow
- Main branch: `main`
- Commit format: `type: description` (e.g., `feat:`, `fix:`, `chore:`)
- Recent focus areas: Tutorial system, collision handling, level flow

## Dependencies
- **DOTween**: Animation and tweening
- Unity Addressables (configured via ProjectSettings)

## Notes for Claude
- **Prefer editing existing files** over creating new ones
- **Read files before suggesting changes** to understand context
- **Follow existing patterns** in the codebase (Service Locator, async/await, etc.)
- **Maintain separation** between Sorolla Core (reusable) and _Game (specific)
- **Document architecture decisions** with inline comments when adding complexity
- **Test changes** in context of the tutorial and level flow systems
- **Avoid over-engineering**: Keep solutions simple and aligned with existing patterns
