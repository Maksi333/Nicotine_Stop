using SnusStop.Core.Services;

namespace SnusStop.Core.Tests;

public class DailyXpServiceTests
{
    /// <summary>In-memory stand-in for the app's Preferences-backed store.</summary>
    private sealed class FakeStore : IDailyXpStore
    {
        private readonly Dictionary<string, DateTime> _claims = new();

        public DateTime? GetLastClaimedDate(string activityId) =>
            _claims.TryGetValue(activityId, out var d) ? d : null;

        public void SetLastClaimedDate(string activityId, DateTime localDate) =>
            _claims[activityId] = localDate;

        public void Clear() => _claims.Clear();
    }

    /// <summary>A clock the test moves by hand, so "local midnight" is exercised deterministically.</summary>
    private sealed class FakeClock
    {
        public DateTime LocalNow { get; set; } = new(2026, 7, 14, 9, 0, 0, DateTimeKind.Local);
        public DateTime Now() => LocalNow;
    }

    private static (DailyXpService Svc, FakeStore Store, FakeClock Clock) Sut()
    {
        var store = new FakeStore();
        var clock = new FakeClock();
        return (new DailyXpService(store, clock.Now), store, clock);
    }

    [Fact]
    public void First_completion_of_the_day_awards_the_full_amount()
    {
        var (svc, _, _) = Sut();

        Assert.True(svc.CanClaimXp(ActivityIds.Minesweeper));
        Assert.Equal(15, svc.ClaimXp(ActivityIds.Minesweeper, 15));
    }

    [Fact]
    public void Later_completions_the_same_day_award_zero()
    {
        var (svc, _, _) = Sut();

        svc.ClaimXp(ActivityIds.Minesweeper, 15);

        Assert.False(svc.CanClaimXp(ActivityIds.Minesweeper));
        Assert.Equal(0, svc.ClaimXp(ActivityIds.Minesweeper, 15));
        Assert.Equal(0, svc.ClaimXp(ActivityIds.Minesweeper, 15));
    }

    [Fact]
    public void Activities_are_independent_of_each_other()
    {
        var (svc, _, _) = Sut();

        svc.ClaimXp(ActivityIds.Minesweeper, 15);

        Assert.True(svc.CanClaimXp(ActivityIds.Memory));
        Assert.Equal(10, svc.ClaimXp(ActivityIds.Memory, 10));
        Assert.True(svc.CanClaimXp(ActivityIds.RemindMeWhy));
        Assert.Equal(15, svc.ClaimXp(ActivityIds.RemindMeWhy, 15));

        // ...and the already-claimed one stays claimed.
        Assert.False(svc.CanClaimXp(ActivityIds.Minesweeper));
    }

    [Fact]
    public void Crossing_local_midnight_re_enables_the_activity()
    {
        var (svc, _, clock) = Sut();

        Assert.Equal(15, svc.ClaimXp(ActivityIds.Breathe, 15));

        // Later the same evening: still spent.
        clock.LocalNow = new DateTime(2026, 7, 14, 23, 59, 0, DateTimeKind.Local);
        Assert.False(svc.CanClaimXp(ActivityIds.Breathe));

        // One minute later it is a new local day.
        clock.LocalNow = new DateTime(2026, 7, 15, 0, 1, 0, DateTimeKind.Local);
        Assert.True(svc.CanClaimXp(ActivityIds.Breathe));
        Assert.Equal(15, svc.ClaimXp(ActivityIds.Breathe, 15));
    }

    [Fact]
    public void Claim_state_survives_a_restart_via_the_store()
    {
        var store = new FakeStore();
        var clock = new FakeClock();

        // First "run" of the app claims the XP.
        new DailyXpService(store, clock.Now).ClaimXp(ActivityIds.Reflex, 10);

        // A brand-new service instance (app restarted) reads the same persisted store.
        var afterRestart = new DailyXpService(store, clock.Now);
        Assert.False(afterRestart.CanClaimXp(ActivityIds.Reflex));
        Assert.Equal(0, afterRestart.ClaimXp(ActivityIds.Reflex, 10));
    }

    [Fact]
    public void Time_of_day_does_not_matter_only_the_calendar_date()
    {
        var (svc, _, clock) = Sut();

        clock.LocalNow = new DateTime(2026, 7, 14, 0, 5, 0, DateTimeKind.Local);
        Assert.Equal(10, svc.ClaimXp(ActivityIds.PouchPop, 10));

        // 23 hours later, same calendar day → still spent.
        clock.LocalNow = new DateTime(2026, 7, 14, 23, 5, 0, DateTimeKind.Local);
        Assert.False(svc.CanClaimXp(ActivityIds.PouchPop));
    }
}
