namespace SnusStop.Core.Models;

/// <summary>Kinds of entries in the local events log (single source of truth for history).</summary>
public enum EventType
{
    CheckIn,
    CravingWon,
    Slip,
    BadgeEarned,
    XpDelta,
    GoalFunded,
}
