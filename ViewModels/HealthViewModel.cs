using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Nicotine_Stop.Services;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.ViewModels;

public partial class HealthMilestoneVM : ObservableObject
{
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public bool IsDone { get; init; }
    public bool IsInProgress { get; init; }
    public bool IsLocked { get; init; }
    public bool IsLast { get; init; }
    public double ProgressFraction { get; init; }

    public string StateLabel => IsDone ? "DONE" : IsInProgress ? "IN PROGRESS" : "";
}

public partial class HealthViewModel : ObservableObject
{
    private readonly AppState _state;
    private readonly ClockService _clock;

    public HealthViewModel(AppState state, ClockService clock)
    {
        _state = state;
        _clock = clock;
        _clock.Tick += (_, _) => Refresh();
        _state.Changed += (_, _) => Refresh();
        Build();
    }

    public ObservableCollection<HealthMilestoneVM> Milestones { get; } = new();

    public double Recovery { get; private set; }
    public string RecoveryPercentStr => $"{(int)Math.Round(Recovery * 100)}%";

    private void Refresh()
    {
        Build();
        OnPropertyChanged(nameof(Recovery));
        OnPropertyChanged(nameof(RecoveryPercentStr));
    }

    private void Build()
    {
        var elapsed = _clock.NowUtc - _state.CleanSinceUtc;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
        Recovery = StatsCalculator.RecoveryPercent(elapsed);

        Milestones.Clear();
        var all = SnusStop.Core.Models.Milestones.Health;
        bool inProgressAssigned = false;
        for (int i = 0; i < all.Count; i++)
        {
            var m = all[i];
            bool done = elapsed >= m.At;
            bool inProgress = false;
            double frac = 0;
            if (!done && !inProgressAssigned)
            {
                inProgress = true;
                inProgressAssigned = true;
                var prevAt = i == 0 ? TimeSpan.Zero : all[i - 1].At;
                frac = Math.Clamp((elapsed - prevAt).TotalSeconds / (m.At - prevAt).TotalSeconds, 0, 1);
            }
            Milestones.Add(new HealthMilestoneVM
            {
                Title = m.Title,
                Body = _state.Copy.HealthBody(m.Key),
                IsDone = done,
                IsInProgress = inProgress,
                IsLocked = !done && !inProgress,
                IsLast = i == all.Count - 1,
                ProgressFraction = frac,
            });
        }
    }
}
