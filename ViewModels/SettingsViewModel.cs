using CommunityToolkit.Mvvm.ComponentModel;
using Nicotine_Stop.Services;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppState _state;
    private readonly NotificationService _notifications;
    private bool _suppress;

    public SettingsViewModel(AppState state, NotificationService notifications)
    {
        _state = state;
        _notifications = notifications;
        _suppress = true;
        Milestone = _state.Profile.NotifyMilestone;
        Daily = _state.Profile.NotifyDaily;
        Weekly = _state.Profile.NotifyWeekly;
        _suppress = false;
    }

    public string QuitDateStr => _state.Profile.QuitUtc.ToLocalTime().ToString("dd MMM · HH:mm");
    public string UsageStr => $"{_state.Profile.PouchesPerDay}/day · {_state.Profile.PouchesPerCan} per can";
    public string PriceStr => $"{StatsCalculator.FormatMoney(_state.Profile.CanPrice, false)} {_state.Profile.Currency.Symbol()}";
    public string CurrencyStr => _state.Profile.Currency.Code();

    [ObservableProperty] private bool milestone;
    [ObservableProperty] private bool daily;
    [ObservableProperty] private bool weekly;

    public void Refresh()
    {
        OnPropertyChanged(nameof(QuitDateStr));
        OnPropertyChanged(nameof(UsageStr));
        OnPropertyChanged(nameof(PriceStr));
        OnPropertyChanged(nameof(CurrencyStr));
    }

    partial void OnMilestoneChanged(bool value)
    {
        _state.Profile.NotifyMilestone = value;
        Persist();
    }

    partial void OnDailyChanged(bool value)
    {
        _state.Profile.NotifyDaily = value;
        Persist();
    }

    partial void OnWeeklyChanged(bool value)
    {
        _state.Profile.NotifyWeekly = value;
        Persist();
    }

    private async void Persist()
    {
        if (_suppress) return;
        await _state.SaveProfileAsync(_state.Profile);
        await _notifications.ApplyAllAsync(_state.Profile);
    }
}
