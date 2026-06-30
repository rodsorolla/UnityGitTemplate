using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sorolla.PersistentData;
using UnityEngine;

namespace Sorolla.Events
{
    /// <summary>
    /// Events orchestrator. Place a single instance in the Init scene and add it
    /// to <see cref="GameManager._gameManagers"/>. Owns <see cref="EventsSaveData"/>
    /// and registers <see cref="IEventService"/>. Saves only at well-defined
    /// checkpoints (pause, quit, commit, claim, cutover) — never per frame.
    /// </summary>
    public class EventManager : SorollaManager, IEventService
    {
        private const string DefaultSaveFileName = "events";

        [SerializeField] private string _saveFileNameOverride;

        // ---- State ----
        private EventsSaveData _data;
        private IAuthoritativeTime _clock;
        private IEventCatalogProvider _catalog;
        private IRewardGranter _granter;
        private IEventNotificationScheduler _notifier;
        private EventDefinition _active;
        private EventCollector _runCollector;
        private bool _dirty;
        private bool _disableSaves;

        private string SaveFileName =>
            string.IsNullOrEmpty(_saveFileNameOverride) ? DefaultSaveFileName : _saveFileNameOverride;

        // ---- IEventService surface (filled in later tasks) ----
        public EventDefinition ActiveEvent => _active;

        public event Action<EventDefinition> OnActiveEventStarted;
        public event Action<EventDefinition, EventEndReason> OnActiveEventEnded;
        public event Action<string, int, int> OnProgressChanged;
        public event Action<string, int> OnStepClaimed;
        public event Action<string> OnGrandPrizeClaimed;
        public event Action OnClockRollbackDetected;

        public EventState GetState(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || _catalog == null) return EventState.Inactive;
            var catalog = _catalog.GetScheduledEvents();
            EventDefinition def = null;
            for (int i = 0; i < catalog.Count; i++)
                if (catalog[i] != null && catalog[i].EventId == eventId) { def = catalog[i]; break; }
            if (def == null) return EventState.Inactive;

            if (!EventScheduler.IsActiveOn(def, _clock.UtcNow)) return EventState.Inactive;

            var progress = _data.Find(eventId);
            if (progress != null && IsAllStepsClaimed(def, progress))
                return EventState.GrandPrizeReady;

            return EventState.Active;
        }

        public EventInstanceProgress GetProgress(string eventId) => _data?.Find(eventId);

        public void SetProgress(string eventId, int newProgress)
        {
            if (string.IsNullOrEmpty(eventId) || _data == null) return;
            var row = _data.FindOrCreate(eventId, _clock.UtcNow.ToString("o"));
            int previous = row.progress;
            if (previous == newProgress) return;
            row.progress = newProgress;
            _dirty = true;
            OnProgressChanged?.Invoke(eventId, newProgress, newProgress - previous);
        }

        public bool IsUnlocked(int progressiveLevelIndex)
        {
            if (_active == null) return false;
            var threshold = _active.UnlockLevel < 0 ? EventConfigKeys.DefaultUnlockLevelValue : _active.UnlockLevel;
            return progressiveLevelIndex >= threshold;
        }

        public TimeSpan TimeUntilActiveEnds =>
            _active == null ? TimeSpan.Zero : EventScheduler.TimeUntilEnd(_active, _clock.UtcNow);

        public TimeSpan TimeUntilNextEventStarts =>
            _catalog == null ? TimeSpan.Zero
                             : EventScheduler.TimeUntilNextStart(_catalog.GetScheduledEvents(), _clock.UtcNow);
        public bool LastClockRollbackDetected => _clock?.RollbackDetectedThisSession ?? false;

        private int _pendingHomeAnimationDelta;
        public int PendingHomeAnimationDelta => _pendingHomeAnimationDelta;
        public void ConsumePendingHomeAnimation() => _pendingHomeAnimationDelta = 0;

        public EventCollector BeginRunCollector()
        {
            Tick();
            if (_active == null) return null;
            _runCollector = new EventCollector(_active.EventId);
            return _runCollector;
        }

        public void CommitRun(EventCollector collector, EventCommitContext ctx = null)
        {
            if (collector == null) return;
            if (_active == null || collector.EventId != _active.EventId) return;
            if (collector.CollectedThisRun <= 0) return;

            var p = _data.FindOrCreate(_active.EventId, _clock.UtcNow.ToString("o"));
            int before = p.progress;
            p.progress = before + collector.CollectedThisRun;
            _dirty = true;
            EventTelemetry.TrackProgress(_active.EventId, collector.CollectedThisRun, p.progress);
            _pendingHomeAnimationDelta += collector.CollectedThisRun;
            OnProgressChanged?.Invoke(_active.EventId, p.progress, collector.CollectedThisRun);

            // Auto-claim newly-crossed steps. EventStep.Threshold is a per-step
            // delta; the cumulative checkpoint at which step i is claimable is
            // the sum of all step deltas up to and including i.
            var steps = _active.Steps;
            if (steps != null)
            {
                int cumulative = 0;
                for (int i = 0; i < steps.Count && i < 64; i++)
                {
                    var step = steps[i];
                    cumulative += step.Threshold;
                    ulong bit = 1UL << i;
                    if ((p.claimedStepBitset & bit) != 0) continue;       // already claimed
                    if (p.progress < cumulative) continue;                // not crossed yet
                    p.claimedStepBitset |= bit;
                    EventTelemetry.TrackStepClaimed(_active.EventId, i, cumulative);
                    if (_granter != null && step.Rewards != null)
                    {
                        foreach (var reward in step.Rewards)
                            _granter.Grant(reward, new RewardGrantContext
                            {
                                EventId = _active.EventId, StepIndex = i, IsGrandPrize = false
                            }).Forget();
                    }
                    OnStepClaimed?.Invoke(_active.EventId, i);
                }
            }

            // Auto-grant Grand Prize when all steps claimed.
            if (IsAllStepsClaimed(_active, p) && !p.grandPrizeClaimed)
            {
                p.grandPrizeClaimed = true;
                if (_granter != null && _active.GrandPrize != null)
                    _granter.Grant(_active.GrandPrize, new RewardGrantContext
                    {
                        EventId = _active.EventId, StepIndex = -1, IsGrandPrize = true
                    }).Forget();
                EventTelemetry.TrackGrandPrizeClaimed(_active.EventId);
                OnGrandPrizeClaimed?.Invoke(_active.EventId);
            }

            _runCollector = null;
            FlushSaveSync();
        }
        public bool TryClaimStep(string eventId, int stepIndex) => false; // v1: reserved
        public bool TryClaimGrandPrize(string eventId) => false; // v1: reserved

        // ---- Private helpers ----
        private static bool TryParseUtc(string iso, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrEmpty(iso)) return false;
            if (!DateTime.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var parsed)) return false;
            utc = parsed;
            return utc.Kind == DateTimeKind.Utc;
        }

        private static bool IsAllStepsClaimed(EventDefinition def, EventInstanceProgress p)
        {
            if (def?.Steps == null || def.Steps.Count == 0) return false;
            ulong mask = def.Steps.Count >= 64 ? ulong.MaxValue : ((1UL << def.Steps.Count) - 1);
            return (p.claimedStepBitset & mask) == mask;
        }

        private static int Popcount(ulong v)
        {
            int c = 0;
            while (v != 0) { v &= v - 1; c++; }
            return c;
        }

        private void Tick()
        {
            if (_catalog == null) return;
            _data.lastSeenUtcIso = _clock.UtcNow.ToString("o");
            _dirty = true;

            if (!EventConfigKeys.Enabled)
            {
                SwapActive(null, EventEndReason.KillSwitchDisabled);
                return;
            }
            var next = EventScheduler.GetActive(_catalog.GetScheduledEvents(), _clock.UtcNow);
            if (ReferenceEquals(next, _active)) return;
            SwapActive(next, _active == null
                ? EventEndReason.WindowExpired
                : (next == null ? EventEndReason.WindowExpired : EventEndReason.Replaced));
        }

        private void SwapActive(EventDefinition next, EventEndReason endReason)
        {
            var prev = _active;
            if (prev != null)
            {
                var prevProgress = _data.Find(prev.EventId);
                int finalProgress = prevProgress?.progress ?? 0;
                int stepsClaimed = prevProgress == null ? 0 : Popcount(prevProgress.claimedStepBitset);
                bool grandPrizeClaimed = prevProgress?.grandPrizeClaimed ?? false;
                _data.Remove(prev.EventId);
                _dirty = true;
                EventTelemetry.TrackEventEnded(prev.EventId, endReason, finalProgress, stepsClaimed, grandPrizeClaimed);
                OnActiveEventEnded?.Invoke(prev, endReason);
            }
            _active = next;
            if (next != null)
            {
                _data.FindOrCreate(next.EventId, _clock.UtcNow.ToString("o"));
                _dirty = true;
                EventTelemetry.TrackEventStarted(next.EventId);
                OnActiveEventStarted?.Invoke(next);
            }
            FlushSaveSync();
        }

        private void OnCatalogChanged() => Tick();

        private void OnClockRollbackForward() => OnClockRollbackDetected?.Invoke();

        private void ApplyRollbackObservation()
        {
            if (string.IsNullOrEmpty(_data.lastSeenUtcIso)) return;
            if (!TryParseUtc(_data.lastSeenUtcIso, out var lastSeen)) return;
            var grace = TimeSpan.FromSeconds(EventConfigKeys.ClockRollbackGraceSeconds);
            _clock.ObservePersisted(lastSeen, grace);
        }

        // ---- Lifecycle ----
        protected override void Initialize()
        {
            EnsureDependencies();
            LoadOrCreate();
            if (_catalog != null) _catalog.OnCatalogChanged += OnCatalogChanged;
            if (_clock != null) _clock.OnRollbackDetected += OnClockRollbackForward;
            ApplyRollbackObservation();
            Tick();
            ServiceLocator.Instance.Register<IEventService>(this);
        }

        private void OnDestroy()
        {
            if (_catalog != null) _catalog.OnCatalogChanged -= OnCatalogChanged;
            if (_clock != null) _clock.OnRollbackDetected -= OnClockRollbackForward;
        }

        private void EnsureDependencies()
        {
            _clock ??= ServiceLocator.Instance.TryResolve<IAuthoritativeTime>()
                       ?? new DefaultAuthoritativeTime();
            _catalog ??= ServiceLocator.Instance.TryResolve<IEventCatalogProvider>();
            _granter ??= ServiceLocator.Instance.TryResolve<IRewardGranter>();
            _notifier ??= ServiceLocator.Instance.TryResolve<IEventNotificationScheduler>();

            if (_catalog == null)
                Debug.LogWarning("[EventManager] No IEventCatalogProvider registered; module will idle.");
            if (_granter == null)
                Debug.LogWarning("[EventManager] No IRewardGranter registered; reward grants will be skipped.");
        }

        private void LoadOrCreate()
        {
            _data = EventsSaveMigrator.Migrate(SaveSystem.Load<EventsSaveData>(SaveFileName));
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) FlushSaveSync();
        }

        private void OnApplicationQuit() => FlushSaveSync();

        /// <summary>
        /// Forces a synchronous persist of pending changes. Used at explicit save points
        /// and by tests (EditMode cannot deliver OnApplicationQuit via SendMessage).
        /// </summary>
        public void FlushSave() => FlushSaveSync();

        // EventManager persists at discrete points (level-end CommitRun, event swap,
        // pause/quit) — never per-frame. Saves are synchronous so concurrent writes can't
        // collide on the shared temp file and data is durable before the next read.
        // (A prior fire-and-forget SaveAsync pattern raced its own writes on <file>.json.tmp
        // — "Sharing violation" / "Could not find file" — and lost data on quit.)
        private void FlushSaveSync()
        {
            if (!_dirty) return;
            _dirty = false;
            if (_disableSaves) return;
            SaveSystem.Save(_data, SaveFileName);
        }

        // ---- Test seam ----
        public static EventManager CreateForTests(IAuthoritativeTime clock, IEventCatalogProvider catalog,
            IRewardGranter granter, IEventNotificationScheduler notifier, string saveFileName,
            bool disableSaves = true)
        {
            var go = new GameObject("EventManager_Test");
            var mgr = go.AddComponent<EventManager>();
            mgr._clock = clock;
            mgr._catalog = catalog;
            mgr._granter = granter;
            mgr._notifier = notifier;
            mgr._saveFileNameOverride = saveFileName;
            mgr._disableSaves = disableSaves;
            mgr.Init();
            return mgr;
        }
    }
}
