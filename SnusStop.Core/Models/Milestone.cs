namespace SnusStop.Core.Models;

/// <summary>
/// A point on the timeline. Used both for the Home progress ring and the Health recovery timeline.
/// Body copy is deliberately absent — it depends on what the user is quitting, so it lives in
/// <see cref="AddictionCopy.HealthBody"/>, keyed by <see cref="Key"/>.
/// </summary>
public record Milestone(string Key, string Title, TimeSpan At);

public static class Milestones
{
    /// <summary>
    /// Health-recovery timeline shown on the Health tab (frame 1n). The time-points are the same
    /// whatever the user quit; only the body copy differs.
    /// </summary>
    public static readonly IReadOnlyList<Milestone> Health = new List<Milestone>
    {
        new("20m", "20 minutes", TimeSpan.FromMinutes(20)),
        new("72h", "72 hours", TimeSpan.FromHours(72)),
        new("1w", "1 week", TimeSpan.FromDays(7)),
        new("2w", "2 weeks", TimeSpan.FromDays(14)),
        new("1m", "1 month", TimeSpan.FromDays(30)),
        new("3m", "3 months", TimeSpan.FromDays(91)),
        new("1y", "1 year", TimeSpan.FromDays(365)),
    };

    /// <summary>
    /// Progress targets for the Home ring and the streak/day badges. The ring measures elapsed
    /// time against the next unreached target.
    /// </summary>
    public static readonly IReadOnlyList<Milestone> Progress = new List<Milestone>
    {
        new("1d", "1 day", TimeSpan.FromDays(1)),
        new("3d", "3 days", TimeSpan.FromDays(3)),
        new("1w", "1 week", TimeSpan.FromDays(7)),
        new("2w", "2 weeks", TimeSpan.FromDays(14)),
        new("1m", "1 month", TimeSpan.FromDays(30)),
        new("3m", "3 months", TimeSpan.FromDays(91)),
        new("1y", "1 year", TimeSpan.FromDays(365)),
    };

    /// <summary>Cumulative recovery weight (0..1) credited once each Health milestone is reached,
    /// interpolated linearly in between. Front-loaded so early wins feel meaningful.</summary>
    public static readonly double[] HealthRecoveryWeights = { 0.05, 0.15, 0.28, 0.45, 0.65, 0.85, 1.0 };
}
