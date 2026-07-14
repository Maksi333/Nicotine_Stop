using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nicotine_Stop.Data;
using Nicotine_Stop.Services;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.ViewModels;

public partial class GoalCardVM : ObservableObject
{
    public GoalItem Model { get; init; } = new();
    public string Name { get; init; } = "";
    public string PriceStr { get; init; } = "";
    public string? PhotoPath { get; init; }
    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
    public string TrophyCaption { get; init; } = "";

    // Funding moves with the savings total, so these tick rather than being fixed at load.
    [ObservableProperty] private double progressFraction;
    [ObservableProperty] private string percentStr = "";
    [ObservableProperty] private string statusStr = "";
    [ObservableProperty] private Color barColor = Color.FromArgb("#14B36B");
    [ObservableProperty] private bool percentIsBlue;
}

public partial class GoalsViewModel : ObservableObject
{
    private readonly IGoalRepository _goals;
    private readonly AppState _state;
    private readonly ClockService _clock;
    private List<GoalItem> _loaded = new();
    private Stats _stats;

    /// <summary>Raised when the view should present the goal editor (null = new goal).</summary>
    public event Action<GoalItem?>? EditRequested;

    public GoalsViewModel(IGoalRepository goals, AppState state, ClockService clock)
    {
        _goals = goals;
        _state = state;
        _clock = clock;
        _stats = _state.StatsAt(_clock.NowUtc);

        // Savings accrue every second. Without this the page froze at its load-time figure and
        // drifted away from Home, which is what made the total look wrong.
        _clock.Tick += (_, _) => Refresh();
        _state.Changed += (_, _) => Rebuild();
    }

    public ObservableCollection<GoalCardVM> Goals { get; } = new();
    public ObservableCollection<GoalCardVM> Trophies { get; } = new();

    [ObservableProperty] private bool isEmpty = true;
    [ObservableProperty] private bool splitSavings;

    /// <summary>
    /// The one money-saved figure, taken from the same <see cref="AppState"/> stats Home reads and
    /// sampled on the same tick, so the two screens cannot disagree. Do not round or re-derive it
    /// anywhere else — that divergence was the bug.
    /// </summary>
    private decimal MoneySaved => _stats.Money;

    private Currency Currency => _state.Profile.Currency;

    /// <summary>Header pill. Formatted exactly as Home's money stat, decimals and symbol included.</summary>
    public string SavedStr => StatsCalculator.FormatMoney(MoneySaved, Currency, true);

    /// <summary>Whole-unit form, used only in the empty-state prose where decimals read badly.</summary>
    public string SavedIntStr => StatsCalculator.FormatMoney(MoneySaved, Currency, false);

    public bool HasTrophies => Trophies.Count > 0;

    // Empty-state suggestion chips. The amounts are examples, so they follow the user's currency.
    public string Suggestion1 => $"🎮 New game · {StatsCalculator.FormatMoney(449m, Currency, false)}";
    public string Suggestion2 => $"👟 Sneakers · {StatsCalculator.FormatMoney(900m, Currency, false)}";
    public string Suggestion3 => $"✈️ Weekend trip · {StatsCalculator.FormatMoney(2500m, Currency, false)}";

    public async Task LoadAsync()
    {
        _loaded = await _goals.AllAsync();
        SplitSavings = _state.Profile.SplitSavings;
        Rebuild();
    }

    private sealed record Alloc(GoalItem Goal, decimal Amount, bool Funded, bool IsActive);

    /// <summary>
    /// Splits the savings across goals. Off = top-down (fill the first goal, spill into the next);
    /// on = an equal share each.
    /// </summary>
    private List<Alloc> Allocate(decimal money)
    {
        var ordered = _loaded.OrderBy(g => g.SortOrder).ToList();
        var result = new List<Alloc>(ordered.Count);
        decimal remaining = money;
        bool firstActiveTaken = false;

        foreach (var g in ordered)
        {
            decimal amount;
            if (SplitSavings)
            {
                amount = Math.Min(g.Price, money / Math.Max(1, ordered.Count));
            }
            else
            {
                amount = Math.Min(g.Price, Math.Max(0, remaining));
                remaining -= amount;
            }

            bool funded = amount >= g.Price && g.Price > 0;
            bool isActive = false;
            if (!funded)
            {
                isActive = SplitSavings || !firstActiveTaken;
                if (!SplitSavings) firstActiveTaken = true;
            }

            result.Add(new Alloc(g, amount, funded, isActive));
        }

        return result;
    }

    /// <summary>Full rebuild of both collections. Needed when the goals themselves change.</summary>
    private void Rebuild()
    {
        _stats = _state.StatsAt(_clock.NowUtc);
        var profile = _state.Profile;
        decimal perPouch = StatsCalculator.PerPouch(profile);
        decimal perDay = StatsCalculator.PerDayCost(profile);
        var cur = Currency;

        Goals.Clear();
        Trophies.Clear();

        foreach (var a in Allocate(MoneySaved))
        {
            var g = a.Goal;

            if (a.Funded)
            {
                int pouches = perPouch > 0 ? (int)(g.Price / perPouch) : 0;
                int day = perDay > 0 ? (int)Math.Ceiling(g.Price / perDay) : 0;
                Trophies.Add(new GoalCardVM
                {
                    Model = g,
                    Name = g.Name,
                    PriceStr = StatsCalculator.FormatMoney(g.Price, cur, false),
                    TrophyCaption = $"Funded on day {day}. Paid for by {pouches} {_state.Copy.SkippedNoun}.",
                });
                continue;
            }

            var card = new GoalCardVM
            {
                Model = g,
                Name = g.Name,
                PriceStr = StatsCalculator.FormatMoney(g.Price, cur, false),
                PhotoPath = g.PhotoPath,
            };
            Apply(card, a, cur);
            Goals.Add(card);
        }

        IsEmpty = _loaded.Count == 0;
        RaiseMoney();
        OnPropertyChanged(nameof(HasTrophies));
    }

    /// <summary>
    /// Per-second refresh. Re-points the existing cards at the new savings total instead of
    /// rebuilding the collections, so the list doesn't churn once a second. A goal crossing its
    /// price is the one case that needs a real rebuild — it has to move to the trophy shelf.
    /// </summary>
    private void Refresh()
    {
        _stats = _state.StatsAt(_clock.NowUtc);
        var allocs = Allocate(MoneySaved);

        bool fundedSetChanged = allocs.Count(a => a.Funded) != Trophies.Count;
        if (fundedSetChanged)
        {
            Rebuild();
            return;
        }

        var cur = Currency;
        foreach (var a in allocs.Where(a => !a.Funded))
        {
            var card = Goals.FirstOrDefault(c => c.Model.Id == a.Goal.Id);
            if (card is not null) Apply(card, a, cur);
        }

        RaiseMoney();
    }

    private static void Apply(GoalCardVM card, Alloc a, Currency cur)
    {
        double frac = a.Goal.Price > 0 ? Math.Clamp((double)(a.Amount / a.Goal.Price), 0, 1) : 0;
        card.ProgressFraction = frac;
        card.PercentStr = $"{frac * 100:F0}% funded";
        card.StatusStr = a.IsActive
            ? $"{StatsCalculator.FormatMoney(a.Goal.Price - a.Amount, cur, false)} to go"
            : "next in line";
        card.BarColor = Color.FromArgb(a.IsActive ? "#14B36B" : "#4C9EF5");
        card.PercentIsBlue = !a.IsActive;
    }

    private void RaiseMoney()
    {
        OnPropertyChanged(nameof(SavedStr));
        OnPropertyChanged(nameof(SavedIntStr));
        OnPropertyChanged(nameof(Suggestion1));
        OnPropertyChanged(nameof(Suggestion2));
        OnPropertyChanged(nameof(Suggestion3));
    }

    partial void OnSplitSavingsChanged(bool value)
    {
        if (_state.Profile.SplitSavings != value)
        {
            _state.Profile.SplitSavings = value;
            _ = _state.SaveProfileAsync(_state.Profile);
        }
        Rebuild();
    }

    [RelayCommand]
    private void AddGoal() => EditRequested?.Invoke(null);

    [RelayCommand]
    private void EditGoal(GoalCardVM card) => EditRequested?.Invoke(card.Model);
}
