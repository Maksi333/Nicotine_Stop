namespace SnusStop.Core.Models;

/// <summary>
/// The user's quit plan. Single row in the local DB. All display statistics are derived
/// from this (plus the events log) at runtime and are never stored.
/// </summary>
public class Profile
{
    public string Name { get; set; } = "";

    /// <summary>
    /// What the user is quitting. Drives all units and copy via <see cref="AddictionCopy"/>.
    /// Defaults to Snus so profiles written before this existed keep working unchanged.
    /// </summary>
    public AddictionType Addiction { get; set; } = AddictionType.Snus;

    /// <summary>
    /// Instant the user became (or will become) nicotine-free, in UTC. Defaults to "now" rather
    /// than default(DateTime): a placeholder profile is otherwise dated year 1, and every stat
    /// derived from it reads as ~740,000 nicotine-free days. Real profiles always set this.
    /// </summary>
    public DateTime QuitUtc { get; set; } = DateTime.UtcNow;

    // Units of the chosen addiction, not necessarily pouches: cigarettes-per-day and
    // cigarettes-per-pack for a smoker. The names predate AddictionType and are kept so existing
    // databases and CSV exports stay readable; the labels shown to users come from AddictionCopy.
    public int PouchesPerDay { get; set; } = 15;
    public int PouchesPerCan { get; set; } = 20;
    public decimal CanPrice { get; set; } = 45m;
    public Currency Currency { get; set; } = Currency.DKK;

    public List<string> Motivations { get; set; } = new();

    public bool SplitSavings { get; set; }

    public bool NotifyMilestone { get; set; } = true;
    public bool NotifyDaily { get; set; } = true;
    public bool NotifyWeekly { get; set; }

    public bool OnboardingComplete { get; set; }

    /// <summary>True while QuitUtc is still in the future (onboarding "pick a future date" path).</summary>
    public bool IsFutureQuit(DateTime nowUtc) => QuitUtc > nowUtc;
}
