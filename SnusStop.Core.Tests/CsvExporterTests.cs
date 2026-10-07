using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace SnusStop.Core.Tests;

public class CsvExporterTests
{
    [Fact]
    public void Export_preserves_slip_spending_with_invariant_decimals_and_escaped_note()
    {
        var p = new Profile();
        var slip = EventLog.Slip(DateTime.UtcNow, Trigger.Stress, "Coffee, then a slip", 45.50m);
        var csv = CsvExporter.Build(p, Array.Empty<GoalItem>(), new[] { slip });
        Assert.Contains("TimestampUtc,Type,Source,Trigger,XpDelta,Note,AmountSpent", csv);
        Assert.Contains(",Slip,\"\",Stress,0,\"Coffee, then a slip\",45.50", csv);
    }

    [Fact]
    public void Builds_sections_and_escapes_special_characters()
    {
        var p = new Profile { Name = "Mik,el", QuitUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) };
        var goals = new[] { new GoalItem { Name = "PS5 \"Pro\"", Price = 549m } };
        var events = new[] { EventLog.CheckIn(new DateTime(2026, 7, 12, 8, 0, 0, DateTimeKind.Utc)) };

        var csv = CsvExporter.Build(p, goals, events);

        Assert.Contains("PROFILE", csv);
        Assert.Contains("GOALS", csv);
        Assert.Contains("EVENTS", csv);
        Assert.Contains("\"Mik,el\"", csv);              // comma-bearing field is quoted
        Assert.Contains("\"PS5 \"\"Pro\"\"\"", csv);      // embedded quotes doubled
        Assert.Contains("CheckIn", csv);
    }
}
