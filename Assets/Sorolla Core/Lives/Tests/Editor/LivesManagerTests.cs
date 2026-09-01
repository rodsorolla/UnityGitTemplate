using System;
using NUnit.Framework;
using Sorolla;
using Sorolla.Lives;
using Sorolla.Lives.Tests.Helpers;
using Sorolla.PersistentData;
using UnityEngine;

namespace Sorolla.Lives.Tests
{
    public class LivesManagerTests
    {
        private FakeTimeSource _clock;
        private FakeRemoteConfigProvider _rc;
        private LivesManager _mgr;

        private const string TestSaveFile = "lives_test";

        [SetUp]
        public void Setup()
        {
            ServiceLocator.Reset();
            SaveSystem.Delete(TestSaveFile);

            _clock = new FakeTimeSource(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            _rc = new FakeRemoteConfigProvider();
            _rc.Ints["lives_max"] = 5;
            _rc.Ints["lives_regen_interval_seconds"] = 1800;
            _rc.Ints["lives_system_min_level"] = 1;
            _rc.Ints["lives_booster_default_duration_seconds"] = 1800;
            ServiceLocator.Instance.Register<IRemoteConfigProvider>(_rc);

            _mgr = LivesManager.CreateForTests(_clock, TestSaveFile);
        }

        [TearDown]
        public void Teardown()
        {
            if (_mgr != null)
            {
                UnityEngine.Object.DestroyImmediate(_mgr.gameObject);
                _mgr = null;
            }
            SaveSystem.Delete(TestSaveFile);
            ServiceLocator.Reset();
        }

        [Test]
        public void FreshInstall_StartsAtMax_NoTimer()
        {
            Assert.That(_mgr.Current, Is.EqualTo(5));
            Assert.That(_mgr.IsAtMax, Is.True);
            Assert.That(_mgr.TimeUntilNextLife, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void AddLives_Negative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _mgr.AddLives(-1));
        }

        [Test]
        public void RefillToMax_FromZero_FiresOnCurrentChanged()
        {
            _mgr.SetForTests(current: 0, nextLifeAtUtcIso: _clock.UtcNow.AddSeconds(1800).ToString("o"));
            int? observed = null;
            _mgr.OnCurrentChanged += v => observed = v;

            _mgr.RefillToMax();

            Assert.That(_mgr.Current, Is.EqualTo(5));
            Assert.That(_mgr.IsAtMax, Is.True);
            Assert.That(_mgr.TimeUntilNextLife, Is.EqualTo(TimeSpan.Zero));
            Assert.That(observed, Is.EqualTo(5));
        }

        [Test]
        public void Regen_AdvancesOneLife_AfterIntervalElapses()
        {
            _mgr.SetForTests(current: 3, nextLifeAtUtcIso: _clock.UtcNow.AddSeconds(1800).ToString("o"));

            _clock.Advance(TimeSpan.FromSeconds(1801));
            Assert.That(_mgr.Current, Is.EqualTo(4));
        }

        [Test]
        public void Regen_AdvancesMultipleLives_AfterLongElapse()
        {
            _mgr.SetForTests(current: 0, nextLifeAtUtcIso: _clock.UtcNow.AddSeconds(1800).ToString("o"));

            _clock.Advance(TimeSpan.FromSeconds(1800 * 3 + 1));
            Assert.That(_mgr.Current, Is.EqualTo(3));
        }

        [Test]
        public void Regen_CapsAtMax()
        {
            _mgr.SetForTests(current: 4, nextLifeAtUtcIso: _clock.UtcNow.AddSeconds(1800).ToString("o"));

            _clock.Advance(TimeSpan.FromSeconds(1800 * 100));
            Assert.That(_mgr.Current, Is.EqualTo(5));
            Assert.That(_mgr.TimeUntilNextLife, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void Tick_FiresOnCurrentChangedOncePerTick_NotPerLifeRegenerated()
        {
            _mgr.SetForTests(current: 0, nextLifeAtUtcIso: _clock.UtcNow.AddSeconds(1800).ToString("o"));
            int callCount = 0;
            _mgr.OnCurrentChanged += _ => callCount++;

            _clock.Advance(TimeSpan.FromSeconds(1800 * 3 + 1));
            _ = _mgr.Current; // trigger Tick

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void TimeUntilNextLife_ReflectsRemainingInterval()
        {
            _mgr.SetForTests(current: 3, nextLifeAtUtcIso: _clock.UtcNow.AddSeconds(1800).ToString("o"));
            _clock.Advance(TimeSpan.FromSeconds(600));
            Assert.That(_mgr.TimeUntilNextLife.TotalSeconds, Is.EqualTo(1200).Within(0.01));
        }
    }

    public class LivesManagerLossTests
    {
        private FakeTimeSource _clock;
        private FakeRemoteConfigProvider _rc;
        private LivesManager _mgr;
        private const string TestSaveFile = "lives_test_loss";

        [SetUp]
        public void Setup()
        {
            ServiceLocator.Reset();
            SaveSystem.Delete(TestSaveFile);
            _clock = new FakeTimeSource(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            _rc = new FakeRemoteConfigProvider();
            _rc.Ints["lives_max"] = 5;
            _rc.Ints["lives_regen_interval_seconds"] = 1800;
            _rc.Ints["lives_system_min_level"] = 5;
            ServiceLocator.Instance.Register<IRemoteConfigProvider>(_rc);
            _mgr = LivesManager.CreateForTests(_clock, TestSaveFile);
        }

        [TearDown]
        public void Teardown()
        {
            if (_mgr != null)
            {
                UnityEngine.Object.DestroyImmediate(_mgr.gameObject);
                _mgr = null;
            }
            SaveSystem.Delete(TestSaveFile);
            ServiceLocator.Reset();
        }

        [Test]
        public void Lose_BelowMinLevel_NoOp()
        {
            var deducted = _mgr.TryConsumeLifeForLoss(Sorolla.LevelFlow.LevelEndReason.OutOfLives, 4);
            Assert.That(deducted, Is.False);
            Assert.That(_mgr.Current, Is.EqualTo(5));
        }

        [Test]
        public void Lose_AtOrAboveMinLevel_Deducts()
        {
            var deducted = _mgr.TryConsumeLifeForLoss(Sorolla.LevelFlow.LevelEndReason.OutOfLives, 5);
            Assert.That(deducted, Is.True);
            Assert.That(_mgr.Current, Is.EqualTo(4));
        }

        [Test]
        public void Lose_StartsRegenTimer_FromMax()
        {
            _mgr.TryConsumeLifeForLoss(Sorolla.LevelFlow.LevelEndReason.OutOfLives, 10);
            Assert.That(_mgr.TimeUntilNextLife.TotalSeconds, Is.EqualTo(1800).Within(0.5));
        }

        [Test]
        public void Lose_PlayerQuit_DoesNotDeduct()
        {
            var deducted = _mgr.TryConsumeLifeForLoss(Sorolla.LevelFlow.LevelEndReason.PlayerQuit, 10);
            Assert.That(deducted, Is.False);
            Assert.That(_mgr.Current, Is.EqualTo(5));
        }

        [Test]
        public void Lose_WinReason_DoesNotDeduct()
        {
            var deducted = _mgr.TryConsumeLifeForLoss(Sorolla.LevelFlow.LevelEndReason.AllGoalsComplete, 10);
            Assert.That(deducted, Is.False);
            Assert.That(_mgr.Current, Is.EqualTo(5));
        }

        [Test]
        public void Lose_NoneReason_DoesNotDeduct()
        {
            var deducted = _mgr.TryConsumeLifeForLoss(Sorolla.LevelFlow.LevelEndReason.None, 10);
            Assert.That(deducted, Is.False);
        }

        [Test]
        public void Lose_GameSpecific100_Deducts()
        {
            var deducted = _mgr.TryConsumeLifeForLoss(Sorolla.LevelFlow.LevelEndReason.Custom, 10);
            Assert.That(deducted, Is.True);
        }

        [Test]
        public void Lose_AtZero_StaysAtZero()
        {
            _mgr.SetForTests(current: 0);
            var deducted = _mgr.TryConsumeLifeForLoss(Sorolla.LevelFlow.LevelEndReason.OutOfLives, 10);
            Assert.That(deducted, Is.False);
            Assert.That(_mgr.Current, Is.EqualTo(0));
        }

        [Test]
        public void Lose_MidCycle_DoesNotResetTimer()
        {
            _mgr.SetForTests(current: 4, nextLifeAtUtcIso: _clock.UtcNow.AddSeconds(900).ToString("o"));
            _mgr.TryConsumeLifeForLoss(Sorolla.LevelFlow.LevelEndReason.OutOfLives, 10);
            Assert.That(_mgr.Current, Is.EqualTo(3));
            Assert.That(_mgr.TimeUntilNextLife.TotalSeconds, Is.EqualTo(900).Within(0.5));
        }

        [Test]
        public void Clock_BackwardJump_BeyondTolerance_DetectsAndFreezes()
        {
            // Establish lastSeen
            _ = _mgr.Current;
            _clock.SetUtcNow(_clock.UtcNow.AddSeconds(-30));
            _ = _mgr.Current;
            Assert.That(_mgr.LastClockRollbackDetected, Is.True);
        }

        [Test]
        public void Clock_BackwardJump_WithinTolerance_NotDetected()
        {
            _ = _mgr.Current; // establish
            _clock.SetUtcNow(_clock.UtcNow.AddSeconds(-3)); // within 5s tolerance
            _ = _mgr.Current;
            Assert.That(_mgr.LastClockRollbackDetected, Is.False);
        }
    }

    public class LivesManagerBoosterTests
    {
        private FakeTimeSource _clock;
        private FakeRemoteConfigProvider _rc;
        private LivesManager _mgr;
        private const string TestSaveFile = "lives_test_booster";

        [SetUp]
        public void Setup()
        {
            ServiceLocator.Reset();
            SaveSystem.Delete(TestSaveFile);
            _clock = new FakeTimeSource(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            _rc = new FakeRemoteConfigProvider();
            _rc.Ints["lives_max"] = 5;
            _rc.Ints["lives_regen_interval_seconds"] = 1800;
            _rc.Ints["lives_system_min_level"] = 1;
            ServiceLocator.Instance.Register<IRemoteConfigProvider>(_rc);
            _mgr = LivesManager.CreateForTests(_clock, TestSaveFile);
        }

        [TearDown]
        public void Teardown()
        {
            if (_mgr != null)
            {
                UnityEngine.Object.DestroyImmediate(_mgr.gameObject);
                _mgr = null;
            }
            SaveSystem.Delete(TestSaveFile);
            ServiceLocator.Reset();
        }

        [Test]
        public void Activate_RefillsToMax_AndSetsBoosterActive()
        {
            _mgr.SetForTests(current: 1);
            _mgr.ActivateInfiniteLivesBooster(TimeSpan.FromMinutes(30));
            Assert.That(_mgr.IsBoosterActive, Is.True);
            Assert.That(_mgr.Current, Is.EqualTo(5));
            Assert.That(_mgr.BoosterTimeRemaining.TotalMinutes, Is.EqualTo(30).Within(0.1));
        }

        [Test]
        public void Activate_FiresOnBoosterActivated()
        {
            TimeSpan? received = null;
            _mgr.OnBoosterActivated += d => received = d;
            _mgr.ActivateInfiniteLivesBooster(TimeSpan.FromMinutes(10));
            Assert.That(received.HasValue, Is.True);
            Assert.That(received.Value.TotalMinutes, Is.EqualTo(10).Within(0.1));
        }

        [Test]
        public void MultiPurchase_Extends_AndFiresWithTotalRemaining()
        {
            _mgr.ActivateInfiniteLivesBooster(TimeSpan.FromMinutes(10));
            _clock.Advance(TimeSpan.FromMinutes(5));

            TimeSpan? received = null;
            _mgr.OnBoosterActivated += d => received = d;
            _mgr.ActivateInfiniteLivesBooster(TimeSpan.FromMinutes(30));

            Assert.That(_mgr.BoosterTimeRemaining.TotalMinutes, Is.EqualTo(35).Within(0.1));
            Assert.That(received.Value.TotalMinutes, Is.EqualTo(35).Within(0.1));
        }

        [Test]
        public void Active_LossIsNoOp()
        {
            _mgr.ActivateInfiniteLivesBooster(TimeSpan.FromMinutes(30));
            var deducted = _mgr.TryConsumeLifeForLoss(Sorolla.LevelFlow.LevelEndReason.OutOfLives, 10);
            Assert.That(deducted, Is.False);
            Assert.That(_mgr.Current, Is.EqualTo(5));
        }

        [Test]
        public void Active_RegenIsFrozen()
        {
            _mgr.SetForTests(current: 2, nextLifeAtUtcIso: _clock.UtcNow.AddSeconds(1800).ToString("o"));
            _mgr.ActivateInfiniteLivesBooster(TimeSpan.FromMinutes(60)); // refills to max 5
            Assert.That(_mgr.Current, Is.EqualTo(5));
            _clock.Advance(TimeSpan.FromMinutes(30));
            Assert.That(_mgr.Current, Is.EqualTo(5)); // still max, no regen needed but not negative
        }

        [Test]
        public void Expiry_FiresOnBoosterExpired_Once()
        {
            _mgr.ActivateInfiniteLivesBooster(TimeSpan.FromMinutes(10));
            int count = 0;
            _mgr.OnBoosterExpired += () => count++;

            _clock.Advance(TimeSpan.FromMinutes(11));
            _ = _mgr.Current; // trigger Tick
            _ = _mgr.Current; // re-trigger; should not fire again
            Assert.That(count, Is.EqualTo(1));
            Assert.That(_mgr.IsBoosterActive, Is.False);
        }

        [Test]
        public void ZeroDuration_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _mgr.ActivateInfiniteLivesBooster(TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _mgr.ActivateInfiniteLivesBooster(TimeSpan.FromMinutes(-1)));
        }
    }

    public class LivesManagerLevelFlowTests
    {
        private class FakeFlow : Sorolla.LevelFlow.ILevelFlowManager
        {
#pragma warning disable 0067 // interface events not raised by this test stub
            public event Action<Sorolla.LevelFlow.LevelState> OnStateChanged;
            public event Action<int> OnLevelSetupRequested;
            public event Action OnLevelCleanupRequested;
            public event Action<int> OnLevelStarted;
            public event Action<Sorolla.LevelFlow.LevelEndReason> OnLevelEnded;
            public event Action OnLevelPaused;
            public event Action OnLevelResumed;
            public event Action<int> OnWorldCompleted;
            public event Action<int> OnWorldUnlocked;
            public event Action OnEndPanelDismissed;
#pragma warning restore 0067

            public Sorolla.LevelFlow.LevelState CurrentState => Sorolla.LevelFlow.LevelState.Playing;
            public Sorolla.LevelFlow.LevelEndReason LastEndReason => Sorolla.LevelFlow.LevelEndReason.None;
            public bool IsLevelActive => true;
            public int CurrentLevelIndex { get; set; } = 10;
            public int HighestLevelReached => 10;
            public int TotalLevelCount => 100;
            public bool UsesWorldSystem => false;
            public int CurrentWorldIndex => 1;
            public int HighestWorldReached => 1;
            public int WorldCount => 1;

            public void StartLevel(int levelIndex) { }
            public void RestartLevel() { }
            public void PauseLevel() { }
            public void ResumeLevel() { }
            public void WinLevel(Sorolla.LevelFlow.LevelEndReason reason = Sorolla.LevelFlow.LevelEndReason.AllGoalsComplete) { }
            public void LoseLevel(Sorolla.LevelFlow.LevelEndReason reason)
                => OnLevelEnded?.Invoke(reason);
            public void QuitLevel() => OnLevelEnded?.Invoke(Sorolla.LevelFlow.LevelEndReason.PlayerQuit);
            public void AdvanceToNextLevel() { }
            public void SaveProgress() { }
            public Sorolla.LevelFlow.LevelProgressData GetProgressData() => null;
            public void SetTotalLevelCount(int count) { }
            public int GetActualLevelIndex(int progressiveLevelIndex) => progressiveLevelIndex;
            public int GetLoopIndex(int progressiveLevelIndex)
                => progressiveLevelIndex < 1 ? 0 : (progressiveLevelIndex - 1) / TotalLevelCount;
            public int GetLevelIndexInWorld(int globalLevelIndex) => globalLevelIndex;
            public int GetWorldForLevel(int globalLevelIndex) => 1;
            public int GetFirstLevelOfWorld(int worldIndex) => 1;
            public int GetLastLevelOfWorld(int worldIndex) => TotalLevelCount;
            public bool IsWorldUnlocked(int worldIndex) => true;
            public Sorolla.LevelFlow.WorldConfig GetWorldConfig(int worldIndex) => null;
        }

        [Test]
        public void Subscribed_LoseLevel_Deducts()
        {
            ServiceLocator.Reset();
            SaveSystem.Delete("lives_test_flow");
            var rc = new FakeRemoteConfigProvider();
            rc.Ints["lives_max"] = 5;
            rc.Ints["lives_regen_interval_seconds"] = 1800;
            rc.Ints["lives_system_min_level"] = 1;
            ServiceLocator.Instance.Register<IRemoteConfigProvider>(rc);
            var flow = new FakeFlow { CurrentLevelIndex = 10 };
            ServiceLocator.Instance.Register<Sorolla.LevelFlow.ILevelFlowManager>(flow);

            var clock = new FakeTimeSource(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var mgr = LivesManager.CreateForTests(clock, "lives_test_flow");

            flow.LoseLevel(Sorolla.LevelFlow.LevelEndReason.OutOfLives);
            Assert.That(mgr.Current, Is.EqualTo(4));

            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
            SaveSystem.Delete("lives_test_flow");
            ServiceLocator.Reset();
        }

        [Test]
        public void LivesGate_GuardStartLevel_NoLives_ReturnsTrue()
        {
            ServiceLocator.Reset();
            SaveSystem.Delete("lives_test_gate");
            var rc = new FakeRemoteConfigProvider();
            rc.Ints["lives_max"] = 5;
            rc.Ints["lives_system_min_level"] = 1;
            ServiceLocator.Instance.Register<IRemoteConfigProvider>(rc);
            var clock = new FakeTimeSource(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var mgr = LivesManager.CreateForTests(clock, "lives_test_gate");
            mgr.SetForTests(current: 0);

            Assert.That(LivesGate.IsBlockedAt(10), Is.True);

            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
            SaveSystem.Delete("lives_test_gate");
            ServiceLocator.Reset();
        }

        [Test]
        public void LivesGate_GuardStartLevel_WithLives_ReturnsFalse()
        {
            ServiceLocator.Reset();
            SaveSystem.Delete("lives_test_gate2");
            var rc = new FakeRemoteConfigProvider();
            rc.Ints["lives_max"] = 5;
            rc.Ints["lives_system_min_level"] = 1;
            ServiceLocator.Instance.Register<IRemoteConfigProvider>(rc);
            var clock = new FakeTimeSource(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var mgr = LivesManager.CreateForTests(clock, "lives_test_gate2");
            // current = 5 (fresh)

            Assert.That(LivesGate.IsBlockedAt(10), Is.False);

            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
            SaveSystem.Delete("lives_test_gate2");
            ServiceLocator.Reset();
        }
    }
}
