using SnusStop.Core.Services;

namespace SnusStop.Core.Models;

/// <summary>An achievement badge (frame 1o). 24 total; the first six are earned by the sample persona.</summary>
public record Badge(string Key, string Emoji, string Label);

public static class Badges
{
    /// <summary>
    /// Definition of one badge. <paramref name="Label"/> may contain tokens resolved per user:
    /// {skipped} → the addiction's unit caption, {money} → <paramref name="Money"/> in the user's currency.
    /// </summary>
    private sealed record Def(string Key, string Emoji, string Label, decimal? Money = null);

    /// <summary>
    /// The full 24-badge set. Emoji/label for the leading eight match frame 1o exactly
    /// (six earned + "2 weeks" and "1 month" locked). Order here is the canonical grid order.
    /// </summary>
    private static readonly Def[] Defs =
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
        // Savings — the amount is rendered in whatever currency the user picked.
        new("save250", "💰", "{money} saved", 250m),
        new("save500", "💵", "{money} saved", 500m),
        new("save1000", "🤑", "{money} saved", 1000m),
        new("save5000", "🏦", "{money} saved", 5000m),
        // Cravings
        new("cravings10", "🥊", "10 cravings won"),
        new("cravings25", "🔥", "25 cravings won"),
        new("cravings50", "💪", "50 cravings won"),
        // Goals
        new("goal1", "🎯", "Goal funded"),
        new("goal3", "🎁", "3 goals funded"),
        // Units avoided
        new("pouches500", "🚫", "500 {skipped}"),
        new("pouches1000", "🛑", "1.000 {skipped}"),
        // Activity
        new("breathe1", "🧘", "First breathing session"),
        new("game1", "🎮", "First game won"),
        new("checkin7", "✅", "7-day check-in streak"),
        new("level5", "🌟", "Level 5 reached"),
        new("weekly1", "📊", "First weekly summary"),
    };

    private const string SkippedToken = "{skipped}";
    private const string MoneyToken = "{money}";

    /// <summary>Canonical badge keys, in grid order.</summary>
    public static int Count => Defs.Length;

    /// <summary>
    /// The badge set with every label resolved for this user — their addiction ("500 pouches
    /// skipped" vs "500 cigarettes not smoked") and their currency ("250 kr saved" vs "$250 saved").
    /// This is the only source of displayable badges.
    /// </summary>
    public static IReadOnlyList<Badge> For(AddictionType type, Currency currency) =>
        Defs.Select(d => Resolve(d, type, currency)).ToList();

    public static Badge ByKey(string key, AddictionType type, Currency currency) =>
        Resolve(Defs.First(d => d.Key == key), type, currency);

    private static Badge Resolve(Def d, AddictionType type, Currency currency)
    {
        string label = d.Label;

        if (label.Contains(SkippedToken))
            label = label.Replace(SkippedToken, AddictionCopy.For(type).SkippedCaption);

        if (label.Contains(MoneyToken) && d.Money is decimal amount)
            label = label.Replace(MoneyToken, StatsCalculator.FormatMoney(amount, currency, withDecimals: false));

        return new Badge(d.Key, d.Emoji, label);
    }
}
