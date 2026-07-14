using SQLite;

namespace Nicotine_Stop.Data;

// Row DTOs keep sqlite-net attributes out of SnusStop.Core (which stays persistence-free).

[Table("profile")]
public class ProfileRow
{
    [PrimaryKey] public int Id { get; set; } = 1;   // single-row table
    public string Name { get; set; } = "";

    // AddictionType. Added after 1.0: CreateTableAsync backfills it as 0 (Snus) on existing
    // installs, which is exactly the pre-upgrade behaviour.
    public int Addiction { get; set; }

    public DateTime QuitUtc { get; set; }
    public int PouchesPerDay { get; set; }
    public int PouchesPerCan { get; set; }
    public double CanPrice { get; set; }
    public int Currency { get; set; }
    public string MotivationsJson { get; set; } = "[]";
    public bool SplitSavings { get; set; }
    public bool NotifyMilestone { get; set; }
    public bool NotifyDaily { get; set; }
    public bool NotifyWeekly { get; set; }
    public bool OnboardingComplete { get; set; }
}

[Table("goals")]
public class GoalRow
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
    public string? PhotoPath { get; set; }
    public int SortOrder { get; set; }
    public bool Funded { get; set; }
    public int? FundedOnDay { get; set; }
    public int? FundedByPouches { get; set; }
}

[Table("events")]
public class EventRow
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public DateTime TimestampUtc { get; set; }
    public int Type { get; set; }
    public string? Source { get; set; }
    public int Trigger { get; set; }
    public string? Note { get; set; }
    public int XpDelta { get; set; }
}

/// <summary>Owns the on-device SQLite connection and lazy table creation.</summary>
public class AppDatabase
{
    private SQLiteAsyncConnection? _conn;
    private readonly string _path;

    public AppDatabase()
    {
        _path = Path.Combine(FileSystem.AppDataDirectory, "snusstop.db3");
    }

    public async Task<SQLiteAsyncConnection> ConnAsync()
    {
        if (_conn is not null) return _conn;

        var conn = new SQLiteAsyncConnection(_path,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await conn.CreateTableAsync<ProfileRow>();
        await conn.CreateTableAsync<GoalRow>();
        await conn.CreateTableAsync<EventRow>();
        _conn = conn;
        return conn;
    }
}
