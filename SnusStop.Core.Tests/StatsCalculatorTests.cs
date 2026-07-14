using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace SnusStop.Core.Tests;

public class StatsCalculatorTests
{
    private static Profile P(int ppd = 15, int ppc = 20, decimal price = 45m)
        => new()
        {
            Name = "Mikkel",
            QuitUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            PouchesPerDay = ppd,
            PouchesPerCan = ppc,
            CanPrice = price,
            Currency = Currency.DKK,
        };

    [Fact]
    public void PerPouch_is_price_over_can()
        => Assert.Equal(2.25m, StatsCalculator.PerPouch(P()));

    [Fact]
    public void Money_and_pouches_grow_with_elapsed_days()
    {
        var p = P();
        var now = p.QuitUtc.AddDays(10);
        var s = StatsCalculator.Compute(p, now, null);

        Assert.Equal(10, s.Days);
        Assert.Equal(337.5m, Math.Round(s.Money, 1));   // 10 * 15 * 2.25
        Assert.Equal(150, s.PouchesAvoided);            // floor(10 * 15)
    }

    [Fact]
    public void Next_progress_milestone_after_10_days_is_two_weeks()
    {
        var p = P();
        var s = StatsCalculator.Compute(p, p.QuitUtc.AddDays(10), null);
        Assert.Equal("2w", s.Next!.Key);
    }

    [Fact]
    public void Streak_counts_from_last_slip()
    {
        var p = P();
        var now = p.QuitUtc.AddDays(20);
        var slip = now.AddDays(-3);
        var s = StatsCalculator.Compute(p, now, slip);
        Assert.Equal(3, s.CurrentStreak);
    }

    [Fact]
    public void Streak_is_full_elapsed_without_a_slip()
    {
        var p = P();
        var s = StatsCalculator.Compute(p, p.QuitUtc.AddDays(12), null);
        Assert.Equal(12, s.CurrentStreak);
    }

    [Fact]
    public void FormatMoney_uses_danish_grouping()
    {
        Assert.Equal("1.234,56", StatsCalculator.FormatMoney(1234.56m, withDecimals: true));
        Assert.Equal("1.235", StatsCalculator.FormatMoney(1234.56m, withDecimals: false));
    }

    [Fact]
    public void Recovery_is_monotonic_and_bounded()
    {
        var d1 = StatsCalculator.RecoveryPercent(TimeSpan.FromDays(1));
        var d12 = StatsCalculator.RecoveryPercent(TimeSpan.FromDays(12));
        var d400 = StatsCalculator.RecoveryPercent(TimeSpan.FromDays(400));
        Assert.True(d1 < d12);
        Assert.True(d12 is > 0 and < 1);
        Assert.Equal(1.0, d400);
    }
}
