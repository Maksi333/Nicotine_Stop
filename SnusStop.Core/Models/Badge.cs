namespace SnusStop.Core.Models;

/// <summary>An achievement badge (frame 1o). 24 total; the first six are earned by the sample persona.</summary>
public record Badge(string Key, string Emoji, string Label);

public static class Badges
{
    /// <summary>
    /// The full 24-badge set. Emoji/label for the leading eight match frame 1o exactly
    /// (six earned + "2 weeks" and "1 month" locked). Order here is the canonical grid order.
    /// </summary>
    public static readonly IReadOnlyList<Badge> All = new List<Badge>
    {
        // Time
        new("day1", "🌱", "1 day"),
        new("day3", "🌿", "3 days"),
        new("week1", "🏆", "1 week"),
        new("week2", "📅", "2 weeks"),
        new("month1", "👑", "1 month"),
        new("month3", "💎", "3 months"),
        new("month6", "⭐", "6 months"),
        new("year1", "🎖️", "1 year"),
        // Savings
        new("save250", "💰", "250 kr saved"),
        new("save500", "💵", "500 kr saved"),
        new("save1000", "🤑", "1.000 kr saved"),
        new("save5000", "🏦", "5.000 kr saved"),
        // Cravings
        new("cravings10", "🥊", "10 cravings won"),
        new("cravings25", "🔥", "25 cravings won"),
        new("cravings50", "💪", "50 cravings won"),
        // Goals
        new("goal1", "🎯", "Goal funded"),
        new("goal3", "🎁", "3 goals funded"),
        // Pouches
        new("pouches500", "🚫", "500 pouches skipped"),
        new("pouches1000", "🛑", "1.000 pouches skipped"),
        // Activity
        new("breathe1", "🧘", "First breathing session"),
        new("game1", "🎮", "First game won"),
        new("checkin7", "✅", "7-day check-in streak"),
        new("level5", "🌟", "Level 5 reached"),
        new("weekly1", "📊", "First weekly summary"),
    };

    public static Badge ByKey(string key) => All.First(b => b.Key == key);
}
