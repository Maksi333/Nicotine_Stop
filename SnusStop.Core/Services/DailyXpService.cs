namespace SnusStop.Core.Services;

/// <summary>Stable keys for every activity whose XP is capped to once per day.</summary>
public static class ActivityIds
{
    public const string Minesweeper = "game.minesweeper";
    public const string Memory = "game.memory";
    public const string Reflex = "game.reflex";
    public const string PouchPop = "game.pouchpop";
    public const string RemindMeWhy = "remind_me_why";
    public const string Breathe = "breathe";
}

/// <summary>
/// Persistence for the last local date an activity's XP was claimed. The app backs this with
/// Preferences; tests use an in-memory dictionary.
/// </summary>
public interface IDailyXpStore
{
    DateTime? GetLastClaimedDate(string activityId);
    void SetLastClaimedDate(string activityId, DateTime localDate);
}

public interface IDailyXpService
{
    bool CanClaimXp(string activityId);

    /// <summary>Claims the activity's XP for today. Returns the XP actually granted — 0 if it was
    /// already claimed on this local date, in which case no reward animation should be shown.</summary>
    int ClaimXp(string activityId, int amount);
}

/// <summary>
/// Grants an activity's XP at most once per <em>local</em> calendar day. The activity itself stays
/// freely repeatable — only the reward is capped, which is what stops XP farming.
/// </summary>
public sealed class DailyXpService : IDailyXpService
{
    private readonly IDailyXpStore _store;
    private readonly Func<DateTime> _localNow;

    /// <param name="localNow">Local wall-clock time. Injected so tests can cross midnight.</param>
    public DailyXpService(IDailyXpStore store, Func<DateTime>? localNow = null)
    {
        _store = store;
        _localNow = localNow ?? (() => DateTime.Now);
    }

    public bool CanClaimXp(string activityId) =>
        _store.GetLastClaimedDate(activityId)?.Date != _localNow().Date;

    public int ClaimXp(string activityId, int amount)
    {
        if (!CanClaimXp(activityId)) return 0;
        _store.SetLastClaimedDate(activityId, _localNow().Date);
        return amount;
    }
}
