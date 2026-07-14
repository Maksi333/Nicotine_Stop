using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace SnusStop.Core.Tests;

public class CsvExporterTests
{
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
