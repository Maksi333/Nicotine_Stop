using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace SnusStop.Core.Tests;

public class GoalAllocatorTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    private static GoalItem Goal(int id, decimal price, int order = 0, string? name = null) =>
        new() { Id = id, Name = name ?? $"Goal {id}", Price = price, SortOrder = order };

    // ---- Allocate: top-down ------------------------------------------------------------------

    [Fact]
    public void Top_down_fills_goals_in_sort_order_and_spills_over()
    {
        var goals = new[] { Goal(2, 300, order: 1), Goal(1, 100, order: 0) };

        var a = GoalAllocator.Allocate(goals, 250m, split: false);

        Assert.Equal(new[] { 1, 2 }, a.Select(x => x.Goal.Id));
        Assert.Equal(100m, a[0].Amount);
        Assert.True(a[0].Funded);
        Assert.Equal(150m, a[1].Amount);
        Assert.False(a[1].Funded);
    }

    [Fact]
    public void Top_down_marks_only_the_first_unfunded_goal_active()
    {
        var goals = new[] { Goal(1, 100, 0), Goal(2, 300, 1), Goal(3, 500, 2) };

        var a = GoalAllocator.Allocate(goals, 150m, split: false);

        Assert.Equal(new[] { false, true, false }, a.Select(x => x.IsActive));
    }

    [Fact]
    public void Negative_savings_allocate_nothing()
    {
        var a = GoalAllocator.Allocate(new[] { Goal(1, 100) }, -40m, split: false);

        Assert.Equal(0m, a[0].Amount);
        Assert.False(a[0].Funded);
    }

    // ---- Allocate: split ---------------------------------------------------------------------

    [Fact]
    public void Split_gives_each_goal_an_equal_share_capped_at_its_price()
    {
        var goals = new[] { Goal(1, 50, 0), Goal(2, 300, 1) };

        var a = GoalAllocator.Allocate(goals, 200m, split: true);

        Assert.Equal(50m, a[0].Amount);
        Assert.True(a[0].Funded);
        Assert.Equal(100m, a[1].Amount);
        Assert.False(a[1].Funded);
    }

    [Fact]
    public void Split_marks_every_unfunded_goal_active()
    {
        var goals = new[] { Goal(1, 50, 0), Goal(2, 300, 1), Goal(3, 400, 2) };

        var a = GoalAllocator.Allocate(goals, 300m, split: true);

        Assert.Equal(new[] { false, true, true }, a.Select(x => x.IsActive));
    }

    // ---- Active goal (widget) ----------------------------------------------------------------

    [Fact]
    public void Active_is_null_without_goals()
    {
        Assert.Null(GoalAllocator.Active(Array.Empty<GoalItem>(), 500m, split: false, perDayCost: 30m));
    }

    [Fact]
    public void Active_is_null_once_every_goal_is_funded()
    {
        Assert.Null(GoalAllocator.Active(new[] { Goal(1, 100) }, 500m, split: false, perDayCost: 30m));
    }

    [Fact]
    public void Active_is_the_first_unfunded_goal_with_its_progress()
    {
        var goals = new[] { Goal(1, 100, 0), Goal(2, 400, 1, "Weekend trip") };

        var g = GoalAllocator.Active(goals, 340m, split: false, perDayCost: 30m)!;

        Assert.Equal("Weekend trip", g.Name);
        Assert.Equal(240m, g.Amount);
        Assert.Equal(160m, g.Remaining);
        Assert.Equal(0.6, g.Fraction, 3);
    }

    [Fact]
    public void Top_down_days_in_and_left_use_the_full_daily_rate()
    {
        // 240 in the goal at 30/day = 8 days in; 160 to go = 5.33 → 6 days left.
        var goals = new[] { Goal(1, 100, 0), Goal(2, 400, 1) };

        var g = GoalAllocator.Active(goals, 340m, split: false, perDayCost: 30m)!;

        Assert.Equal(8, g.DaysIn);
        Assert.Equal(6, g.DaysLeft);
    }

    [Fact]
    public void Split_days_in_and_left_use_the_goal_s_share_of_the_daily_rate()
    {
        // Two goals share 30/day → 15/day each. Goal 1 holds 90 (6 days in), needs 210 more (14 days).
        var goals = new[] { Goal(1, 300, 0), Goal(2, 300, 1) };

        var g = GoalAllocator.Active(goals, 180m, split: true, perDayCost: 30m)!;

        Assert.Equal(1, g.Goal.Id);
        Assert.Equal(6, g.DaysIn);
        Assert.Equal(14, g.DaysLeft);
    }

    [Fact]
    public void Days_are_unknown_when_nothing_is_saved_per_day()
    {
        var g = GoalAllocator.Active(new[] { Goal(1, 100) }, 20m, split: false, perDayCost: 0m)!;

        Assert.Null(g.DaysIn);
        Assert.Null(g.DaysLeft);
        Assert.Equal(0.2, g.Fraction, 3);
    }

    [Fact]
    public void Negative_savings_show_an_empty_goal()
    {
        var g = GoalAllocator.Active(new[] { Goal(1, 100) }, -50m, split: false, perDayCost: 10m)!;

        Assert.Equal(0m, g.Amount);
        Assert.Equal(0d, g.Fraction);
        Assert.Equal(0, g.DaysIn);
        Assert.Equal(10, g.DaysLeft);
    }

    [Theory]
    [InlineData(9, 23, "9 days in · 23 days to go")]
    [InlineData(1, 1, "1 day in · 1 day to go")]
    [InlineData(0, 30, "0 days in · 30 days to go")]
    public void Time_line_reads_days_in_and_days_to_go(int daysIn, int daysLeft, string expected)
    {
        var g = new ActiveGoal(Goal(1, 100), 0m, 0d, daysIn, daysLeft);
        Assert.Equal(expected, g.TimeLine);
    }

    [Fact]
    public void Time_line_for_a_goal_without_a_price_asks_for_one()
    {
        // Goals saved before price validation existed can have price 0 — "0 days to go" would lie.
        var g = GoalAllocator.Active(new[] { Goal(1, 0) }, 200m, split: false, perDayCost: 30m)!;
        Assert.Equal("Set a price in Puffy", g.TimeLine);
    }

    [Fact]
    public void Time_line_without_a_daily_rate_says_so()
    {
        var g = new ActiveGoal(Goal(1, 100), 0m, 0d, null, null);
        Assert.Equal("No daily savings set yet", g.TimeLine);
    }

    // ---- Funded-goal events (badge fix) ------------------------------------------------------

    [Fact]
    public void Funded_goal_without_an_event_is_unrecorded()
    {
        var goals = new[] { Goal(7, 100, 0), Goal(8, 500, 1) };

        var fresh = GoalAllocator.UnrecordedFunded(goals, 150m, split: false, new List<EventLog>());

        Assert.Equal(new[] { 7 }, fresh.Select(g => g.Id));
    }

    [Fact]
    public void Recorded_goal_is_never_reported_again()
    {
        var events = new List<EventLog> { EventLog.GoalFunded(Now, 7) };

        var fresh = GoalAllocator.UnrecordedFunded(new[] { Goal(7, 100) }, 150m, split: false, events);

        Assert.Empty(fresh);
    }

    [Fact]
    public void Goal_funded_event_counts_toward_the_goal_badge()
    {
        var p = new Profile { QuitUtc = Now, PouchesPerDay = 15, PouchesPerCan = 20, CanPrice = 45m };
        var events = new List<EventLog> { EventLog.GoalFunded(Now, 7) };

        var earned = BadgeService.Earned(p, events, StatsCalculator.Compute(p, Now, null));

        Assert.Contains("goal1", earned.Select(b => b.Key));
    }
}
