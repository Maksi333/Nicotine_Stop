using System.Globalization;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Services;

/// <summary>
/// Persists each activity's last XP claim as a local calendar date in app preferences, so the
/// once-per-day cap survives a restart. Dates are stored round-trip-safe (yyyy-MM-dd), not as a
/// timestamp — the cap is about the day the user was in, never an elapsed duration.
/// </summary>
public sealed class PreferencesDailyXpStore : IDailyXpStore
{
    private const string Prefix = "dailyxp.";
    private const string Format = "yyyy-MM-dd";

    public DateTime? GetLastClaimedDate(string activityId)
    {
        string raw = Preferences.Default.Get(Prefix + activityId, string.Empty);
        if (string.IsNullOrEmpty(raw)) return null;

        return DateTime.TryParseExact(raw, Format, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    public void SetLastClaimedDate(string activityId, DateTime localDate) =>
        Preferences.Default.Set(Prefix + activityId, localDate.ToString(Format, CultureInfo.InvariantCulture));
}
