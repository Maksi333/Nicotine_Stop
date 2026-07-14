using Nicotine_Stop.Data;
using Nicotine_Stop.Services;
using Nicotine_Stop.ViewModels;
using SnusStop.Core.Models;

namespace Nicotine_Stop.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;
    private readonly AppState _state;
    private readonly NotificationService _notifications;
    private readonly CsvExportService _csv;
    private readonly IEventRepository _events;
    private bool _suppress;

    public SettingsPage(SettingsViewModel vm, AppState state, NotificationService notifications, CsvExportService csv, IEventRepository events)
    {
        InitializeComponent();
        _vm = vm;
        _state = state;
        _notifications = notifications;
        _csv = csv;
        _events = events;
        BindingContext = vm;

        QuitDatePicker.DateSelected += (_, _) => ApplyQuit();
        QuitTimePicker.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time)) ApplyQuit(); };
        Helpers.SafeArea.ApplyInsets(this, top: true, bottom: true);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _suppress = true;
        var local = _state.Profile.QuitUtc.ToLocalTime();
        QuitDatePicker.Date = local.Date;
        QuitTimePicker.Time = local.TimeOfDay;
        _suppress = false;
    }

    private async void ApplyQuit()
    {
        if (_suppress) return;
        var date = QuitDatePicker.Date is DateTime d ? d.Date : DateTime.Today;
        var time = QuitTimePicker.Time ?? new TimeSpan(21, 30, 0);
        var local = date + time;
        _state.Profile.QuitUtc = local.ToUniversalTime();
        await SaveAsync();
    }

    private async void OnEditUsage(object? sender, EventArgs e)
    {
        var p = _state.Profile;
        var d = await DisplayPromptAsync("Pouches per day", "On a typical day", initialValue: p.PouchesPerDay.ToString(), keyboard: Keyboard.Numeric);
        if (int.TryParse(d, out var pd) && pd > 0) p.PouchesPerDay = pd;
        var c = await DisplayPromptAsync("Pouches per can", "Check the label", initialValue: p.PouchesPerCan.ToString(), keyboard: Keyboard.Numeric);
        if (int.TryParse(c, out var pc) && pc > 0) p.PouchesPerCan = pc;
        await SaveAsync();
    }

    private async void OnEditPrice(object? sender, EventArgs e)
    {
        var p = _state.Profile;
        var v = await DisplayPromptAsync("Price per can", "What one can costs", initialValue: ((int)p.CanPrice).ToString(), keyboard: Keyboard.Numeric);
        if (decimal.TryParse(v, out var price) && price > 0) { p.CanPrice = price; await SaveAsync(); }
    }

    private async void OnEditCurrency(object? sender, EventArgs e)
    {
        var choice = await DisplayActionSheet("Currency", "Cancel", null, "DKK", "SEK", "NOK", "EUR");
        if (choice is null or "Cancel") return;
        _state.Profile.Currency = CurrencyExtensions.Parse(choice);
        await SaveAsync();
    }

    private async void OnExport(object? sender, EventArgs e)
    {
        try { await _csv.ExportAsync(); }
        catch { await DisplayAlert("Export failed", "Couldn't create the export file.", "OK"); }
    }

    private async void OnReset(object? sender, EventArgs e)
    {
        bool ok = await DisplayAlert("Reset all progress",
            "This clears your check-ins, cravings, slips and XP, and restarts your quit from now. Goals are kept. This can't be undone.",
            "Reset", "Cancel");
        if (!ok) return;

        await _events.ClearAsync();
        _state.Profile.QuitUtc = DateTime.UtcNow;
        await _state.SaveProfileAsync(_state.Profile);
        await _state.LoadAsync();
        _vm.Refresh();
        await Navigation.PopModalAsync();
    }

    private async void OnClose(object? sender, EventArgs e) => await Navigation.PopModalAsync();

    private async Task SaveAsync()
    {
        await _state.SaveProfileAsync(_state.Profile);
        await _notifications.ApplyAllAsync(_state.Profile);
        _vm.Refresh();
    }
}
