namespace SnusStop.Core.Models;

/// <summary>
/// The user's quit plan. Single row in the local DB. All display statistics are derived
/// from this (plus the events log) at runtime and are never stored.
/// </summary>
public class Profile
{
    public string Name { get; set; } = "";

    /// <summary>Instant the user became (or will become) nicotine-free, in UTC.</summary>
    public DateTime QuitUtc { get; set; }

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
