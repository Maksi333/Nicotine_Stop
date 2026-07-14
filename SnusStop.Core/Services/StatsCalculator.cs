using System.Globalization;
using SnusStop.Core.Models;

namespace SnusStop.Core.Services;

/// <summary>The full set of live-derived statistics rendered across the app.</summary>
public record Stats(
    int Days, int Hours, int Min, int Sec,
    double DaysFloat,
    decimal Money,
    int PouchesAvoided,
    int CurrentStreak,
    Milestone? Next,
    double RingProgress,
    string RingDash,
    double RecoveryPercent);

/// <summary>
/// Pure derivations from the profile + last-slip time. Nothing here is persisted.
/// Formulas mirror the design's logic block (spec §5).
/// </summary>
public static class StatsCalculator
{
    private static readonly CultureInfo Da = CultureInfo.GetCultureInfo("da-DK");

    /// <summary>Ring circumference for the 232px home ring: 2·π·r with r = 100.</summary>
    public const double RingCircumference = 628.3;

    public static decimal PerPouch(Profile p) =>
        p.PouchesPerCan <= 0 ? 0m : p.CanPrice / p.PouchesPerCan;

    public static decimal PerDayCost(Profile p) => p.PouchesPerDay * PerPouch(p);

    public static decimal WeekSave(Profile p) => PerDayCost(p) * 7m;
    public static decimal MonthSave(Profile p) => PerDayCost(p) * 30.4m;
    public static decimal YearSave(Profile p) => PerDayCost(p) * 365m;
    public static int YearPouches(Profile p) => p.PouchesPerDay * 365;
    public static int CansPerWeek(Profile p) =>
        p.PouchesPerCan <= 0 ? 0 : (int)Math.Round(p.PouchesPerDay * 7.0 / p.PouchesPerCan);

    public static Stats Compute(Profile p, DateTime nowUtc, DateTime? lastSlipUtc)
    {
        var elapsed = nowUtc - p.QuitUtc;
        double totalSeconds = Math.Max(0, elapsed.TotalSeconds);
        double daysFloat = totalSeconds / 86400.0;

        int days = (int)(totalSeconds / 86400);
        int hours = (int)(totalSeconds % 86400 / 3600);
        int min = (int)(totalSeconds % 3600 / 60);
        int sec = (int)(totalSeconds % 60);

        decimal money = (decimal)daysFloat * p.PouchesPerDay * PerPouch(p);
        int pouchesAvoided = (int)Math.Floor(daysFloat * p.PouchesPerDay);

        int currentStreak;
        if (lastSlipUtc is DateTime slip && slip > p.QuitUtc)
            currentStreak = Math.Max(0, (int)((nowUtc - slip).TotalDays));
        else
            currentStreak = days;

        Milestone? next = null;
        foreach (var m in Milestones.Progress)
        {
            if (elapsed < m.At) { next = m; break; }
        }
        double ringProgress = next is null ? 1.0 : Math.Min(1.0, totalSeconds / next.At.TotalSeconds);
        string ringDash = $"{ringProgress * RingCircumference:F1} {RingCircumference:F1}";

        return new Stats(days, hours, min, sec, daysFloat, money, pouchesAvoided,
            currentStreak, next, ringProgress, ringDash, RecoveryPercent(elapsed));
    }

    /// <summary>Days until the next progress milestone (0 once all are reached).</summary>
    public static int DaysUntilNext(Stats s) =>
        s.Next is null ? 0 : Math.Max(0, (int)Math.Ceiling(s.Next.At.TotalDays - s.DaysFloat));

    public static string DaysUntilNextLabel(Stats s)
    {
        int d = DaysUntilNext(s);
        return d == 1 ? "1 day" : $"{d} days";
    }

    /// <summary>Weighted 0..1 recovery across the health-milestone timeline (front-loaded).</summary>
    public static double RecoveryPercent(TimeSpan elapsed)
    {
        var health = Milestones.Health;
        var w = Milestones.HealthRecoveryWeights;
        if (elapsed <= TimeSpan.Zero) return 0;
        if (elapsed >= health[^1].At) return 1.0;
        for (int i = 0; i < health.Count; i++)
        {
            if (elapsed < health[i].At)
            {
                double prevWeight = i == 0 ? 0 : w[i - 1];
                TimeSpan prevAt = i == 0 ? TimeSpan.Zero : health[i - 1].At;
                double frac = (elapsed - prevAt).TotalSeconds / (health[i].At - prevAt).TotalSeconds;
                return prevWeight + frac * (w[i] - prevWeight);
            }
        }
        return 1.0;
    }

    /// <summary>
    /// The one way to render money. Produces the complete string — amount *and* the user's currency
    /// symbol, grouped and positioned per that currency ("1.234,56 kr", "45 €", "$1,234.56").
    ///
    /// Never build a money string by hand: appending a literal "kr" is exactly how every screen
    /// ended up ignoring the user's choice.
    /// </summary>
    public static string FormatMoney(decimal v, Currency currency, bool withDecimals)
    {
        string amount = v.ToString(withDecimals ? "N2" : "N0", currency.NumberCulture());
        return currency.SymbolLeads()
            ? $"{currency.Symbol()}{amount}"
            : $"{amount} {currency.Symbol()}";
    }

    /// <summary>Grouped number with no currency symbol — for counts like pouches or cigarettes.</summary>
    public static string FormatNumber(decimal v, Currency currency) =>
        v.ToString("N0", currency.NumberCulture());
}
