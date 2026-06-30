using System;
using Sorolla.LevelFlow;
using Sorolla.PersistentData;
using UnityEngine;

namespace Sorolla.Lives
{
    /// <summary>
    /// Lives/hearts system. Place a single instance in the Init scene.
    /// Initialize is idempotent (SorollaManager pattern). Owns LivesData and
    /// implements ILivesService. Saves only at well-defined checkpoints — never
    /// per frame — to keep iOS disk I/O off the gameplay path.
    /// </summary>
    public class LivesManager : SorollaManager, ILivesService
    {
        // ---- Internal state ----
        private const string DefaultSaveFileName = "lives";
        private const double NtpToleranceSeconds = 5.0;

        private LivesData _data;
        private string _saveFileName = DefaultSaveFileName;
        private ITimeSource _clock = SystemTimeSource.Instance;
        private bool _dirty;
        private bool _disableSaves;
        private ILevelFlowManager _flow;

        // ---- ILivesService state surface ----
        public int Current { get { Tick(); return _data.current; } }
        // Cached after Initialize() to avoid hitting Remote Config on every Tick()
        // call (which UI widgets may invoke once per frame). RC values are immutable
        // for the lifetime of a session in practice, so caching is safe.
        private int _cachedMax;
        public int Max => _cachedMax > 0 ? _cachedMax : (_cachedMax = Mathf.Max(1, LivesConfig.MaxLives));
        public bool IsAtMax { get { Tick(); return _data.current >= Max; } }
        public TimeSpan TimeUntilNextLife
        {
            get
            {
                Tick();
                if (_data.current >= Max) return TimeSpan.Zero;
                if (string.IsNullOrEmpty(_data.nextLifeAtUtcIso)) return TimeSpan.Zero;
                var next = ParseUtc(_data.nextLifeAtUtcIso);
                var remaining = next - _clock.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
        public bool IsBoosterActive { get { Tick(); return BoosterActiveAt(_clock.UtcNow); } }
        public TimeSpan BoosterTimeRemaining
        {
            get
            {
                Tick();
                if (string.IsNullOrEmpty(_data.boosterUntilUtcIso)) return TimeSpan.Zero;
                var until = ParseUtc(_data.boosterUntilUtcIso);
                var remaining = until - _clock.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
        public bool LastClockRollbackDetected { get; private set; }
        public bool LastLossConsumedLife { get; private set; }
        // Cached parallel to Max — UI widgets call IsActiveForLevel on every refresh
        // and we don't want a Remote Config hit per call.
        private int _cachedMinLevel = -1;
        public bool IsActiveForLevel(int progressiveLevelIndex)
        {
            if (_cachedMinLevel < 0) _cachedMinLevel = LivesConfig.LivesSystemMinLevel;
            return progressiveLevelIndex >= _cachedMinLevel;
        }

        // ---- Events ----
        public event Action<int> OnCurrentChanged;
        public event Action OnRegenAdvanced;
        public event Action<TimeSpan> OnBoosterActivated;
        public event Action OnBoosterExpired;
        public event Action OnClockRollbackDetected;

        // ---- Initialization ----
        protected override void Initialize()
        {
            LoadOrCreate();
            ServiceLocator.Instance.Register<ILivesService>(this);

            var flow = ServiceLocator.Instance.TryResolve<ILevelFlowManager>();
            if (flow != null)
            {
                _flow = flow;
                _flow.OnLevelEnded += HandleLevelEnded;
            }
        }

        private void OnDestroy()
        {
            if (_flow != null) _flow.OnLevelEnded -= HandleLevelEnded;
        }

        private void HandleLevelEnded(LevelEndReason reason)
        {
            if (_flow == null) return;
            LastLossConsumedLife = TryConsumeLifeForLoss(reason, _flow.CurrentLevelIndex);
        }

        private void LoadOrCreate()
        {
            _data = SaveSystem.Load<LivesData>(_saveFileName);
            // Fresh install / corrupted: SaveSystem returns a default-constructed LivesData
            // with current=0. Detect fresh install via empty lastSeen and start at Max
            // so the player isn't gated immediately.
            if (string.IsNullOrEmpty(_data.lastSeenUtcIso) && _data.current == 0)
            {
                _data.current = Max;
            }
        }

        // ---- Public mutations ----
        // Max acts as a regen ceiling, not a hard cap. AddLives/SetLives can push
        // current above Max (banked lives from rewards, purchases, debug). Regen
        // never advances current above Max. Losses decrement normally; when
        // current crosses below Max for the first time, a fresh regen timer starts.
        public void RefillToMax()
        {
            Tick();
            if (_data.current >= Max) return;
            _data.current = Max;
            _data.nextLifeAtUtcIso = null;
            _dirty = true;
            OnCurrentChanged?.Invoke(_data.current);
            FlushSaveSync();
        }

        public void AddLives(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0) return;
            Tick();
            _data.current += count;
            if (_data.current >= Max) _data.nextLifeAtUtcIso = null;
            _dirty = true;
            OnCurrentChanged?.Invoke(_data.current);
            FlushSaveSync();
        }

        public void SetLives(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            Tick();
            if (_data.current == count) return;
            _data.current = count;
            if (_data.current >= Max)
            {
                _data.nextLifeAtUtcIso = null;
            }
            else if (string.IsNullOrEmpty(_data.nextLifeAtUtcIso))
            {
                _data.nextLifeAtUtcIso = _clock.UtcNow.AddSeconds(LivesConfig.RegenIntervalSeconds).ToString("o");
            }
            _dirty = true;
            OnCurrentChanged?.Invoke(_data.current);
            FlushSaveSync();
        }

        public bool TryConsumeLifeForLoss(LevelEndReason reason, int progressiveLevelIndex)
        {
            if (!IsLossReason(reason)) return false;
            if (!IsActiveForLevel(progressiveLevelIndex)) return false;

            Tick();

            if (BoosterActiveAt(_clock.UtcNow)) return false;
            if (_data.current <= 0) return false;

            // Start a fresh regen timer only when this loss is the one that drops
            // current below Max (Max → Max-1). Banked lives above Max don't trigger
            // regen; mid-cycle losses preserve the in-flight timer.
            int before = _data.current;
            _data.current--;
            if (before >= Max && _data.current < Max)
                _data.nextLifeAtUtcIso = _clock.UtcNow.AddSeconds(LivesConfig.RegenIntervalSeconds).ToString("o");

            _dirty = true;
            OnCurrentChanged?.Invoke(_data.current);
            FlushSaveSync();
            return true;
        }

        // Sorolla.LevelFlow.LevelEndReason convention: 1-19 win, 20-99 lose, 100+ game-specific (treated as loss by default).
        private static bool IsLossReason(LevelEndReason r)
        {
            if (r == LevelEndReason.None) return false;
            if (r == LevelEndReason.PlayerQuit) return false;
            int v = (int)r;
            if (v >= 1 && v < 20) return false;        // win range
            if (v >= 20 && v < 100) return true;       // standard lose
            if (v >= 100) return true;                 // game-specific (loss by default)
            return false;
        }

        public void ActivateInfiniteLivesBooster(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), "Booster duration must be positive.");

            var now = ClockUtcNow();
            DateTime newUntil;
            if (BoosterActiveAt(now) && !string.IsNullOrEmpty(_data.boosterUntilUtcIso))
            {
                // Extend remaining time.
                newUntil = ParseUtc(_data.boosterUntilUtcIso) + duration;
            }
            else
            {
                // Fresh activation — refill to max (but don't reduce banked lives).
                newUntil = now + duration;
                if (_data.current < Max)
                {
                    _data.current = Max;
                    _data.nextLifeAtUtcIso = null;
                    OnCurrentChanged?.Invoke(_data.current);
                }
            }

            _data.boosterUntilUtcIso = newUntil.ToString("o");
            _dirty = true;
            FlushSaveSync();

            var totalRemaining = newUntil - now;
            OnBoosterActivated?.Invoke(totalRemaining > TimeSpan.Zero ? totalRemaining : TimeSpan.Zero);
        }

        // ---- Tick / regen ----
        private void Tick()
        {
            var now = ClockUtcNow();
            HandleBoosterExpiry(now);
            if (BoosterActiveAt(now)) return;
            if (_data.current >= Max)
            {
                if (!string.IsNullOrEmpty(_data.nextLifeAtUtcIso))
                {
                    _data.nextLifeAtUtcIso = null;
                    _dirty = true;
                }
                return;
            }
            if (string.IsNullOrEmpty(_data.nextLifeAtUtcIso))
            {
                _data.nextLifeAtUtcIso = now.AddSeconds(LivesConfig.RegenIntervalSeconds).ToString("o");
                _dirty = true;
                return;
            }

            var next = ParseUtc(_data.nextLifeAtUtcIso);
            int before = _data.current;
            while (_data.current < Max && now >= next)
            {
                _data.current++;
                next = next.AddSeconds(LivesConfig.RegenIntervalSeconds);
            }
            if (_data.current >= Max)
                _data.nextLifeAtUtcIso = null;
            else
                _data.nextLifeAtUtcIso = next.ToString("o");

            if (_data.current != before)
            {
                _dirty = true;
                OnRegenAdvanced?.Invoke();
                OnCurrentChanged?.Invoke(_data.current);
                FlushSaveSync();
            }
        }

        private bool BoosterActiveAt(DateTime now)
        {
            if (string.IsNullOrEmpty(_data.boosterUntilUtcIso)) return false;
            return now < ParseUtc(_data.boosterUntilUtcIso);
        }

        private void HandleBoosterExpiry(DateTime now)
        {
            if (string.IsNullOrEmpty(_data.boosterUntilUtcIso)) return;
            var until = ParseUtc(_data.boosterUntilUtcIso);
            if (now < until) return;

            _data.boosterUntilUtcIso = null;
            _dirty = true;
            OnBoosterExpired?.Invoke();
        }

        // ---- Clock with rollback detection ----
        private DateTime ClockUtcNow()
        {
            var systemNow = _clock.UtcNow;
            if (!string.IsNullOrEmpty(_data.lastSeenUtcIso))
            {
                var lastSeen = ParseUtc(_data.lastSeenUtcIso);
                if (systemNow < lastSeen.AddSeconds(-NtpToleranceSeconds))
                {
                    if (!LastClockRollbackDetected)
                    {
                        LastClockRollbackDetected = true;
                        OnClockRollbackDetected?.Invoke();
                    }
                    return lastSeen;
                }
            }
            _data.lastSeenUtcIso = systemNow.ToString("o");
            return systemNow;
        }

        private static DateTime ParseUtc(string iso)
            => DateTime.Parse(iso, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

        // ---- Save cadence ----
        // Saves run synchronously at discrete points (mutations, level-end, pause/quit),
        // never per-frame. Fire-and-forget SaveAsync to a single file races its own writes
        // on the shared temp file and loses data on quit — the same issue EventManager fixed.
        private void FlushSaveSync()
        {
            if (!_dirty) return;
            _dirty = false;
            if (_disableSaves) return;
            SaveSystem.Save(_data, _saveFileName);
        }

        /// <summary>
        /// Save unconditionally (bypassing the _dirty flag) at app lifecycle boundaries
        /// so in-memory updates to lastSeenUtcIso are persisted even when no counted
        /// state changed this session. Required for backward-clock-jump cheat detection
        /// to remain reliable across force-kill.
        /// </summary>
        private void SaveNowSync()
        {
            _dirty = false;
            if (_disableSaves) return;
            SaveSystem.Save(_data, _saveFileName);
        }

        /// <summary>
        /// Synchronously persist lives state to disk now. Used by the purchase grant checkpoint
        /// so a lives reward is durably saved before the purchase is acknowledged to the store.
        /// </summary>
        public void FlushNow() => SaveNowSync();

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveNowSync();
        }

        private void OnApplicationQuit() => SaveNowSync();

        // ---- Test seam ----
        public static LivesManager CreateForTests(ITimeSource clock, string saveFileName)
        {
            var go = new GameObject("LivesManager_Test");
            var mgr = go.AddComponent<LivesManager>();
            mgr._clock = clock;
            mgr._saveFileName = saveFileName;
            // Disable disk persistence in tests. Save/Load round-trip is covered by
            // LivesDataTests against SaveSystem directly; LivesManager tests assert
            // state and event behavior in-memory. Allowing async saves to fly during
            // tests races with TearDown's Delete and produces sharing-violation logs.
            mgr._disableSaves = true;
            mgr.Init();
            return mgr;
        }

        public void SetForTests(int current = -1, string nextLifeAtUtcIso = null,
            string boosterUntilUtcIso = null, string lastSeenUtcIso = null)
        {
            if (current >= 0) _data.current = current;
            if (nextLifeAtUtcIso != null) _data.nextLifeAtUtcIso = nextLifeAtUtcIso;
            if (boosterUntilUtcIso != null) _data.boosterUntilUtcIso = boosterUntilUtcIso;
            if (lastSeenUtcIso != null) _data.lastSeenUtcIso = lastSeenUtcIso;
        }
    }
}
