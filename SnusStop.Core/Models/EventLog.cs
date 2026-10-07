namespace SnusStop.Core.Models;

/// <summary>One entry in the append-only local events log.</summary>
public class EventLog
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public EventType Type { get; set; }

    /// <summary>For CravingWon: "breathe" | "game" | "reasons". Null otherwise.</summary>
    public string? Source { get; set; }

    /// <summary>For Slip only.</summary>
    public Trigger Trigger { get; set; }

    public string? Note { get; set; }

    /// <summary>XP awarded by this event (0 if none).</summary>
    public int XpDelta { get; set; }

    /// <summary>Money spent on a slip, in the profile's currency. Older events default to zero.</summary>
    public decimal AmountSpent { get; set; }

    public static EventLog CheckIn(DateTime nowUtc) =>
        new() { TimestampUtc = nowUtc, Type = EventType.CheckIn, XpDelta = 10 };

    public static EventLog CravingWon(DateTime nowUtc, string source, int xp) =>
        new() { TimestampUtc = nowUtc, Type = EventType.CravingWon, Source = source, XpDelta = xp };

    public static EventLog Slip(DateTime nowUtc, Trigger trigger, string? note, decimal amountSpent = 0m)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amountSpent);
        return new() { TimestampUtc = nowUtc, Type = EventType.Slip, Trigger = trigger, Note = note, AmountSpent = amountSpent };
    }

    /// <summary>
    /// A badge payout. <paramref name="key"/> is the <see cref="Badge.Key"/>; <paramref name="xp"/>
    /// is 0 for the one-time baseline that marks pre-feature badges as paid without paying them.
    /// </summary>
    public static EventLog BadgeEarned(DateTime nowUtc, string key, int xp) =>
        new() { TimestampUtc = nowUtc, Type = EventType.BadgeEarned, Source = key, XpDelta = xp };

    /// <summary>A goal reached its price for the first time. Source is <see cref="GoalKey"/>.</summary>
    public static EventLog GoalFunded(DateTime nowUtc, int goalId) =>
        new() { TimestampUtc = nowUtc, Type = EventType.GoalFunded, Source = GoalKey(goalId) };

    /// <summary>The <see cref="Source"/> that identifies a goal in <see cref="EventType.GoalFunded"/> entries.</summary>
    public static string GoalKey(int goalId) => $"goal:{goalId}";
}
