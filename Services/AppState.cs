using Nicotine_Stop.Data;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Services;

/// <summary>
/// Shared, in-memory view of the user's saved state: profile, events log, last slip, XP. Loaded once
/// when the main app opens; the tab view models read live stats off it and log events through it.
/// </summary>
public class AppState
{
    private readonly IProfileRepository _profiles;
    private readonly IEventRepository _events;

    public Profile Profile { get; private set; } = new();
    public List<EventLog> Events { get; private set; } = new();
    public DateTime? LastSlipUtc { get; private set; }
    public int TotalXp { get; private set; }

    private const string BaselineKey = "badge_xp_baseline_v1";
    private bool _syncingBadges;

    /// <summary>Raised whenever the underlying data changes (load, new event, profile edit).</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Raised with the badges just paid out, so the UI can toast them. Never raised for the one-time
    /// baseline — those badges are marked paid without paying, and there is nothing to celebrate.
    /// </summary>
    public event EventHandler<IReadOnlyList<Badge>>? BadgesEarned;

    public AppState(IProfileRepository profiles, IEventRepository events)
    {
        _profiles = profiles;
        _events = events;
    }

    /// <summary>True once the saved state has been read; until then the stats are placeholders.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// Loads only if nothing has been read yet. Call this before showing any page that renders
    /// stats: view models compute from <see cref="Profile"/> in their constructors, so a page built
    /// ahead of the load would render the placeholder profile for a frame.
    /// </summary>
    public Task EnsureLoadedAsync() => IsLoaded ? Task.CompletedTask : LoadAsync();

    public async Task LoadAsync()
    {
        Profile = await _profiles.GetAsync() ?? new Profile();
        Events = await _events.AllAsync();
        LastSlipUtc = await _events.LastSlipAsync();
        TotalXp = Events.Sum(e => e.XpDelta);
        IsLoaded = true;
        Raise();
    }

    /// <summary>
    /// Terminology for whatever the user is quitting. Every screen reads its units and copy from
    /// here, so switching addiction in Settings re-labels the whole app.
    /// </summary>
    public AddictionCopy Copy => AddictionCopy.For(Profile.Addiction);

    public Stats StatsAt(DateTime nowUtc) => StatsCalculator.Compute(Profile, nowUtc, LastSlipUtc);
    public Stats StatsNow() => StatsAt(DateTime.UtcNow);

    public int CravingsWon => Events.Count(e => e.Type == EventType.CravingWon);

    public bool CheckedInToday()
    {
        var today = DateTime.Now.Date;
        return Events.Any(e => e.Type == EventType.CheckIn && e.TimestampUtc.ToLocalTime().Date == today);
    }

    public async Task AddEventAsync(EventLog e)
    {
        await _events.AddAsync(e);
        Events.Add(e);
        if (e.Type == EventType.Slip) LastSlipUtc = e.TimestampUtc;
        TotalXp += e.XpDelta;
        Raise();
    }

    /// <summary>
    /// Pays <see cref="XpService.BadgeXp"/> for every badge that is earned but not yet recorded in
    /// the log. Safe to call as often as you like — the log is the record, so a badge pays once.
    ///
    /// The loop is not paranoia: a payout counts toward the level, which can cross the level-5
    /// threshold and unlock the "level5" badge, which then pays on the next pass. Three passes is
    /// more than that can ever need.
    ///
    /// On the first run in an install that already has history, this instead records the
    /// currently-earned badges at 0 XP: they are marked paid so they never pay later, but the user
    /// gets nothing for progress made before the rule existed. A fresh install reaches this with an
    /// empty log, so nothing is suppressed and every badge pays as it lands — including the ones a
    /// backdated quit date unlocks straight away.
    /// </summary>
    public async Task SyncBadgesAsync()
    {
        if (_syncingBadges || !IsLoaded) return;
        _syncingBadges = true;
        try
        {
            bool baseline = !Preferences.Default.Get(BaselineKey, false) && Events.Count > 0;
            var granted = new List<Badge>();

            for (int pass = 0; pass < 3; pass++)
            {
                var pending = BadgeAwardService.Pending(Profile, Events, StatsNow());
                if (pending.Count == 0) break;

                foreach (var b in pending)
                    await AddEventAsync(EventLog.BadgeEarned(
                        DateTime.UtcNow, b.Key, baseline ? 0 : XpService.BadgeXp));

                if (!baseline) granted.AddRange(pending);
            }

            Preferences.Default.Set(BaselineKey, true);

            if (granted.Count > 0) BadgesEarned?.Invoke(this, granted);
        }
        finally { _syncingBadges = false; }
    }

    public async Task SaveProfileAsync(Profile profile)
    {
        Profile = profile;
        await _profiles.SaveAsync(profile);
        Raise();
    }

    private void Raise() => Changed?.Invoke(this, EventArgs.Empty);
}
