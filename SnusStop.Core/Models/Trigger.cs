namespace SnusStop.Core.Models;

/// <summary>Optional trigger tagged onto a slip (frame 1u chips).</summary>
public enum Trigger
{
    None,
    Party,
    Stress,
    Coffee,
    AfterMeal,
    Boredom,
    FriendsUsing,
}

public static class TriggerExtensions
{
    public static string Label(this Trigger t) => t switch
    {
        Trigger.Party => "Party",
        Trigger.Stress => "Stress",
        Trigger.Coffee => "Coffee",
        Trigger.AfterMeal => "After a meal",
        Trigger.Boredom => "Boredom",
        Trigger.FriendsUsing => "Friends using",
        _ => "",
    };

    public static string Emoji(this Trigger t) => t switch
    {
        Trigger.Party => "🍺",
        Trigger.Stress => "😤",
        Trigger.Coffee => "☕",
        Trigger.AfterMeal => "🍽️",
        Trigger.Boredom => "😴",
        Trigger.FriendsUsing => "👥",
        _ => "",
    };

    /// <summary>The chips shown on the slip screen, in design order (excludes None).</summary>
    public static IReadOnlyList<Trigger> Selectable { get; } = new[]
    {
        Trigger.Party, Trigger.Stress, Trigger.Coffee,
        Trigger.AfterMeal, Trigger.Boredom, Trigger.FriendsUsing,
    };
}
