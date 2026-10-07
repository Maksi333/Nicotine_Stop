using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace SnusStop.Core.Tests;

public class BadgeAwardServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

    private static Profile Quit(int daysAgo) => new()
    {
        QuitUtc = Now.AddDays(-daysAgo),
        PouchesPerDay = 15,
        PouchesPerCan = 20,
        CanPrice = 45m,
    };

    private static Stats StatsFor(Profile p, DateTime? lastSlipUtc = null) =>
        StatsCalculator.Compute(p, Now, lastSlipUtc);

    private static EventLog Paid(string key, int xp = 100) =>
        new() { TimestampUtc = Now.AddMinutes(-5), Type = EventType.BadgeEarned, Source = key, XpDelta = xp };

    [Fact]
    public void Badge_award_is_100_xp()
    {
        Assert.Equal(100, XpService.BadgeXp);
    }

    [Fact]
    public void Nothing_pending_on_the_very_first_day()
    {
        var p = Quit(0);
        Assert.Empty(BadgeAwardService.Pending(p, new List<EventLog>(), StatsFor(p)));
    }

    [Fact]
    public void Newly_earned_badge_is_pending()
    {
        var p = Quit(1);
        var pending = BadgeAwardService.Pending(p, new List<EventLog>(), StatsFor(p));
        Assert.Contains("day1", pending.Select(b => b.Key));
    }

    [Fact]
    public void Pending_is_everything_earned_when_nothing_is_recorded()
    {
        var p = Quit(21);
        var s = StatsFor(p);
        var earned = BadgeService.Earned(p, new List<EventLog>(), s);
        var pending = BadgeAwardService.Pending(p, new List<EventLog>(), s);
        Assert.Equal(earned.Select(b => b.Key), pending.Select(b => b.Key));
        Assert.True(pending.Count >= 4, "3 weeks in should unlock at least 1 day / 3 days / 1 week / 2 weeks");
    }

    [Fact]
    public void Recorded_badge_never_pays_again()
    {
        var p = Quit(1);
        var pending = BadgeAwardService.Pending(p, new List<EventLog> { Paid("day1") }, StatsFor(p));
        Assert.DoesNotContain("day1", pending.Select(b => b.Key));
    }

    [Fact]
    public void Baselined_badge_recorded_at_zero_xp_never_pays()
    {
        var p = Quit(21);
        var log = BadgeService.Earned(p, new List<EventLog>(), StatsFor(p))
            .Select(b => Paid(b.Key, xp: 0)).ToList();
        Assert.Empty(BadgeAwardService.Pending(p, log, StatsFor(p)));
    }

    [Fact]
    public void Badge_re_earned_after_a_slip_does_not_pay_twice()
    {
        var p = Quit(30);
        var log = new List<EventLog>
        {
            new() { TimestampUtc = Now.AddDays(-9), Type = EventType.Slip },
            Paid("week1"),
        };

        // Slipped 9 days ago, so the streak is 9 days and "1 week" is earned all over again.
        var s = StatsFor(p, Now.AddDays(-9));

        Assert.Contains("week1", BadgeService.Earned(p, log, s).Select(b => b.Key));
        Assert.DoesNotContain("week1", BadgeAwardService.Pending(p, log, s).Select(b => b.Key));
    }

    [Fact]
    public void Slip_preserves_recorded_badges_without_unlocking_unearned_time_badges()
    {
        var p = Quit(30);
        var log = new List<EventLog> { Paid("week1"), Paid("save500"), EventLog.Slip(Now, Trigger.None, null, 900m) };
        var stats = StatsCalculator.Compute(p, Now, Now, 900m);
        var (earned, locked) = BadgeService.Split(p, log, stats);

        Assert.Contains(earned, b => b.Key == "week1");
        Assert.Contains(earned, b => b.Key == "save500");
        Assert.Contains(locked, b => b.Key == "month1");
        Assert.Equal(200, log.Sum(e => e.XpDelta));
        Assert.DoesNotContain(BadgeAwardService.Pending(p, log, stats), b => b.Key == "week1" || b.Key == "save500");
    }
}
