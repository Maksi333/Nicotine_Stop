using SnusStop.Core.Models;

namespace SnusStop.Core.Services;

/// <summary>How much of the savings one goal currently holds.</summary>
/// <param name="IsActive">Unfunded and currently receiving money: the first unfunded goal when
/// savings fill top-down, every unfunded goal when they are split.</param>
public sealed record GoalAllocation(GoalItem Goal, decimal Amount, bool Funded, bool IsActive);

/// <summary>
/// The goal the user is working toward right now, with the time it represents.
/// <see cref="DaysIn"/> / <see cref="DaysLeft"/> are null when nothing is saved per day.
/// </summary>
public sealed record ActiveGoal(GoalItem Goal, decimal Amount, double Fraction, int? DaysIn, int? DaysLeft)
{
    public string Name => Goal.Name;
    public decimal Remaining => Math.Max(0m, Goal.Price - Amount);

    /// <summary>"9 days in · 23 days to go" — the goal widget's time line.</summary>
    public string TimeLine =>
        Goal.Price <= 0m ? "Set a price in Puffy"   // saved before prices were validated
        : DaysIn is int days && DaysLeft is int left ? $"{Days(days)} in · {Days(left)} to go"
        : "No daily savings set yet";

    private static string Days(int n) => n == 1 ? "1 day" : $"{n} days";
}

/// <summary>
/// Splits the savings across goals. Derived live from the savings total, never stored — the Goals
/// tab and the home-screen widget both read it from here so they cannot disagree.
/// </summary>
public static class GoalAllocator
{
    /// <summary>
    /// Off = top-down (fill the first goal in sort order, spill into the next); on = an equal share
    /// each. Negative savings (slip spending above what was saved) allocate nothing.
    /// </summary>
    public static IReadOnlyList<GoalAllocation> Allocate(IEnumerable<GoalItem> goals, decimal money, bool split)
    {
        var ordered = goals.OrderBy(g => g.SortOrder).ToList();
        var result = new List<GoalAllocation>(ordered.Count);
        decimal remaining = money;
        bool firstActiveTaken = false;

        foreach (var g in ordered)
        {
            decimal amount;
            if (split)
            {
                amount = Math.Min(g.Price, Math.Max(0, money) / Math.Max(1, ordered.Count));
            }
            else
            {
                amount = Math.Min(g.Price, Math.Max(0, remaining));
                remaining -= amount;
            }

            bool funded = amount >= g.Price && g.Price > 0;
            bool isActive = false;
            if (!funded)
            {
                isActive = split || !firstActiveTaken;
                if (!split) firstActiveTaken = true;
            }

            result.Add(new GoalAllocation(g, amount, funded, isActive));
        }

        return result;
    }

    /// <summary>
    /// The first unfunded goal in sort order, or null when there are no goals or all are funded.
    /// Its days are measured at the rate money actually reaches it: the full daily cost top-down,
    /// an equal share of it when split (every goal accrues the same share).
    /// </summary>
    public static ActiveGoal? Active(IEnumerable<GoalItem> goals, decimal money, bool split, decimal perDayCost)
    {
        var allocs = Allocate(goals, money, split);
        var a = allocs.FirstOrDefault(x => !x.Funded);
        if (a is null) return null;

        decimal rate = split ? perDayCost / allocs.Count : perDayCost;
        double fraction = a.Goal.Price > 0 ? Math.Clamp((double)(a.Amount / a.Goal.Price), 0, 1) : 0;

        int? daysIn = null, daysLeft = null;
        if (rate > 0)
        {
            daysIn = (int)Math.Floor(a.Amount / rate);
            daysLeft = (int)Math.Ceiling(Math.Max(0m, a.Goal.Price - a.Amount) / rate);
        }

        return new ActiveGoal(a.Goal, a.Amount, fraction, daysIn, daysLeft);
    }

    /// <summary>
    /// Goals that are funded now but have no <see cref="EventType.GoalFunded"/> entry yet. The log is
    /// the record, as with badges: a goal is reported once, even if a slip later un-funds it and the
    /// savings fund it again.
    /// </summary>
    public static IReadOnlyList<GoalItem> UnrecordedFunded(
        IEnumerable<GoalItem> goals, decimal money, bool split, IEnumerable<EventLog> events)
    {
        var recorded = events
            .Where(e => e.Type == EventType.GoalFunded && e.Source is not null)
            .Select(e => e.Source!)
            .ToHashSet();

        return Allocate(goals, money, split)
            .Where(a => a.Funded && !recorded.Contains(EventLog.GoalKey(a.Goal.Id)))
            .Select(a => a.Goal)
            .ToList();
    }
}
