# Package Audit & Integration

## Summary
Audit all third-party packages, update CLAUDE.md with usage instructions, and migrate existing scripts to use UniTask (instead of System.Threading.Tasks) and ZLinq (instead of System.Linq).

## Packages Inventory

| Package | ID | Status |
|---------|---|--------|
| **UniTask** | `com.cysharp.unitask` | Installed, **NOT used** — all async code uses `System.Threading.Tasks` |
| **ZLinq** | NuGet `ZLinq 1.5.4` | Installed, **NOT used** — all LINQ uses `System.Linq` |
| **NaughtyAttributes** | `com.dbrizov.naughtyattributes` | Installed, used in TutorialStepBase |
| **SceneRefAttribute** | `com.kylewbanks.scenerefattribute` | Installed, unused |
| **Unity-Utils** | `com.gitamend.unityutils` | Installed, unused |
| **ParticleEffectForUGUI** | `com.coffee.ui-particle` | Installed, unused (prefab reference only) |
| **Newtonsoft.Json** | NuGet `13.0.4` | Installed, used in SaveSystem |
| **DOTween** | Plugin folder | Active, used heavily for UI animations |
| **Cinemachine** | `com.unity.cinemachine` 3.1.6 | Installed |
| **Input System** | `com.unity.inputsystem` 1.18.0 | Installed |
| **URP** | `com.unity.render-pipelines.universal` 17.3.0 | Installed |
| **NuGetForUnity** | `com.github-glitchenzo.nugetforunity` | Package manager for NuGet |

## Tasks

### Phase 1: Migrate async Task → UniTask (20 files)
- [x] 1. Update `IUITransition.cs` — interface returns `UniTask` instead of `Task`
- [x] 2. Update `UIScreen.cs` + `UIPanel` — base classes return `UniTask`
- [x] 3. Update `UITransitionBase.cs`, `FadeTransition.cs`, `ScaleTransition.cs`, `SlideTransition.cs` — replace `AsyncWaitForCompletion()` with direct `await` via UniTask DOTween integration
- [x] 4. Update `UIOverlay.cs` — same DOTween migration
- [x] 5. Update `UIManager.cs` — all async methods return `UniTask`, replace `System.Linq` with `ZLinq`
- [x] 6. Update `AlertDialog.cs`, `ConfirmDialog.cs` — UniTask + DOTween await
- [x] 7. Update `CelebrationPanel.cs` — UniTask + DOTween await
- [x] 8. Update `ToastPanel.cs` — UniTask + `UniTask.Delay` instead of `Task.Delay`
- [x] 9. Update `ConfigurablePanel.cs` — UniTask
- [x] 10. Update `ContinuePanel.cs` — UniTask
- [x] 11. Update `SaveSystem.cs` — `UniTask.RunOnThreadPool` instead of `Task.Run`
- [x] 12. Update `LocalFileStorage.cs` + `IStorageProvider.cs` — UniTask
- [x] 13. Update `GameDataServiceBase.cs` + `IGameDataService.cs` + `ExampleGameDataService.cs` — UniTask
- [x] 14. Update `SceneLoader.cs` — UniTask + `UniTask.Yield` + `op.ToUniTask()`
- [x] 15. Update `GameManager.cs` — UniTask + replace `System.Linq` with `ZLinq`
- [x] 16. Update `LevelSessionController.cs` — UniTask
- [x] 17. Update `LevelFlowManager.cs` — `UniTask.Delay` instead of `Task.Delay`
- [x] 18. Update `GameInitializer.cs` — UniTask + `Func<UniTask>` event
- [x] 19. Update `SettingsPanel.cs` — UniTask

### Phase 2: Migrate System.Linq → ZLinq (4 files)
- [x] 20. `GameManager.cs` — `.AsValueEnumerable().Where().ToArray()` → ZLinq
- [x] 21. `UIManager.cs` — `.AsValueEnumerable().OrderByDescending()`, `.FirstOrDefault()`, `.ToList()` → ZLinq
- [x] 22. `TutorialController.cs` — `.AsValueEnumerable().OrderBy()` → ZLinq
- [x] 23. `BackupManager.cs` — `.AsValueEnumerable().OrderByDescending().ToArray()` → ZLinq

### Phase 3: Documentation
- [x] 24. Update `CLAUDE.md` with package info for future sessions
- [x] 25. Update memory with package knowledge
- [x] 26. Update Sorolla Core SKILL.md

---

## Migration Cheat Sheet

### UniTask replacements:
- `using System.Threading.Tasks;` → `using Cysharp.Threading.Tasks;`
- `async Task` → `async UniTask`
- `async Task<T>` → `async UniTask<T>`
- `Task.CompletedTask` → `UniTask.CompletedTask`
- `Task.Delay(ms)` → `UniTask.Delay(ms)` or `UniTask.Delay(TimeSpan)`
- `Task.Yield()` → `UniTask.Yield()`
- `Task.Run(() => ...)` → `UniTask.RunOnThreadPool(() => ...)`
- `TaskCompletionSource<T>` → `UniTaskCompletionSource<T>`
- `.AsyncWaitForCompletion()` → just `await tween` (UniTask DOTween integration provides `GetAwaiter()`)
- `.ContinueWith(...)` → use `try/catch` or `UniTask.Void()`

### ZLinq replacements:
- `using System.Linq;` → `using ZLinq;`
- API is the same (`.Where()`, `.OrderBy()`, `.FirstOrDefault()`, etc.) but zero-allocation

---

## Review

### Changes Made

**21 files migrated from `System.Threading.Tasks` → `Cysharp.Threading.Tasks` (UniTask):**
- IUITransition.cs, UIScreen.cs (UIScreen + UIPanel), UITransitionBase.cs
- FadeTransition.cs, ScaleTransition.cs, SlideTransition.cs, UIOverlay.cs
- UIManager.cs, AlertDialog.cs, ConfirmDialog.cs, CelebrationPanel.cs
- ToastPanel.cs, ConfigurablePanel.cs, ContinuePanel.cs, SettingsPanel.cs
- SaveSystem.cs, LocalFileStorage.cs, IStorageProvider.cs
- IGameDataService.cs, GameDataServiceBase.cs, ExampleGameDataService.cs
- SceneLoader.cs, GameManager.cs, GameInitializer.cs
- LevelSessionController.cs, LevelFlowManager.cs

**4 files migrated from `System.Linq` → `ZLinq`:**
- GameManager.cs, UIManager.cs, TutorialController.cs, BackupManager.cs

**Key changes per pattern:**
- `async Task` → `async UniTask`, `Task.CompletedTask` → `UniTask.CompletedTask`
- `Task.Delay(ms)` → `UniTask.Delay(ms)`
- `Task.Yield()` → `UniTask.Yield()`
- `Task.Run(...)` → `UniTask.RunOnThreadPool(...)`
- `TaskCompletionSource<T>` → `UniTaskCompletionSource<T>`
- `.AsyncWaitForCompletion()` → direct `await` on tween (UniTask DOTween GetAwaiter)
- `.ContinueWith(...)` → `.Forget()` (fire-and-forget in UIManager)
- `TaskCompletionSource` for scene ops → `op.ToUniTask()` (SceneLoader)
- `.Where(...)` → `.AsValueEnumerable().Where(...)` (ZLinq)

**Documentation updated:**
- CLAUDE.md: Tech Stack, Async/Await patterns, LINQ patterns, Dependencies sections
- Sorolla Core SKILL.md: UIPanel and GameDataService code examples
- Memory: Package audit saved for future sessions

### Notes
- Editor-only script `GradientColorEditor.cs` intentionally left with `System.Linq` (no runtime perf concern)
- Could not verify compilation — Unity Editor not running. All changes are mechanical 1:1 replacements.
