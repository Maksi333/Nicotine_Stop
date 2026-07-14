namespace SnusStop.Core.Models;

/// <summary>One entry in the append-only local events log.</summary>
public class EventLog
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public EventType Type { get; set; }

    /// <summary>For CravingWon: "breathe" | "game" | "reasons". Null otherwise.</summary>
    public string? Source { get; set; }

    /// <summary>For Slip only.</summary>
    public Trigger Trigger { get; set; }

    public string? Note { get; set; }

    /// <summary>XP awarded by this event (0 if none).</summary>
    public int XpDelta { get; set; }

    public static EventLog CheckIn(DateTime nowUtc) =>
        new() { TimestampUtc = nowUtc, Type = EventType.CheckIn, XpDelta = 10 };

    public static EventLog CravingWon(DateTime nowUtc, string source, int xp) =>
        new() { TimestampUtc = nowUtc, Type = EventType.CravingWon, Source = source, XpDelta = xp };

    public static EventLog Slip(DateTime nowUtc, Trigger trigger, string? note) =>
        new() { TimestampUtc = nowUtc, Type = EventType.Slip, Trigger = trigger, Note = note };
}
