---
name: unity-gameplay
description: Expert Unity C# developer for Hungry Snake game. Handles snake mechanics, collision detection, tutorial system debugging, level flow issues, and UI panel problems. Use when debugging Unity scripts, fixing gameplay bugs, adding features to snake systems, or troubleshooting tutorial progression in C# Unity projects.
---

# Unity Gameplay Expert

## Project Context
Hungry Snake is a 3D Unity game using:
- ServiceLocator pattern for dependency injection
- DOTween for animations
- Addressables for asset management
- Separation between reusable framework (Sorolla Core) and game code (_Game)

## Key Systems

### Snake System (`Assets/_Game/Scripts/Gameplay/Snake/`)
- `SnakeController`: Main orchestrator
- `SnakeMovement`/`AISnakeMovement`: Movement implementations
- `SnakeGrowth`: Growth and scaling logic
- `SnakeCollisionHandler`: Food and collision detection

### Level Management (`Assets/_Game/Scripts/Managers/`)
- `LevelManager`: Core level state and lifecycle
- `LevelFlowService`: Level flow operations (UI, rebuilding, transitions)
- `LevelTransitionController`: Orchestrates transitions between levels
- `LevelFactory`: Creates and configures level instances

### Tutorial System
- Framework: `Assets/Sorolla Core/Tutorial/`
- Game-specific: `Assets/_Game/Scripts/Tutorial/`
- `TutorialController`: Main tutorial orchestrator
- `TutorialStepBase`: Base class for tutorial steps

### UI System (`Assets/Sorolla Core/UI/`)
- `UIManager`: Central UI panel management
- `UIRegistry`: ScriptableObject-based UI panel registry
- `UIScreen`: Base class for UI screens
- Async/await pattern for panel transitions

## Common Debugging Areas

### Collision Issues
- Check `BiteWidth.CanSwallow()` logic
- Verify collider sizes and layer masks
- Review `SnakeCollisionHandler` event subscriptions
- Ensure proper tag setup (use `CompareTag()` not string comparison)

### Tutorial Problems
- Verify `TutorialStepBase` completion conditions
- Check event subscriptions in `TutorialController`
- Ensure proper step initialization in `Initialize()` method
- Confirm step order in tutorial sequence

### UI Panels Not Showing
- Verify `UIRegistry` ScriptableObject configuration
- Check panel IDs match between code and registry
- Ensure `UIManager` async operations are properly awaited
- Confirm prefab paths are correct

### Performance Issues
- Check object pooling usage (`FXPoolService`)
- Verify component references are cached (not repeated `GetComponent()`)
- Use static readonly for Animator parameter hashes
- Profile frequently instantiated objects

## Coding Conventions

### Naming
- Private fields: `_camelCase` with underscore prefix
- Public properties/methods: `PascalCase`
- Events: `On` prefix (e.g., `OnFoodEaten`)
- Interfaces: `I` prefix (e.g., `ILevelReadOnly`)

### Serialization
- Use `[SerializeField]` for private inspector fields
- Use `[Header("Category")]` for organization
- Use `[Min(value)]` for numeric constraints

### Namespaces
- Game code: `HungrySnake`, `HungrySnake.UI`
- Core framework: `Sorolla`, `Sorolla.UI`, `Sorolla.Tutorial`

### Best Practices
- Cache component references in `Start()` or `Awake()`
- Use null-coalescing assignment: `_controller ??= GetComponent<Controller>()`
- Event-driven communication: `OnEvent?.Invoke(args)`
- Async/await for UI: `await _uiManager.OpenPanelAsync(panelId)`
- Use object pooling for frequently instantiated objects

## Architecture Patterns

### Service Locator Pattern
```csharp
var levelManager = ServiceLocator.Instance.TryResolve<LevelManager>();
```

### Services vs Controllers vs Managers
- **Services**: Reusable operations, stateless toolkits
- **Controllers**: Event handling, state management
- **Managers**: System-wide state and coordination

### Data-Driven Design
- Use ScriptableObjects for game data (levels, food items, goals)
- Separation of data from logic
- Asset-based configuration

## Instructions

When debugging or adding features:

1. **Read first**: Always read existing files before suggesting changes
2. **Follow patterns**: Use ServiceLocator, async/await for UI, event-driven communication
3. **Check CLAUDE.md**: Reference architecture patterns and common issues section
4. **Prefer editing**: Modify existing files rather than creating new ones
5. **Test context**: Consider impact on tutorial and level flow systems
6. **Avoid over-engineering**: Keep solutions simple and aligned with existing patterns
7. **Maintain separation**: Keep Sorolla Core (reusable) separate from _Game (specific)

## Common Tasks

### Adding a New Food Type
1. Add enum to `FoodType` in `Assets/_Game/Scripts/Data/`
2. Create `GameFoodItem` ScriptableObject
3. Update `GameFood` registry
4. Implement collision logic in `SnakeCollisionHandler`

### Creating a New UI Panel
1. Create prefab in `Assets/_Game/Prefabs/UI/`
2. Add to `UIRegistry` ScriptableObject
3. Create `UIScreen` component if custom logic needed
4. Use `UIManager.OpenPanelAsync()` to show

### Adding a New Tutorial Step
1. Create `TutorialStepBase` subclass in `Assets/_Game/Data/Tutorial Steps/`
2. Implement `Initialize()` and completion logic
3. Add step to tutorial sequence in `TutorialController`
4. Create ScriptableObject asset

### Adding a Level End Reason
1. Add to `LevelEndReason` enum
2. Update `LevelTransitionController` logic
3. Add corresponding UI panel if needed
4. Update `LevelFlowService.GetPanelId()` logic

## Examples

Use this skill when the user says:
- "Snake collision isn't working correctly"
- "Add a new food type"
- "Tutorial step won't complete"
- "Create a new UI panel for end game"
- "Refactor the snake movement system"
- "Fix the level transition flow"
- "Add a new tutorial step"
- "Debug why the UI isn't showing"
- "Optimize snake performance"
- "Implement new snake ability"