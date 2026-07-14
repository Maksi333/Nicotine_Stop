using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nicotine_Stop.Services;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly AppState _state;
    private readonly ClockService _clock;
    private Stats _stats;

    public HomeViewModel(AppState state, ClockService clock)
    {
        _state = state;
        _clock = clock;
        _stats = _state.StatsAt(_clock.NowUtc);
        _clock.Tick += (_, _) => RefreshLive();
        _state.Changed += (_, _) => RefreshAll();
    }

    public void RefreshAll()
    {
        _stats = _state.StatsAt(_clock.NowUtc);
        RaiseAll();
    }

    private void RefreshLive()
    {
        _stats = _state.StatsAt(_clock.NowUtc);
        RaiseLive();
    }

    public string GreetingName => string.IsNullOrWhiteSpace(_state.Profile.Name) ? "there" : _state.Profile.Name;
    public string CurrencySymbol => _state.Profile.Currency.Symbol();

    public string Dd => _stats.Days.ToString();
    public string Hh => _stats.Hours.ToString("D2");
    public string Mm => _stats.Min.ToString("D2");
    public string Ss => _stats.Sec.ToString("D2");
    public string Ticker => $"{Hh}:{Mm}:{Ss}";

    public string MoneyStr => StatsCalculator.FormatMoney(_stats.Money, true);
    public string PouchesAvoidedStr => _stats.PouchesAvoided.ToString();
    public string StreakStr => _stats.CurrentStreak.ToString();
    public double RingProgress => _stats.RingProgress;
    public string MilestoneTitle => _stats.Next?.Title ?? "1 year";
    public string MilestoneLeftLabel => StatsCalculator.DaysUntilNextLabel(_stats);

    private XpService.Level Level => XpService.ForXp(_state.TotalXp);
    public int LevelNumber => Level.Number;
    public string LevelLabel => $"Level {Level.Number} · {Level.Name}";
    public string XpText
    {
        get { var (i, s) = XpService.LevelProgress(_state.TotalXp); return $"{i} / {s} XP"; }
    }
    public double XpFraction
    {
        get { var (i, s) = XpService.LevelProgress(_state.TotalXp); return s > 0 ? (double)i / s : 0; }
    }

    public bool CheckedIn => _state.CheckedInToday();

    [RelayCommand]
    private async Task CheckInAsync()
    {
        if (_state.CheckedInToday()) return;
        await _state.AddEventAsync(EventLog.CheckIn(DateTime.UtcNow));
    }

    private void RaiseLive()
    {
        OnPropertyChanged(nameof(Dd));
        OnPropertyChanged(nameof(Hh));
        OnPropertyChanged(nameof(Mm));
        OnPropertyChanged(nameof(Ss));
        OnPropertyChanged(nameof(Ticker));
        OnPropertyChanged(nameof(MoneyStr));
        OnPropertyChanged(nameof(PouchesAvoidedStr));
        OnPropertyChanged(nameof(StreakStr));
        OnPropertyChanged(nameof(RingProgress));
        OnPropertyChanged(nameof(MilestoneTitle));
        OnPropertyChanged(nameof(MilestoneLeftLabel));
    }

    private void RaiseAll()
    {
        RaiseLive();
        OnPropertyChanged(nameof(GreetingName));
        OnPropertyChanged(nameof(CurrencySymbol));
        OnPropertyChanged(nameof(LevelNumber));
        OnPropertyChanged(nameof(LevelLabel));
        OnPropertyChanged(nameof(XpText));
        OnPropertyChanged(nameof(XpFraction));
        OnPropertyChanged(nameof(CheckedIn));
    }
}
