namespace SnusStop.Core.Models;

/// <summary>A savings goal the user funds with the money they don't spend on pouches.</summary>
public class GoalItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }

    /// <summary>Local file path of a user-picked photo, or null for the striped placeholder slot.</summary>
    public string? PhotoPath { get; set; }

    public int SortOrder { get; set; }

    public bool Funded { get; set; }
    public int? FundedOnDay { get; set; }
    public int? FundedByPouches { get; set; }
}
