using Nicotine_Stop.Data;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Services;

/// <summary>Builds the CSV export and hands it to the system share sheet.</summary>
public class CsvExportService
{
    private readonly AppState _state;
    private readonly IGoalRepository _goals;
    private readonly IEventRepository _events;

    public CsvExportService(AppState state, IGoalRepository goals, IEventRepository events)
    {
        _state = state;
        _goals = goals;
        _events = events;
    }

    public async Task ExportAsync()
    {
        var goals = await _goals.AllAsync();
        var events = await _events.AllAsync();
        var csv = CsvExporter.Build(_state.Profile, goals, events);

        var path = Path.Combine(FileSystem.CacheDirectory, "puffy-export.csv");
        await File.WriteAllTextAsync(path, csv);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Puffy data export",
            File = new ShareFile(path),
        });
    }
}
