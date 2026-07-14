namespace SnusStop.Core.Models;

/// <summary>A point on the timeline. Used both for the Home progress ring and the Health recovery timeline.</summary>
public record Milestone(string Key, string Title, TimeSpan At, string Body);

public static class Milestones
{
    /// <summary>
    /// Health-recovery timeline shown on the Health tab (frame 1n). Bodies are final copy.
    /// </summary>
    public static readonly IReadOnlyList<Milestone> Health = new List<Milestone>
    {
        new("20m", "20 minutes", TimeSpan.FromMinutes(20), "Heart rate and blood pressure settle back to normal."),
        new("72h", "72 hours", TimeSpan.FromHours(72), "Nicotine is fully out of your body. The worst is behind you."),
        new("1w", "1 week", TimeSpan.FromDays(7), "Cravings peak — and start fading in strength and number."),
        new("2w", "2 weeks", TimeSpan.FromDays(14), "Circulation and gum blood flow improving where pouches sat."),
        new("1m", "1 month", TimeSpan.FromDays(30), "Gums begin healing. Energy and sleep improve."),
        new("3m", "3 months", TimeSpan.FromDays(91), "Focus and mood stabilize as dopamine rebalances."),
        new("1y", "1 year", TimeSpan.FromDays(365), "Risk to your heart drops significantly. Full trophy unlock. 🏆"),
    };

    /// <summary>
    /// Progress targets for the Home ring and the streak/day badges. The ring measures elapsed
    /// time against the next unreached target.
    /// </summary>
    public static readonly IReadOnlyList<Milestone> Progress = new List<Milestone>
    {
        new("1d", "1 day", TimeSpan.FromDays(1), ""),
        new("3d", "3 days", TimeSpan.FromDays(3), ""),
        new("1w", "1 week", TimeSpan.FromDays(7), ""),
        new("2w", "2 weeks", TimeSpan.FromDays(14), ""),
        new("1m", "1 month", TimeSpan.FromDays(30), ""),
        new("3m", "3 months", TimeSpan.FromDays(91), ""),
        new("1y", "1 year", TimeSpan.FromDays(365), ""),
    };

    /// <summary>Cumulative recovery weight (0..1) credited once each Health milestone is reached,
    /// interpolated linearly in between. Front-loaded so early wins feel meaningful.</summary>
    public static readonly double[] HealthRecoveryWeights = { 0.05, 0.15, 0.28, 0.45, 0.65, 0.85, 1.0 };
}
