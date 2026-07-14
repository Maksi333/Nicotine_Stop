using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nicotine_Stop.Data;
using Nicotine_Stop.Services;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.ViewModels;

public class GoalCardVM
{
    public GoalItem Model { get; init; } = new();
    public string Name { get; init; } = "";
    public string PriceStr { get; init; } = "";
    public string? PhotoPath { get; init; }
    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
    public double ProgressFraction { get; init; }
    public string PercentStr { get; init; } = "";
    public string StatusStr { get; init; } = "";
    public Color BarColor { get; init; } = Color.FromArgb("#14B36B");
    public bool PercentIsBlue { get; init; }
    public string TrophyCaption { get; init; } = "";
}

public partial class GoalsViewModel : ObservableObject
{
    private readonly IGoalRepository _goals;
    private readonly AppState _state;
    private List<GoalItem> _loaded = new();

    /// <summary>Raised when the view should present the goal editor (null = new goal).</summary>
    public event Action<GoalItem?>? EditRequested;

    public GoalsViewModel(IGoalRepository goals, AppState state)
    {
        _goals = goals;
        _state = state;
    }

    public ObservableCollection<GoalCardVM> Goals { get; } = new();
    public ObservableCollection<GoalCardVM> Trophies { get; } = new();

    [ObservableProperty] private bool isEmpty = true;
    [ObservableProperty] private bool splitSavings;

    public string SavedStr => StatsCalculator.FormatMoney(_state.StatsNow().Money, false);
    public string CurrencySymbol => _state.Profile.Currency.Symbol();
    public string SavedIntStr => StatsCalculator.FormatMoney(_state.StatsNow().Money, false);
    public bool HasTrophies => Trophies.Count > 0;

    public async Task LoadAsync()
    {
        _loaded = await _goals.AllAsync();
        SplitSavings = _state.Profile.SplitSavings;
        Recompute();
    }

    private void Recompute()
    {
        var profile = _state.Profile;
        decimal money = _state.StatsNow().Money;
        decimal perPouch = StatsCalculator.PerPouch(profile);
        decimal perDay = StatsCalculator.PerDayCost(profile);
        string sym = profile.Currency.Symbol();

        Goals.Clear();
        Trophies.Clear();

        var ordered = _loaded.OrderBy(g => g.SortOrder).ToList();
        decimal remaining = money;
        int activeCount = SplitSavings ? Math.Max(1, ordered.Count(g => money < g.Price || g.Price == 0)) : 1;
        bool firstActiveAssigned = false;

        foreach (var g in ordered)
        {
            decimal alloc;
            if (SplitSavings)
                alloc = Math.Min(g.Price, money / Math.Max(1, ordered.Count));
            else
            {
                alloc = Math.Min(g.Price, Math.Max(0, remaining));
                remaining -= alloc;
            }

            double frac = g.Price > 0 ? Math.Clamp((double)(alloc / g.Price), 0, 1) : 0;
            bool funded = alloc >= g.Price && g.Price > 0;

            if (funded)
            {
                int pouches = perPouch > 0 ? (int)(g.Price / perPouch) : 0;
                int day = perDay > 0 ? (int)Math.Ceiling(g.Price / perDay) : 0;
                Trophies.Add(new GoalCardVM
                {
                    Model = g,
                    Name = g.Name,
                    PriceStr = $"{StatsCalculator.FormatMoney(g.Price, false)} {sym}",
                    TrophyCaption = $"Funded on day {day}. Paid for by {pouches} skipped pouches.",
                });
                continue;
            }

            bool isActive = SplitSavings || !firstActiveAssigned;
            if (!SplitSavings) firstActiveAssigned = true;

            decimal toGo = g.Price - alloc;
            Goals.Add(new GoalCardVM
            {
                Model = g,
                Name = g.Name,
                PriceStr = $"{StatsCalculator.FormatMoney(g.Price, false)} {sym}",
                PhotoPath = g.PhotoPath,
                ProgressFraction = frac,
                PercentStr = $"{(frac * 100):F0}% funded",
                StatusStr = isActive ? $"{StatsCalculator.FormatMoney(toGo, false)} {sym} to go" : "next in line",
                BarColor = isActive ? Color.FromArgb("#14B36B") : Color.FromArgb("#4C9EF5"),
                PercentIsBlue = !isActive,
            });
        }

        IsEmpty = ordered.Count == 0;
        OnPropertyChanged(nameof(SavedStr));
        OnPropertyChanged(nameof(SavedIntStr));
        OnPropertyChanged(nameof(CurrencySymbol));
        OnPropertyChanged(nameof(HasTrophies));
    }

    partial void OnSplitSavingsChanged(bool value)
    {
        if (_state.Profile.SplitSavings != value)
        {
            _state.Profile.SplitSavings = value;
            _ = _state.SaveProfileAsync(_state.Profile);
        }
        Recompute();
    }

    [RelayCommand]
    private void AddGoal() => EditRequested?.Invoke(null);

    [RelayCommand]
    private void EditGoal(GoalCardVM card) => EditRequested?.Invoke(card.Model);
}
