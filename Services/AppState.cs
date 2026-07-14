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

    /// <summary>Raised whenever the underlying data changes (load, new event, profile edit).</summary>
    public event EventHandler? Changed;

    public AppState(IProfileRepository profiles, IEventRepository events)
    {
        _profiles = profiles;
        _events = events;
    }

    public async Task LoadAsync()
    {
        Profile = await _profiles.GetAsync() ?? new Profile();
        Events = await _events.AllAsync();
        LastSlipUtc = await _events.LastSlipAsync();
        TotalXp = Events.Sum(e => e.XpDelta);
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

    public async Task SaveProfileAsync(Profile profile)
    {
        Profile = profile;
        await _profiles.SaveAsync(profile);
        Raise();
    }

    private void Raise() => Changed?.Invoke(this, EventArgs.Empty);
}
