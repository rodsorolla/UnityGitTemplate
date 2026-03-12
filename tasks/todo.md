# Sorolla Core QoL Additions

## Tasks
- [x] 1. Create `SorollaTimer.cs` — lightweight timer utility
- [x] 2. Create `SorollaTimerUpdater.cs` — MonoBehaviour auto-updater
- [x] 3. Create `SceneLoader.cs` — async scene loading wrapper
- [x] 4. Refactor `GameInitializer.cs` to use SceneLoader
- [x] 5. Create `SafeAreaHandler.cs` — safe area RectTransform adjuster
- [x] 6. Add `DEBUG_LogAll()` to ServiceLocator
- [x] 7. Add `DeleteAllData()` to SaveSystem
- [x] 8. Update README.md with new features

---

## Review

### Changes Made

**New files (4):**
- `Assets/Sorolla Core/Utils/SorollaTimer.cs` — Static timer with Start/Countdown factory methods, Pause/Resume/Cancel/Restart control, loop support, unscaled time option. No allocations per-timer beyond the instance itself.
- `Assets/Sorolla Core/Utils/SorollaTimerUpdater.cs` — Auto-created MonoBehaviour singleton that drives `SorollaTimer.UpdateAll()` in Update(). DontDestroyOnLoad.
- `Assets/Sorolla Core/Managers/SceneLoader.cs` — Static async wrapper: `LoadSceneAsync`, `LoadSceneAdditiveAsync`, `UnloadSceneAsync`, `ReloadCurrentSceneAsync`. All with optional progress callback.
- `Assets/Sorolla Core/UI/SafeAreaHandler.cs` — RectTransform component with per-edge toggles. Applies on Start() and auto-updates on orientation change.

**Edited files (4):**
- `ServiceLocator.cs` — Added `DEBUG_LogAll()` with `[Conditional]` attributes (Editor/Dev only).
- `SaveSystem.cs` — Added `DeleteAllData(int slot = -1)`. Added `System.IO` using.
- `GameInitializer.cs` — Replaced 10-line manual `LoadSceneAsync` + `TaskCompletionSource` boilerplate with single `await SceneLoader.LoadSceneAdditiveAsync()` call. Behavior unchanged.
- `README.md` — Added SorollaTimer, SceneLoader, SafeAreaHandler sections to Utils. Added `DEBUG_LogAll` and `DeleteAllData` to existing tables.

### Notes
- All new code follows existing conventions: `_camelCase` privates, `Sorolla`/`Sorolla.UI` namespaces, XML doc comments on public API.
- SorollaTimer uses reverse iteration for safe removal during UpdateAll.
- SafeAreaHandler checks orientation change each frame (lightweight — just comparing cached values).
- Could not verify compilation as Unity Editor is not running.
