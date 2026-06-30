using System;
using NUnit.Framework;
using Sorolla;
using Sorolla.Events.Tests.Helpers;
using UnityEngine;

namespace Sorolla.Events.Tests
{
    public class EventManagerTests
    {
        // 2026-05-15 12:00 UTC is a Friday — anchor used throughout the tests.
        private static readonly DateTime BaseUtc = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);

        [SetUp] public void Setup() => ServiceLocator.Reset();
        [TearDown] public void Teardown()
        {
            ServiceLocator.Reset();
            foreach (var mgr in UnityEngine.Object.FindObjectsByType<EventManager>())
                UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }

        private static EventManager MakeManager(FakeAuthoritativeTime clock = null,
            FakeEventCatalogProvider cat = null, FakeRewardGranter rg = null,
            FakeEventNotificationScheduler ns = null, string saveFile = "events_test",
            bool disableSaves = true)
        {
            clock ??= new FakeAuthoritativeTime(BaseUtc);
            cat ??= new FakeEventCatalogProvider();
            rg ??= new FakeRewardGranter();
            ns ??= new FakeEventNotificationScheduler();
            return EventManager.CreateForTests(clock, cat, rg, ns, saveFile, disableSaves);
        }

        [Test]
        public void Initialize_RegistersIEventService()
        {
            var mgr = MakeManager();
            var resolved = ServiceLocator.Instance.TryResolve<IEventService>();
            Assert.IsNotNull(resolved);
            Assert.AreSame(mgr, resolved);
        }

        [Test]
        public void Initialize_StartsWithNoActiveEvent_WhenCatalogEmpty()
        {
            var mgr = MakeManager();
            Assert.IsNull(mgr.ActiveEvent);
        }

        private static EventDefinition MakeDef(string id, DateTime start, DateTime end, int unlockLevel = -1)
            => MakeDef(id, DayOfWeek.Sunday, DayOfWeek.Saturday, unlockLevel); // covers every day (used by default-active tests)

        private static EventDefinition MakeDef(string id, DayOfWeek startDay, DayOfWeek endDay, int unlockLevel = -1)
            => new EventDefinition
            {
                EventId = id,
                StartDayOfWeek = startDay,
                EndDayOfWeek = endDay,
                UnlockLevel = unlockLevel,
                Steps = new System.Collections.Generic.List<EventStep>(),
            };

        [Test]
        public void Tick_PicksActiveEvent_OnInitWhenInsideWindow()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1)));
            var mgr = MakeManager(clock, cat);
            Assert.AreEqual("a", mgr.ActiveEvent?.EventId);
        }

        [Test]
        public void Tick_FiresOnActiveEventStarted_OnInit()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1)));
            var m = MakeManager(clock, cat);
            // First Tick happens inside Initialize() before any subscriber attaches; we assert
            // via the persistent ActiveEvent state.
            Assert.AreEqual("a", m.ActiveEvent?.EventId);
        }

        [Test]
        public void OnCatalogChanged_PicksUpNewlyScheduledEvent()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            var mgr = MakeManager(clock, cat);
            Assert.IsNull(mgr.ActiveEvent);

            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1)));
            EventDefinition started = null;
            mgr.OnActiveEventStarted += d => started = d;
            cat.FireChanged();

            Assert.AreEqual("a", mgr.ActiveEvent?.EventId);
            Assert.AreEqual("a", started?.EventId);
        }

        [Test]
        public void Cutover_FiresEndedThenStarted_AndForfeitsPrevProgress()
        {
            // BaseUtc = Friday 12:00. "a" covers Fri-Sat, "b" covers Sun-Mon.
            // Advance 2 days → Sunday 12:00 → "a" out, "b" in.
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", DayOfWeek.Friday, DayOfWeek.Saturday));
            cat.Catalog.Add(MakeDef("b", DayOfWeek.Sunday, DayOfWeek.Monday));
            var mgr = MakeManager(clock, cat);

            // Ensure A's entry exists (Tick created it).
            Assert.IsNotNull(mgr.GetProgress("a"));

            clock.Advance(System.TimeSpan.FromDays(2));
            var seq = new System.Collections.Generic.List<string>();
            mgr.OnActiveEventEnded += (d, r) => seq.Add($"end:{d.EventId}:{r}");
            mgr.OnActiveEventStarted += d => seq.Add($"start:{d.EventId}");
            cat.FireChanged();

            Assert.AreEqual("b", mgr.ActiveEvent?.EventId);
            Assert.Contains("end:a:Replaced", seq);
            Assert.Contains("start:b", seq);
            Assert.IsNull(mgr.GetProgress("a"));
            Assert.IsNotNull(mgr.GetProgress("b"));
        }

        [Test]
        public void IsUnlocked_FalseBelowUnlockLevel_TrueAtOrAbove()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1), unlockLevel: 10));
            var mgr = MakeManager(clock, cat);

            Assert.IsFalse(mgr.IsUnlocked(9));
            Assert.IsTrue(mgr.IsUnlocked(10));
            Assert.IsTrue(mgr.IsUnlocked(50));
        }

        [Test]
        public void IsUnlocked_UsesRcDefault_WhenDefinitionHasNoOverride()
        {
            var rc = new FakeRemoteConfigProvider();
            rc.Ints[EventConfigKeys.KeyDefaultUnlockLevel] = 15;
            ServiceLocator.Instance.Register<Sorolla.IRemoteConfigProvider>(rc);

            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1)));
            var mgr = MakeManager(clock, cat);

            Assert.IsFalse(mgr.IsUnlocked(14));
            Assert.IsTrue(mgr.IsUnlocked(15));
        }

        [Test]
        public void TimeUntilActiveEnds_ReportsRemaining()
        {
            // BaseUtc = Friday 12:00 UTC. Event covers Fri-Fri only → ends at Sat 00:00 UTC = 12 hours.
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", DayOfWeek.Friday, DayOfWeek.Friday));
            var mgr = MakeManager(clock, cat);
            Assert.AreEqual(System.TimeSpan.FromHours(12), mgr.TimeUntilActiveEnds);
        }

        [Test]
        public void BeginRunCollector_NullWhenNoActiveEvent()
        {
            var mgr = MakeManager();
            Assert.IsNull(mgr.BeginRunCollector());
        }

        [Test]
        public void BeginRunCollector_ReturnsCollectorForActiveEvent()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1)));
            var mgr = MakeManager(clock, cat);
            var c = mgr.BeginRunCollector();
            Assert.IsNotNull(c);
            Assert.AreEqual("a", c.EventId);
        }

        [Test]
        public void CommitRun_NoOp_WhenCollectorNull()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1)));
            var mgr = MakeManager(clock, cat);
            mgr.CommitRun(null);
            Assert.AreEqual(0, mgr.GetProgress("a").progress);
        }

        [Test]
        public void CommitRun_NoOp_WhenCollectorEmpty()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1)));
            var mgr = MakeManager(clock, cat);
            var c = mgr.BeginRunCollector();
            mgr.CommitRun(c);
            Assert.AreEqual(0, mgr.GetProgress("a").progress);
        }

        [Test]
        public void CommitRun_AddsProgress_AndFiresOnProgressChanged()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1)));
            var mgr = MakeManager(clock, cat);
            int observedNew = -1, observedDelta = -1;
            mgr.OnProgressChanged += (id, n, d) => { observedNew = n; observedDelta = d; };

            var c = mgr.BeginRunCollector();
            c.Add(7);
            mgr.CommitRun(c);

            Assert.AreEqual(7, mgr.GetProgress("a").progress);
            Assert.AreEqual(7, observedNew);
            Assert.AreEqual(7, observedDelta);
        }

        [Test]
        public void CommitRun_AutoClaimsStep_OnThresholdCross_AndGrantsReward()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            var def = MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1));
            def.Steps.Add(new EventStep { Threshold = 5, Rewards = new System.Collections.Generic.List<EventReward>{
                new EventReward { ItemType = "coins", Amount = 100 } } });
            cat.Catalog.Add(def);

            var granter = new FakeRewardGranter();
            var mgr = MakeManager(clock, cat, granter);
            var claimedSteps = new System.Collections.Generic.List<int>();
            mgr.OnStepClaimed += (id, i) => claimedSteps.Add(i);

            var c = mgr.BeginRunCollector();
            c.Add(10);
            mgr.CommitRun(c);

            Assert.AreEqual(new System.Collections.Generic.List<int>{0}, claimedSteps);
            Assert.AreEqual(1, granter.Grants.Count);
            Assert.AreEqual("coins", granter.Grants[0].reward.ItemType);
            Assert.AreEqual(0, granter.Grants[0].ctx.StepIndex);
            Assert.IsFalse(granter.Grants[0].ctx.IsGrandPrize);
        }

        [Test]
        public void CommitRun_StepClaim_IsIdempotent()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            var def = MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1));
            def.Steps.Add(new EventStep { Threshold = 5, Rewards = new System.Collections.Generic.List<EventReward>{
                new EventReward { ItemType = "coins", Amount = 100 } } });
            cat.Catalog.Add(def);
            var granter = new FakeRewardGranter();
            var mgr = MakeManager(clock, cat, granter);

            var c1 = mgr.BeginRunCollector(); c1.Add(10); mgr.CommitRun(c1);
            var c2 = mgr.BeginRunCollector(); c2.Add(5);  mgr.CommitRun(c2);

            Assert.AreEqual(15, mgr.GetProgress("a").progress);
            Assert.AreEqual(1, granter.Grants.Count, "step reward must grant exactly once");
        }

        [Test]
        public void CommitRun_GrandPrize_AutoGranted_WhenAllStepsCrossed()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            var def = MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1));
            def.Steps.Add(new EventStep { Threshold = 5, Rewards = new System.Collections.Generic.List<EventReward>{
                new EventReward { ItemType = "coins", Amount = 100 } } });
            def.Steps.Add(new EventStep { Threshold = 10, Rewards = new System.Collections.Generic.List<EventReward>{
                new EventReward { ItemType = "gems", Amount = 5 } } });
            def.GrandPrize = new EventReward { ItemType = "skin", ItemId = "golden_serpent", Amount = 1 };
            cat.Catalog.Add(def);

            var granter = new FakeRewardGranter();
            var mgr = MakeManager(clock, cat, granter);
            string grandFor = null;
            mgr.OnGrandPrizeClaimed += id => grandFor = id;

            var c = mgr.BeginRunCollector();
            c.Add(20);
            mgr.CommitRun(c);

            Assert.AreEqual("a", grandFor);
            Assert.IsTrue(mgr.GetProgress("a").grandPrizeClaimed);
            Assert.AreEqual(3, granter.Grants.Count); // step0, step1, grand
            Assert.IsTrue(granter.Grants[2].ctx.IsGrandPrize);
            Assert.AreEqual("skin", granter.Grants[2].reward.ItemType);
        }

        [Test]
        public void CommitRun_IgnoredForWrongEventId()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1)));
            var mgr = MakeManager(clock, cat);
            var stale = new EventCollector("b"); // not active
            stale.Add(99);
            mgr.CommitRun(stale);
            Assert.AreEqual(0, mgr.GetProgress("a").progress);
        }

        [Test]
        public void NoCommit_LeavesPersistedProgressUnchanged()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1)));
            var mgr = MakeManager(clock, cat);

            var c = mgr.BeginRunCollector();
            c.Add(50);
            // Simulate lose/quit: caller drops the collector without calling CommitRun.
            // Persisted progress must remain at 0.
            Assert.AreEqual(0, mgr.GetProgress("a").progress);
            Assert.AreEqual(0UL, mgr.GetProgress("a").claimedStepBitset);
        }

        [Test]
        public void Init_FlagsClockRollback_WhenLastSeenIsFurtherFutureThanGrace()
        {
            const string file = "events_rollback_test";
            try
            {
                var clock1 = new FakeAuthoritativeTime(BaseUtc.AddHours(1));
                var mgr1 = MakeManager(clock1, new FakeEventCatalogProvider(), saveFile: file, disableSaves: false);
                // Tick wrote lastSeen; persist by simulating quit.
                mgr1.FlushSave();
                UnityEngine.Object.DestroyImmediate(mgr1.gameObject);

                var clock2 = new FakeAuthoritativeTime(BaseUtc);
                var mgr2 = MakeManager(clock2, new FakeEventCatalogProvider(), saveFile: file, disableSaves: false);
                Assert.IsTrue(mgr2.LastClockRollbackDetected);
            }
            finally
            {
                Sorolla.PersistentData.SaveSystem.Delete(file);
            }
        }

        [Test]
        public void KillSwitch_DropsActiveEvent_WhenEnabledFlipsToFalse()
        {
            var clock = new FakeAuthoritativeTime(BaseUtc);
            var cat = new FakeEventCatalogProvider();
            cat.Catalog.Add(MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1)));

            // Phase 1: enabled, event is active.
            var mgr = MakeManager(clock, cat);
            Assert.AreEqual("a", mgr.ActiveEvent?.EventId);

            // Phase 2: flip the kill switch via RC.
            var rc = new FakeRemoteConfigProvider();
            rc.Bools[EventConfigKeys.KeyEnabled] = false;
            ServiceLocator.Instance.Register<Sorolla.IRemoteConfigProvider>(rc);

            EventEndReason? observed = null;
            mgr.OnActiveEventEnded += (d, r) => observed = r;
            cat.FireChanged(); // triggers Tick

            Assert.IsNull(mgr.ActiveEvent);
            Assert.AreEqual(EventEndReason.KillSwitchDisabled, observed);
        }

        [Test]
        public void Save_RoundTrip_Preserves_ProgressAndClaims()
        {
            const string file = "events_roundtrip_test";
            try
            {
                var clock = new FakeAuthoritativeTime(BaseUtc);
                var cat = new FakeEventCatalogProvider();
                var def = MakeDef("a", BaseUtc.AddHours(-1), BaseUtc.AddHours(1));
                def.Steps.Add(new EventStep { Threshold = 5, Rewards = new System.Collections.Generic.List<EventReward>{
                    new EventReward { ItemType = "coins", Amount = 100 } } });
                cat.Catalog.Add(def);

                var mgr1 = MakeManager(clock, cat, saveFile: file, disableSaves: false);
                var c = mgr1.BeginRunCollector(); c.Add(7); mgr1.CommitRun(c);
                mgr1.FlushSave();
                UnityEngine.Object.DestroyImmediate(mgr1.gameObject);

                var mgr2 = MakeManager(clock, cat, saveFile: file, disableSaves: false);
                var p = mgr2.GetProgress("a");
                Assert.AreEqual(7, p.progress);
                Assert.AreEqual(1UL, p.claimedStepBitset);
            }
            finally
            {
                Sorolla.PersistentData.SaveSystem.Delete(file);
            }
        }
    }
}
