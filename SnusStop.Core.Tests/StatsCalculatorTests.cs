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
        Assert.Equal(3, s.Days);
    }

    [Fact]
    public void Slip_restarts_clean_timer_and_milestones_but_only_deducts_actual_spending()
    {
        var p = P();
        var now = p.QuitUtc.AddDays(10);
        var before = StatsCalculator.Compute(p, now, null);
        var after = StatsCalculator.Compute(p, now, now, 45.50m);

        Assert.Equal(0, after.Days);
        Assert.Equal(0, after.Hours);
        Assert.Equal(0, after.Min);
        Assert.Equal(0, after.Sec);
        Assert.Equal(0, after.DaysFloat);
        Assert.Equal(0, after.CurrentStreak);
        Assert.Equal(0, after.RingProgress);
        Assert.Equal(0, after.RecoveryPercent);
        Assert.Equal(Milestones.Progress[0], after.Next);
        Assert.Equal(before.Money - 45.50m, after.Money);
        Assert.Equal(before.PouchesAvoided, after.PouchesAvoided);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), p.QuitUtc);
    }

    [Fact]
    public void Clean_timer_and_savings_continue_growing_after_a_slip()
    {
        var p = P();
        var slip = p.QuitUtc.AddDays(10);
        var now = slip.AddDays(2).AddHours(3).AddMinutes(4).AddSeconds(5);
        var after = StatsCalculator.Compute(p, now, slip, 45m);
        var withoutSlip = StatsCalculator.Compute(p, now, null);

        Assert.Equal(2, after.Days);
        Assert.Equal(3, after.Hours);
        Assert.Equal(4, after.Min);
        Assert.Equal(5, after.Sec);
        Assert.Equal(withoutSlip.Money - 45m, after.Money);
        Assert.Equal(StatsCalculator.RecoveryPercent(now - slip), after.RecoveryPercent);
    }

    [Fact]
    public void Repeated_slips_deduct_cumulative_spending_and_use_the_latest_restart()
    {
        var p = P();
        var slips = new[]
        {
            EventLog.Slip(p.QuitUtc.AddDays(3), Trigger.Stress, null, 45.50m),
            EventLog.Slip(p.QuitUtc.AddDays(7), Trigger.Coffee, "Bought a pack", 60.25m),
        };
        var now = p.QuitUtc.AddDays(10);
        var after = StatsCalculator.Compute(p, now, slips.Max(e => e.TimestampUtc), slips.Sum(e => e.AmountSpent));

        Assert.Equal(3, after.Days);
        Assert.Equal(231.75m, after.Money);
    }

    [Fact]
    public void Slip_without_spending_preserves_savings_and_restarts_clean_days()
    {
        var p = P();
        var now = p.QuitUtc.AddDays(10);
        var slip = EventLog.Slip(now, Trigger.None, null);
        var after = StatsCalculator.Compute(p, now, slip.TimestampUtc, slip.AmountSpent);

        Assert.Equal(0m, slip.AmountSpent);
        Assert.Equal(0, after.Days);
        Assert.Equal(337.5m, after.Money);
    }

    [Fact]
    public void Spending_beyond_savings_is_fully_deducted_and_can_be_recovered_over_time()
    {
        var p = P();
        var slip = p.QuitUtc.AddDays(1);
        Assert.Equal(-66.25m, StatsCalculator.Compute(p, slip, slip, 100m).Money);
        Assert.Equal(1.25m, StatsCalculator.Compute(p, p.QuitUtc.AddDays(3), slip, 100m).Money);
    }

    [Fact]
    public void Slip_before_the_quit_date_does_not_start_the_clean_timer_early()
    {
        var p = P();
        var beforeQuit = p.QuitUtc.AddDays(-1);
        var s = StatsCalculator.Compute(p, beforeQuit, beforeQuit, 45m);
        Assert.Equal(0, s.Days);
        Assert.Equal(0, s.CurrentStreak);
        Assert.Equal(0, s.RingProgress);
        Assert.Equal(-45m, s.Money);
        Assert.Equal(p.QuitUtc, StatsCalculator.CleanSinceUtc(p, beforeQuit));
    }

    [Fact]
    public void Streak_is_full_elapsed_without_a_slip()
    {
        var p = P();
        var s = StatsCalculator.Compute(p, p.QuitUtc.AddDays(12), null);
        Assert.Equal(12, s.CurrentStreak);
    }

    [Fact]
    public void FormatMoney_uses_danish_grouping_for_kroner()
    {
        Assert.Equal("1.234,56 kr", StatsCalculator.FormatMoney(1234.56m, Currency.DKK, withDecimals: true));
        Assert.Equal("1.235 kr", StatsCalculator.FormatMoney(1234.56m, Currency.DKK, withDecimals: false));
    }

    [Fact]
    public void FormatMoney_carries_the_chosen_currencys_symbol()
    {
        // The bug this guards: every screen used to print "kr" no matter what the user picked.
        Assert.Equal("45 kr", StatsCalculator.FormatMoney(45m, Currency.DKK, false));
        Assert.Equal("45 kr", StatsCalculator.FormatMoney(45m, Currency.SEK, false));
        Assert.Equal("45 kr", StatsCalculator.FormatMoney(45m, Currency.NOK, false));
        Assert.Equal("45 €", StatsCalculator.FormatMoney(45m, Currency.EUR, false));
        Assert.Equal("$45", StatsCalculator.FormatMoney(45m, Currency.USD, false));
    }

    [Fact]
    public void Dollars_lead_the_amount_and_use_us_grouping()
    {
        // "$1,234.56", never "1.234,56 $".
        Assert.Equal("$1,234.56", StatsCalculator.FormatMoney(1234.56m, Currency.USD, withDecimals: true));
        Assert.Equal("$1,235", StatsCalculator.FormatMoney(1234.56m, Currency.USD, withDecimals: false));
    }

    [Fact]
    public void FormatNumber_groups_counts_without_any_currency_symbol()
    {
        // Used for things like "5.475 cigarettes never used" — a count, not money.
        Assert.Equal("5.475", StatsCalculator.FormatNumber(5475m, Currency.DKK));
        Assert.Equal("5,475", StatsCalculator.FormatNumber(5475m, Currency.USD));
    }

    [Fact]
    public void Every_currency_has_a_symbol_and_appears_in_the_picker_list()
    {
        Assert.Equal(5, Currencies.All.Count);
        Assert.Contains(Currency.USD, Currencies.All);
        foreach (var c in Currencies.All)
            Assert.False(string.IsNullOrWhiteSpace(c.Symbol()), $"{c} has no symbol");
    }

    [Fact]
    public void Currency_parses_from_its_code()
    {
        Assert.Equal(Currency.USD, CurrencyExtensions.Parse("USD"));
        Assert.Equal(Currency.EUR, CurrencyExtensions.Parse("EUR"));
        Assert.Equal(Currency.DKK, CurrencyExtensions.Parse("nonsense"));   // safe default
    }

    [Fact]
    public void Placeholder_profile_does_not_read_as_millennia_of_progress()
    {
        // A default Profile is what AppState holds before the saved one is read. Dated
        // default(DateTime) it would compute ~740,000 nicotine-free days and flash that on screen.
        var s = StatsCalculator.Compute(new Profile(), DateTime.UtcNow, null);

        Assert.Equal(0, s.Days);
        Assert.Equal(0, s.PouchesAvoided);
        Assert.Equal(0, s.CurrentStreak);
        Assert.True(s.DaysFloat < 1, $"placeholder profile reported {s.DaysFloat:N0} days");
        Assert.True(s.Money < 0.01m, $"placeholder profile reported {s.Money:N2} saved");
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
