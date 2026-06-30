# Sorolla.Events

Generic LiveOps event framework. Schedules time-windowed events, tracks per-event progress with a defining "lose-level = no commit" rule, fans out rewards on step thresholds, and supports cutover between back-to-back events.

## Quick start (game side)

1. Add `EventManager` to your Init scene and wire it into `GameManager._gameManagers`.
2. Implement `IEventCatalogProvider`, `IRewardGranter`, `IEventNotificationScheduler` in `_Game/` and register them with `ServiceLocator` before `GameManager.InitializeAsync()`.
3. On level start call `IEventService.BeginRunCollector()`; on the collectible-eaten hook call `collector.Add(amount)`.
4. On level **complete** (and ONLY on complete) call `IEventService.CommitRun(collector)`. On fail/quit, drop the collector — do not call CommitRun.

## Defining rule

Lose or quit a level → in-run collectibles do not count. This is enforced by the absence of a `CommitRun` call. `EventCollector` is in-memory; if you never commit, nothing persists.

## Time

Device UTC + rollback detection. `DefaultAuthoritativeTime` writes `lastSeenUtcIso` to the events save; if a subsequent boot reports a wall-clock more than `events_clock_rollback_grace_seconds` (default 60) before the persisted value, the `OnClockRollbackDetected` event fires. Honor the flag in your IAP/refill logic if you care about cheat resistance.

## Persistence

File `events.json` via `Sorolla.PersistentData.SaveSystem`. Schema in `EventsSaveData.cs`. No cloud sync in v1.

## Analytics

All events fire via `EventTelemetry` → `Palette.TrackDesign`. No vendor SDK is referenced from this module.

## See also

`docs/superpowers/specs/2026-05-15-event-system-design.md` for the full design.
