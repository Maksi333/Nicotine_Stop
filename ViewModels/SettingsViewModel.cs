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

    private AddictionCopy Copy => _state.Copy;

    public string QuitDateStr => _state.Profile.QuitUtc.ToLocalTime().ToString("dd MMM · HH:mm");
    public string UsageStr => $"{_state.Profile.PouchesPerDay}/day · {_state.Profile.PouchesPerCan} {Copy.PerContainerLabel}";
    public string PriceStr => StatsCalculator.FormatMoney(_state.Profile.CanPrice, _state.Profile.Currency, false);
    public string CurrencyStr => _state.Profile.Currency.Code();

    /// <summary>"Price per can" / "Price per pack".</summary>
    public string PriceLabel => Copy.PricePerContainerLabel;

    /// <summary>The row value for "What I quit": "Snus" / "Cigarettes".</summary>
    public string AddictionStr => _state.Profile.Addiction == AddictionType.Cigarettes ? "Cigarettes" : "Snus";

    [ObservableProperty] private bool milestone;
    [ObservableProperty] private bool daily;
    [ObservableProperty] private bool weekly;

    public void Refresh()
    {
        OnPropertyChanged(nameof(QuitDateStr));
        OnPropertyChanged(nameof(UsageStr));
        OnPropertyChanged(nameof(PriceStr));
        OnPropertyChanged(nameof(CurrencyStr));
        OnPropertyChanged(nameof(PriceLabel));
        OnPropertyChanged(nameof(AddictionStr));
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
