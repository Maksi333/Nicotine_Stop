using SnusStop.Core.Models;

namespace SnusStop.Core.Services;

/// <summary>XP awards and level thresholds. Level 4 "Fresh Air" at 340/500 matches frame 1h.</summary>
public static class XpService
{
    public record Level(int Number, string Name, int Floor, int Ceil);

    // Standard award amounts.
    public const int CheckInXp = 10;
    public const int CravingXp = 15;
    public const int GameMinesweeperXp = 15;
    public const int GameDefaultXp = 10;
    public const int ChecklistXp = 20;
    public const int MilestoneXp = 50;
    public const int BadgeXp = 100;

    // Cumulative XP floors → level. Chosen so level 4 spans [500,1000): 840 total XP = 340/500 in-level.
    private static readonly (int Floor, string Name)[] Table =
    {
        (0,    "Getting Started"),
        (100,  "Clearing Out"),
        (250,  "Finding Rhythm"),
        (500,  "Fresh Air"),
        (1000, "Clear Headed"),
        (1750, "Unstoppable"),
        (2750, "Free"),
    };

    public static int Award(EventType type, string? source) => type switch
    {
        EventType.CheckIn => CheckInXp,
        EventType.CravingWon => CravingXp,
        _ => 0,
    };

    public static Level ForXp(int xp)
    {
        int idx = 0;
        for (int i = 0; i < Table.Length; i++)
            if (xp >= Table[i].Floor) idx = i;
        int ceil = idx + 1 < Table.Length ? Table[idx + 1].Floor : Table[idx].Floor + 1000;
        return new Level(idx + 1, Table[idx].Name, Table[idx].Floor, ceil);
    }

    /// <summary>(xp earned within the current level, total xp the level spans).</summary>
    public static (int InLevel, int Span) LevelProgress(int xp)
    {
        var l = ForXp(xp);
        return (xp - l.Floor, l.Ceil - l.Floor);
    }
}
