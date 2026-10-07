using System.Text.Json;
using SnusStop.Core.Models;

namespace Nicotine_Stop.Data;

public interface IProfileRepository
{
    Task<Profile?> GetAsync();
    Task SaveAsync(Profile profile);
    Task DeleteAsync();
}

public interface IGoalRepository
{
    Task<List<GoalItem>> AllAsync();
    Task<int> UpsertAsync(GoalItem goal);
    Task DeleteAsync(int id);
}

public interface IEventRepository
{
    Task<List<EventLog>> AllAsync();
    Task AddAsync(EventLog e);
    Task<DateTime?> LastSlipAsync();
    Task<int> TotalXpAsync();
    Task ClearAsync();
}

public class ProfileRepository(AppDatabase db) : IProfileRepository
{
    public async Task<Profile?> GetAsync()
    {
        var conn = await db.ConnAsync();
        var row = await conn.FindAsync<ProfileRow>(1);
        return row is null ? null : Map(row);
    }

    public async Task SaveAsync(Profile p)
    {
        var conn = await db.ConnAsync();
        var row = new ProfileRow
        {
            Id = 1,
            Name = p.Name,
            Addiction = (int)p.Addiction,
            QuitUtc = p.QuitUtc,
            PouchesPerDay = p.PouchesPerDay,
            PouchesPerCan = p.PouchesPerCan,
            CanPrice = (double)p.CanPrice,
            Currency = (int)p.Currency,
            MotivationsJson = JsonSerializer.Serialize(p.Motivations),
            SplitSavings = p.SplitSavings,
            NotifyMilestone = p.NotifyMilestone,
            NotifyDaily = p.NotifyDaily,
            NotifyWeekly = p.NotifyWeekly,
            OnboardingComplete = p.OnboardingComplete,
        };
        await conn.InsertOrReplaceAsync(row);
    }

    public async Task DeleteAsync()
    {
        var conn = await db.ConnAsync();
        await conn.DeleteAsync<ProfileRow>(1);
    }

    private static Profile Map(ProfileRow r) => new()
    {
        Name = r.Name,
        Addiction = (AddictionType)r.Addiction,
        QuitUtc = DateTime.SpecifyKind(r.QuitUtc, DateTimeKind.Utc),
        PouchesPerDay = r.PouchesPerDay,
        PouchesPerCan = r.PouchesPerCan,
        CanPrice = (decimal)r.CanPrice,
        Currency = (Currency)r.Currency,
        Motivations = SafeList(r.MotivationsJson),
        SplitSavings = r.SplitSavings,
        NotifyMilestone = r.NotifyMilestone,
        NotifyDaily = r.NotifyDaily,
        NotifyWeekly = r.NotifyWeekly,
        OnboardingComplete = r.OnboardingComplete,
    };

    private static List<string> SafeList(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }
}

public class GoalRepository(AppDatabase db) : IGoalRepository
{
    public async Task<List<GoalItem>> AllAsync()
    {
        var conn = await db.ConnAsync();
        var rows = await conn.Table<GoalRow>().OrderBy(g => g.SortOrder).ToListAsync();
        return rows.Select(Map).ToList();
    }

    public async Task<int> UpsertAsync(GoalItem g)
    {
        var conn = await db.ConnAsync();
        var row = new GoalRow
        {
            Id = g.Id,
            Name = g.Name,
            Price = (double)g.Price,
            PhotoPath = g.PhotoPath,
            SortOrder = g.SortOrder,
            Funded = g.Funded,
            FundedOnDay = g.FundedOnDay,
            FundedByPouches = g.FundedByPouches,
        };
        if (row.Id == 0) await conn.InsertAsync(row);
        else await conn.UpdateAsync(row);
        return row.Id;
    }

    public async Task DeleteAsync(int id)
    {
        var conn = await db.ConnAsync();
        await conn.DeleteAsync<GoalRow>(id);
    }

    private static GoalItem Map(GoalRow r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Price = (decimal)r.Price,
        PhotoPath = r.PhotoPath,
        SortOrder = r.SortOrder,
        Funded = r.Funded,
        FundedOnDay = r.FundedOnDay,
        FundedByPouches = r.FundedByPouches,
    };
}

public class EventRepository(AppDatabase db) : IEventRepository
{
    public async Task<List<EventLog>> AllAsync()
    {
        var conn = await db.ConnAsync();
        var rows = await conn.Table<EventRow>().OrderBy(e => e.TimestampUtc).ToListAsync();
        return rows.Select(Map).ToList();
    }

    public async Task AddAsync(EventLog e)
    {
        var conn = await db.ConnAsync();
        await conn.InsertAsync(new EventRow
        {
            TimestampUtc = e.TimestampUtc,
            Type = (int)e.Type,
            Source = e.Source,
            Trigger = (int)e.Trigger,
            Note = e.Note,
            XpDelta = e.XpDelta,
            AmountSpent = (double)e.AmountSpent,
        });
    }

    public async Task<DateTime?> LastSlipAsync()
    {
        var conn = await db.ConnAsync();
        int slip = (int)EventType.Slip;
        var row = await conn.Table<EventRow>()
            .Where(e => e.Type == slip)
            .OrderByDescending(e => e.TimestampUtc)
            .FirstOrDefaultAsync();
        return row is null ? null : DateTime.SpecifyKind(row.TimestampUtc, DateTimeKind.Utc);
    }

    public async Task<int> TotalXpAsync()
    {
        var conn = await db.ConnAsync();
        var rows = await conn.Table<EventRow>().ToListAsync();
        return rows.Sum(r => r.XpDelta);
    }

    public async Task ClearAsync()
    {
        var conn = await db.ConnAsync();
        await conn.DeleteAllAsync<EventRow>();
    }

    private static EventLog Map(EventRow r) => new()
    {
        Id = r.Id,
        TimestampUtc = DateTime.SpecifyKind(r.TimestampUtc, DateTimeKind.Utc),
        Type = (EventType)r.Type,
        Source = r.Source,
        Trigger = (SnusStop.Core.Models.Trigger)r.Trigger,
        Note = r.Note,
        XpDelta = r.XpDelta,
        AmountSpent = (decimal)(r.AmountSpent ?? 0d),
    };
}
