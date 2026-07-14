namespace SnusStop.Core.Models;

/// <summary>
/// The complete set of user-facing terminology for one addiction type: the units, what they're
/// bought in, and the health-recovery body copy for every milestone.
///
/// This is the single place addiction-specific wording lives. To support another substance, add a
/// <see cref="AddictionCopy"/> instance here — do not add <c>if (type == …)</c> checks in views or
/// view models.
/// </summary>
public sealed record AddictionCopy
{
    public required AddictionType Type { get; init; }

    /// <summary>Lowercase name of the habit: "snus" / "cigarettes". Reads as "Quit snus."</summary>
    public required string Noun { get; init; }

    /// <summary>The consumable unit, singular: "pouch" / "cigarette".</summary>
    public required string Unit { get; init; }

    /// <summary>The consumable unit, plural: "pouches" / "cigarettes".</summary>
    public required string Units { get; init; }

    /// <summary>What units are bought in, singular: "can" / "pack".</summary>
    public required string Container { get; init; }

    /// <summary>What units are bought in, plural: "cans" / "packs".</summary>
    public required string Containers { get; init; }

    /// <summary>Home stat-card caption: "pouches skipped" / "cigarettes not smoked".</summary>
    public required string SkippedCaption { get; init; }

    /// <summary>Noun phrase for prose: "skipped pouches" / "cigarettes never smoked".</summary>
    public required string SkippedNoun { get; init; }

    /// <summary>Terse caption for tight spaces like the home-screen widget: "skipped" / "not smoked".</summary>
    public required string SkippedShort { get; init; }

    /// <summary>Health-timeline body copy, keyed by <see cref="Milestone.Key"/>.</summary>
    public required IReadOnlyDictionary<string, string> HealthBodies { get; init; }

    public string HealthBody(string milestoneKey) =>
        HealthBodies.TryGetValue(milestoneKey, out var body) ? body : "";

    // Labels assembled from the terms above, so a new addiction gets these for free.
    public string UnitsPerDayLabel => $"{Capitalized(Units)} per day";
    public string UnitsPerContainerLabel => $"{Capitalized(Units)} per {Container}";
    public string PricePerContainerLabel => $"Price per {Container}";
    public string ContainerCostQuestion => $"What does a {Container} cost?";
    public string PerContainerLabel => $"per {Container}";
    public string QuitHeadline => $"Quit {Noun}.";

    private static string Capitalized(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    public static AddictionCopy For(AddictionType type) =>
        type == AddictionType.Cigarettes ? Cigarettes : Snus;

    public static readonly AddictionCopy Snus = new()
    {
        Type = AddictionType.Snus,
        Noun = "snus",
        Unit = "pouch",
        Units = "pouches",
        Container = "can",
        Containers = "cans",
        SkippedCaption = "pouches skipped",
        SkippedNoun = "skipped pouches",
        SkippedShort = "skipped",
        HealthBodies = new Dictionary<string, string>
        {
            ["20m"] = "Heart rate and blood pressure settle back to normal.",
            ["72h"] = "Nicotine is fully out of your body. The worst is behind you.",
            ["1w"] = "Cravings peak — and start fading in strength and number.",
            ["2w"] = "Circulation and gum blood flow improving where pouches sat.",
            ["1m"] = "Gums begin healing. Energy and sleep improve.",
            ["3m"] = "Focus and mood stabilize as dopamine rebalances.",
            ["1y"] = "Risk to your heart drops significantly. Full trophy unlock. 🏆",
        },
    };

    public static readonly AddictionCopy Cigarettes = new()
    {
        Type = AddictionType.Cigarettes,
        Noun = "cigarettes",
        Unit = "cigarette",
        Units = "cigarettes",
        Container = "pack",
        Containers = "packs",
        SkippedCaption = "cigarettes not smoked",
        SkippedNoun = "cigarettes never smoked",
        SkippedShort = "not smoked",
        HealthBodies = new Dictionary<string, string>
        {
            ["20m"] = "Heart rate and blood pressure settle back to normal.",
            ["72h"] = "Nicotine is fully out of your body. Your airways relax — breathing already feels easier.",
            ["1w"] = "Cravings peak — and start fading in strength and number.",
            ["2w"] = "Circulation improves and lung function climbs. Stairs and walking feel easier.",
            ["1m"] = "Coughing and shortness of breath ease as your lungs start clearing themselves out.",
            ["3m"] = "Taste and smell sharpen. Focus and mood stabilize as dopamine rebalances.",
            ["1y"] = "Your risk of heart disease is now around half a smoker's. Full trophy unlock. 🏆",
        },
    };
}
