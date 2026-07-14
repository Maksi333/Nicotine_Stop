using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace SnusStop.Core.Tests;

public class XpServiceTests
{
    [Fact]
    public void Award_amounts_match_design()
    {
        Assert.Equal(10, XpService.Award(EventType.CheckIn, null));
        Assert.Equal(15, XpService.Award(EventType.CravingWon, null));
        Assert.Equal(0, XpService.Award(EventType.Slip, null));
    }

    [Fact]
    public void Level_four_is_fresh_air()
    {
        var lvl = XpService.ForXp(840);
        Assert.Equal(4, lvl.Number);
        Assert.Equal("Fresh Air", lvl.Name);
    }

    [Fact]
    public void Level_progress_matches_frame_1h()
    {
        var (inLevel, span) = XpService.LevelProgress(840);
        Assert.Equal(340, inLevel);   // "340 / 500 XP"
        Assert.Equal(500, span);
    }
}

public class BadgeServiceTests
{
    [Fact]
    public void Sample_persona_earns_exactly_the_six_shown_badges()
    {
        var now = new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);
        var p = new Profile
        {
            QuitUtc = now.AddDays(-12),
            PouchesPerDay = 15,
            PouchesPerCan = 20,
            CanPrice = 45m,
        };

        var events = new List<EventLog>();
        for (int i = 0; i < 10; i++)
            events.Add(EventLog.CravingWon(now.AddHours(-i), "reasons", 15));
        events.Add(new EventLog { Type = EventType.GoalFunded, TimestampUtc = now.AddDays(-3) });

        var s = StatsCalculator.Compute(p, now, null);
        var earned = BadgeService.Earned(p, events, s);

        Assert.Equal(6, earned.Count);
        var keys = earned.Select(b => b.Key).ToHashSet();
        Assert.Contains("day1", keys);
        Assert.Contains("day3", keys);
        Assert.Contains("week1", keys);
        Assert.Contains("save250", keys);
        Assert.Contains("cravings10", keys);
        Assert.Contains("goal1", keys);
        Assert.DoesNotContain("week2", keys);
        Assert.DoesNotContain("pouches500", keys);
    }
}
