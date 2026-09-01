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
10. ONE THING AT A TIME. When a fix is confirmed working, suggest committing before starting the next task. If the user pivots to a different bug or feature, flag any uncommitted work from the previous task first.

## Project Overview
To fill

## Tech Stack
- **Engine**: Unity (C#)
- **Async**: UniTask (`Cysharp.Threading.Tasks`) — all async code uses UniTask, NOT System.Threading.Tasks
- **LINQ**: ZLinq (`using ZLinq;`) — zero-allocation LINQ, same API as System.Linq. Use `.AsValueEnumerable()` before LINQ chains on collections.
- **Animation**: DOTween (use `.AsyncWaitForCompletion()` to await tweens — UniTask can await the returned Task)
- **Inspector**: NaughtyAttributes (`[ShowIf]`, `[BoxGroup]`, `[Button]`, `[Required]`, etc.)
- **Serialization**: Newtonsoft.Json (via NuGet)
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

### Async/Await (UniTask)
```csharp
using Cysharp.Threading.Tasks;

await _uiManager.OpenPanelAsync(panelId);   // Returns UniTask
await UniTask.Delay(ms);                     // NOT Task.Delay
await UniTask.Yield();                       // NOT Task.Yield
await UniTask.RunOnThreadPool(() => ...);    // NOT Task.Run
await tween.SetEase(Ease.OutBack)
    .AsyncWaitForCompletion();               // DOTween await (returns Task, UniTask awaits it)
```
- All async methods return `UniTask` / `UniTask<T>`, never `Task` / `Task<T>`
- Use `UniTaskCompletionSource<T>` instead of `TaskCompletionSource<T>`
- Fire-and-forget: use `.Forget()` instead of `.ContinueWith()`

### LINQ (ZLinq)
```csharp
using ZLinq;

// Use .AsValueEnumerable() before LINQ chains on collections
var items = list.AsValueEnumerable().Where(x => x.IsValid).ToArray();
var first = array.AsValueEnumerable().FirstOrDefault(x => x != null);
```
- Same API as System.Linq but zero-allocation
- Editor-only scripts can still use System.Linq (no runtime perf concern)

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
1. Core enum lives at `Packages/com.sorolla.core/LevelFlow/LevelEndReason.cs` with values 0–99 reserved for Sorolla Core. Game-specific reasons must use values **>= 100** — either extend the enum in a partial file under `_Game/` or define a parallel game enum that maps onto `LevelEndReason.Custom`.
2. Update `LevelTransitionController` logic if needed
3. Add configuration to `EndGamePanelConfig` ScriptableObject:
   - For win scenarios: add to Level Complete Configurations
   - For lose scenarios: add to Game Over Configurations
4. The dynamic panels (`DynamicLevelCompletePanel`, `DynamicGameOverPanel`) will automatically use the new config

### Adding an Async-Initialized Manager
1. Implement `IAsyncInitializable` (namespace `Sorolla`) on a MonoBehaviour added to `GameManager._gameManagers`
2. `GameManager` will await `InitializeAsync(ct)` in array order. The async path is exclusive of `SorollaManager.Init()` — pick one
3. Use this for boot work that must complete before other systems run: remote-config fetch (LiveConfig), addressables warm-up, file I/O

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

## Starting a new project

This repo is the seed for every new Sorolla game. Use the script — it does the clone, the
rename, the GitHub repo and a cold-import verification in one pass:

```bash
./scripts/new-project.sh puzzlequest "Puzzle Quest"
# -> ~/Documents/Git/puzzlequest, private repo sorolla-studio/puzzlequest
# bundle id defaults to com.sorolla.<repo-name>; pass a third arg to override
```

What it does, in order:

1. `git clone --recurse-submodules` the template, so `Packages/com.sorolla.core` comes with it
2. Checks out `main` inside the submodule (a fresh submodule is on a detached HEAD) and sets
   `submodule.recurse true`
3. Rewrites `productName` and `applicationIdentifier` (Android, Standalone, iPhone — all three
   are still Unity's URP-blank defaults in this template), the `CLAUDE.md` title, and resets
   `tasks/todo.md`
4. Creates the private repo under `sorolla-studio`, repoints `origin`, adds a `template` remote
5. Commits, pushes, then runs a batchmode import and reports compile errors, GUID conflicts and
   the Sorolla assembly count (expect `0 / 0 / 29`); exits non-zero if anything is off

It refuses to start if the directory exists, the GitHub repo exists, the name is not lowercase,
or `gh` is not authenticated. Nothing is created until every check passes.

### The `template` remote

New projects get a `template` remote pointing back here. `git merge template/main` pulls template
improvements *and* whatever `Assets/_Game` content this repo holds, which conflicts once a game
diverges — cherry-pick specific commits instead once you have real game code. Core changes never
come through this path; they come through the submodule.

## Sorolla Core (git submodule)

`Packages/com.sorolla.core` is **not part of this repo** — it is a submodule pointing at
[`sorolla-studio/sorolla-core`](https://github.com/sorolla-studio/sorolla-core). This repo only
records *which commit* of Core to use, so Core never changes under you without an explicit bump.

Dependency direction is one-way and must stay that way:
`sorolla-palette (com.sorolla.sdk)` ← `sorolla-core` ← this game.
Core may use Palette. **Palette must never reference Core.**

### Cloning

```bash
git clone --recurse-submodules <repo-url>
```

Forgot the flag? `Packages/com.sorolla.core` will be empty and Unity will report hundreds of
missing scripts. Fix with `git submodule update --init --recursive` — nothing is broken.

### Changing Core while working on a game

Core is editable in place. It needs **two commits, in this order**:

```bash
# 1. the actual change, inside the submodule
cd Packages/com.sorolla.core
git checkout main                 # only if detached
git add -A && git commit -m "feat(lives): add streak bonus"
git push                          # <- publishes it for every project

# 2. the moved pointer, in the game repo
cd ../..
git add Packages/com.sorolla.core
git commit -m "chore: bump sorolla-core"
git push
```

Skipping step 1 pushes a pointer to a commit nobody else can fetch — the classic submodule
failure. `git config submodule.recurse true` (already set locally) makes `git push` push the
submodule too, as a safety net.

### Pulling Core changes made elsewhere

```bash
git submodule update --remote Packages/com.sorolla.core
git add Packages/com.sorolla.core && git commit -m "chore: bump sorolla-core"
```

### Rules

- Game-specific code goes in `Assets/_Game/`, never in `Packages/com.sorolla.core/`.
- A change belongs in Core only if a *different* game would want it too.
- Core's tests stay visible via `"testables": ["com.sorolla.core"]` in `Packages/manifest.json`.
- UPM does not resolve git URLs declared inside a package, so this project's `manifest.json`
  must keep listing Core's git dependencies (palette, UniTask, NaughtyAttributes, …) itself.

## Dependencies

### Core (always use these)
- **UniTask** (`com.cysharp.unitask`) — async/await. Use instead of System.Threading.Tasks
- **ZLinq** (NuGet `1.5.4`) — zero-alloc LINQ. Use instead of System.Linq in runtime code
- **DOTween** — animation/tweening. Await tweens directly via UniTask integration
- **NaughtyAttributes** (`com.dbrizov.naughtyattributes`) — inspector enhancements
- **Newtonsoft.Json** (NuGet `13.0.4`) — JSON serialization for SaveSystem

### Available (use when needed)
- **SceneRefAttribute** (`com.kylewbanks.scenerefattribute`) — auto-resolve component refs in editor
- **Unity-Utils** (`com.gitamend.unityutils`) — extension methods (Transform, Color, etc.)
- **ParticleEffectForUGUI** (`com.coffee.ui-particle`) — particle systems in UGUI canvas
- **Cinemachine** 3.1.6 — camera system
- **Input System** 1.18.0 — new input system
- **URP** 17.3.0 — rendering pipeline
- Unity Addressables (configured via ProjectSettings)

## Notes for Claude
- **Prefer editing existing files** over creating new ones
- **Read files before suggesting changes** to understand context
- **Follow existing patterns** in the codebase (Service Locator, async/await, etc.)
- **Maintain separation** between Sorolla Core (reusable) and _Game (specific)
- **Document architecture decisions** with inline comments when adding complexity
- **Test changes** in context of the tutorial and level flow systems
- **Avoid over-engineering**: Keep solutions simple and aligned with existing patterns
