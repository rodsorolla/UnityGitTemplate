using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Sorolla.Events;
using Sorolla.PersistentData;
using Sorolla.Profile;

namespace Sorolla.Tournaments
{
    /// Weekly client-side tournament. Add to the Init scene; assign config + ProfileCatalog.
    /// The game-side TournamentLevelWinAdapter calls RecordLevelWin() on level wins.
    public class TournamentService : SorollaManager, ITournamentService
    {
        private const string SaveFile = "tournament";

        [SerializeField] private TournamentConfig _config;
        [SerializeField] private ProfileCatalog _catalog;

        private TournamentState _state;
        private TournamentConfigData _data;
        private IAuthoritativeTime _clock;
        private IPlayerProfile _profile;
        private bool _claiming;

        public event Action OnTrophiesChanged;
        public event Action OnTournamentRolledOver;

        private static readonly TierDefinition[] EmptyTiers = Array.Empty<TierDefinition>();

        public int CurrentTierIndex => _state?.CurrentTierIndex ?? 0;
        public int PlayerTrophies => _state?.PlayerTrophies ?? 0;
        public DateTime WeekEndUtc => TournamentWeek.WeekEndUtc(_state?.ActiveWeekIndex ?? 0);
        public IReadOnlyList<TierDefinition> Tiers => _data?.Tiers ?? EmptyTiers;
        public bool HasPendingResult => _state?.PendingResult != null && !_state.PendingResult.Claimed;

        protected override void Initialize()
        {
            _clock = ServiceLocator.Instance.TryResolve<IAuthoritativeTime>() ?? new DefaultAuthoritativeTime();
            _profile = ServiceLocator.Instance.TryResolve<IPlayerProfile>();
            _data = ResolveConfig();

            _state = SaveSystem.Load<TournamentState>(SaveFile) ?? new TournamentState();

            // Feed the last persisted time to the (per-service) clock so a backward device-clock
            // change is detected and freezes rollover. Nothing else observes this clock.
            if (_state.LastSeenUtcTicks != 0)
                _clock.ObservePersisted(new DateTime(_state.LastSeenUtcTicks, DateTimeKind.Utc),
                    TimeSpan.FromSeconds(EventConfigKeys.ClockRollbackGraceSeconds));

            if (_state.ActiveWeekIndex == 0)
                _state.ActiveWeekIndex = TournamentWeek.WeekIndex(_clock.UtcNow);

            SyncToCurrentWeek();
            Persist();

            ServiceLocator.Instance.Register<ITournamentService>(this);
        }

        private TournamentConfigData ResolveConfig()
        {
            var rc = TournamentConfigJson.TryParseActive(out var err);
            if (err != null) Debug.LogWarning($"[Tournaments] config json note: {err}");
            if (rc != null && rc.IsValid) return rc;
            return _config != null ? _config.ToData() : new TournamentConfigData(null, null);
        }

        public void RecordLevelWin()
        {
            if (_state == null) return;
            SyncToCurrentWeek();        // roll over first so the trophy lands in the current week
            _state.PlayerTrophies++;
            Persist();
            OnTrophiesChanged?.Invoke();
        }

        public bool HasJoined => _state?.HasJoined ?? false;

        public void EnsureJoined()
        {
            if (_state == null || _state.HasJoined) return;
            _state.HasJoined = true;
            _state.PlayerTrophies = 0;  // discard any pre-unlock accrual; the player joins at 0
            Persist();
            OnTrophiesChanged?.Invoke();
        }

        public void SyncToCurrentWeek()
        {
            if (_data == null || !_data.IsValid) return;

            int current = TournamentWeek.WeekIndex(_clock.UtcNow);
            var action = RolloverPolicy.Decide(_state.ActiveWeekIndex, current,
                _state.PlayerTrophies, _clock.RollbackDetectedThisSession);

            if (action == RolloverAction.Hold) return;

            if (action == RolloverAction.Finalize)
                FinalizeActiveWeek();

            _state.ActiveWeekIndex = current;
            _state.PlayerTrophies = 0;
            Persist();
            OnTournamentRolledOver?.Invoke();
        }

        public IReadOnlyList<LeaderboardEntry> GetLeaderboard()
        {
            var list = new List<LeaderboardEntry>();
            if (_data == null || !_data.IsValid) return list;

            SyncToCurrentWeek();

            var tier = TierAt(_state.CurrentTierIndex);
            var bots = BotRoster.Build(_state.CurrentTierIndex, _state.ActiveWeekIndex, tier, _catalog, _data.BotNames);
            // Bots ramp from actual week progress (Monday 00:00 UTC), matching the player's own
            // weekly trophies which reset to 0 at week start.
            double frac = TournamentWeek.ElapsedFraction(_clock.UtcNow, _state.ActiveWeekIndex);

            foreach (var b in bots)
                list.Add(new LeaderboardEntry
                {
                    DisplayName = b.displayName,
                    AvatarId = b.avatarId,
                    CountryCode = b.countryCode,
                    Trophies = BotProgress.TrophiesAt(b.weeklyTarget, b.id, frac),
                    IsPlayer = false
                });

            list.Add(new LeaderboardEntry
            {
                DisplayName = _profile?.DisplayName ?? "You",
                AvatarId = _profile?.AvatarId,
                CountryCode = _profile?.CountryCode,
                Trophies = _state.PlayerTrophies,
                IsPlayer = true
            });

            list.Sort((a, b) =>
            {
                if (a.Trophies != b.Trophies) return b.Trophies.CompareTo(a.Trophies);
                if (a.IsPlayer) return -1;
                if (b.IsPlayer) return 1;
                return string.CompareOrdinal(a.DisplayName, b.DisplayName); // deterministic, stable order
            });
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                e.Rank = i + 1;
                list[i] = e;
            }
            return list;
        }

        public PendingResult GetPendingResult() => _state?.PendingResult;

        public async UniTask ClaimPendingResultAsync()
        {
            if (_claiming) return;                  // ignore re-entrant taps while a claim is in flight
            var pr = _state?.PendingResult;
            if (pr == null || pr.Claimed) return;

            _claiming = true;
            try
            {
                if (pr.FinalRank >= 1 && pr.FinalRank <= 3)
                {
                    var tier = TierAt(pr.TierIndex);
                    var rewards = tier?.PodiumReward(pr.FinalRank);
                    var granter = ServiceLocator.Instance.TryResolve<IRewardGranter>();
                    if (rewards != null && granter != null)
                    {
                        var ctx = new RewardGrantContext { EventId = "tournament", StepIndex = pr.FinalRank, IsGrandPrize = pr.FinalRank == 1 };
                        foreach (var reward in rewards)
                            await granter.Grant(reward, ctx);
                    }
                }

                pr.Claimed = true;
                _state.PendingResult = null;
                Persist();
            }
            finally
            {
                _claiming = false;   // on grant failure, pending stays so the player can retry (no double-grant, no lost reward)
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD || SOROLLA_DEBUG_MENU
        /// Debug-only: forces an unclaimed pending result so the results/reward flow can be
        /// exercised without waiting for a real week rollover. tierIndex &lt; 0 = current tier.
        public void DebugForcePendingResult(int finalRank, int tierIndex = -1)
        {
            if (_state == null) return;
            if (tierIndex < 0) tierIndex = _state.CurrentTierIndex;
            _state.PendingResult = new PendingResult
            {
                WeekIndex = _state.ActiveWeekIndex,
                TierIndex = tierIndex,
                FinalRank = finalRank,
                Outcome = TournamentOutcome.Stayed,
                Claimed = false
            };
            Persist();
            Debug.Log($"[Tournaments][Debug] Forced pending result: rank={finalRank}, tier={tierIndex}");
        }

        /// Debug-only: sets the player's current-week trophy count directly.
        public void DebugSetTrophies(int trophies)
        {
            if (_state == null) return;
            _state.PlayerTrophies = trophies < 0 ? 0 : trophies;
            Persist();
            OnTrophiesChanged?.Invoke();
            Debug.Log($"[Tournaments][Debug] Set player trophies to {_state.PlayerTrophies}.");
        }

        /// Debug-only: ends the active week now — finalizes standings against the current
        /// leaderboard (real rank/outcome from the player's trophies), advances to a fresh week,
        /// and raises the rollover event, so the end-of-week results flow can be exercised
        /// without waiting for the real week boundary.
        public void DebugFinishTournament()
        {
            if (_state == null || _data == null || !_data.IsValid)
            {
                Debug.LogWarning("[Tournaments][Debug] Cannot finish tournament: no valid config/state.");
                return;
            }
            FinalizeActiveWeek();
            _state.ActiveWeekIndex += 1;
            _state.PlayerTrophies = 0;
            Persist();
            OnTournamentRolledOver?.Invoke();
            Debug.Log($"[Tournaments][Debug] Finished tournament: rank={_state.PendingResult?.FinalRank}, outcome={_state.PendingResult?.Outcome}.");
        }
#endif

        // Computes the active week's final standings (bots at full weekly target), records the
        // pending result, and applies the tier promotion/demotion. Does NOT advance the week.
        private void FinalizeActiveWeek()
        {
            var tier = TierAt(_state.CurrentTierIndex);
            var bots = BotRoster.Build(_state.CurrentTierIndex, _state.ActiveWeekIndex, tier, _catalog, _data.BotNames);
            var finalTrophies = new List<int>(bots.Count);
            foreach (var b in bots) finalTrophies.Add(BotProgress.TrophiesAt(b.weeklyTarget, b.id, 1.0));

            var r = Standings.Compute(_state.PlayerTrophies, finalTrophies, tier.promotePct, tier.demotePct);
            _state.PendingResult = new PendingResult
            {
                WeekIndex = _state.ActiveWeekIndex,
                TierIndex = _state.CurrentTierIndex,
                FinalRank = r.PlayerRank,
                Outcome = r.PlayerOutcome,
                Claimed = false
            };
            _state.CurrentTierIndex = TierTransition.Apply(_state.CurrentTierIndex, r.PlayerOutcome, _data.Tiers.Count);
        }

        // All state writes go through here so the last-seen UTC stamp stays current for the
        // next-launch backward-clock check. Synchronous (single save file) per the iOS/save guidance.
        private void Persist()
        {
            _state.LastSeenUtcTicks = _clock.UtcNow.Ticks;
            SaveSystem.Save(_state, SaveFile);
        }

        private TierDefinition TierAt(int index)
        {
            if (_data == null || _data.Tiers.Count == 0) return null;
            if (index < 0) index = 0;
            if (index > _data.Tiers.Count - 1) index = _data.Tiers.Count - 1;
            return _data.Tiers[index];
        }
    }
}
