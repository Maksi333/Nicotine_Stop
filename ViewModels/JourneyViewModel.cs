using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Nicotine_Stop.Services;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.ViewModels;

public class BadgeCellVM
{
    public string Emoji { get; init; } = "";
    public string Label { get; init; } = "";
    public bool Earned { get; init; }
    public bool Newest { get; init; }
    public bool Locked => !Earned;
}

public partial class JourneyViewModel : ObservableObject
{
    private readonly AppState _state;

    public JourneyViewModel(AppState state)
    {
        _state = state;
        _state.Changed += (_, _) => Build();
        Build();
    }

    public ObservableCollection<BadgeCellVM> Badges { get; } = new();
    public ObservableCollection<DayCellVM> Calendar { get; } = new();

    public int EarnedCount { get; private set; }
    public string BadgeCountLabel => $"{EarnedCount} of {SnusStop.Core.Models.Badges.Count} badges";
    public string MonthTitle => DateTime.Now.ToString("MMMM yyyy");
    public string InsightLine { get; private set; } = "";

    private void Build()
    {
        var stats = _state.StatsNow();

        var (earned, locked) = BadgeService.Split(_state.Profile, _state.Events, stats);
        EarnedCount = earned.Count;
        var newest = earned.LastOrDefault();

        Badges.Clear();
        foreach (var b in earned)
            Badges.Add(new BadgeCellVM { Emoji = b.Emoji, Label = b.Label, Earned = true, Newest = b == newest });
        foreach (var b in locked)
            Badges.Add(new BadgeCellVM { Emoji = b.Emoji, Label = b.Label, Earned = false });

        BuildCalendar();

        OnPropertyChanged(nameof(EarnedCount));
        OnPropertyChanged(nameof(BadgeCountLabel));
        OnPropertyChanged(nameof(MonthTitle));
        OnPropertyChanged(nameof(InsightLine));
    }

    private void BuildCalendar()
    {
        Calendar.Clear();
        var now = DateTime.Now;
        int year = now.Year, month = now.Month;
        var today = now.Date;
        var quitDate = _state.Profile.QuitUtc.ToLocalTime().Date;

        var cravingDays = _state.Events.Where(e => e.Type == EventType.CravingWon)
            .Select(e => e.TimestampUtc.ToLocalTime().Date).ToHashSet();
        var slipDays = _state.Events.Where(e => e.Type == EventType.Slip)
            .Select(e => e.TimestampUtc.ToLocalTime().Date).ToHashSet();

        var first = new DateTime(year, month, 1);
        int lead = ((int)first.DayOfWeek + 6) % 7;
        for (int i = 0; i < lead; i++) Calendar.Add(new DayCellVM(null));

        int days = DateTime.DaysInMonth(year, month);
        for (int d = 1; d <= days; d++)
        {
            var date = new DateTime(year, month, d);
            string status =
                slipDays.Contains(date) ? "slip" :
                date == today ? "today" :
                date >= quitDate && date <= today ? "clean" :
                "none";
            Calendar.Add(new DayCellVM(d) { Status = status, HasCraving = cravingDays.Contains(date) });
        }

        int cravingsThisMonth = cravingDays.Count(dt => dt.Year == year && dt.Month == month);
        InsightLine = cravingsThisMonth > 0
            ? $"💪 {cravingsThisMonth} craving{(cravingsThisMonth == 1 ? "" : "s")} battled this month — all won."
            : "💪 Every clean day is a win. Keep it going.";
    }
}
