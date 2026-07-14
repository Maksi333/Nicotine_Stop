using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Nicotine_Stop.Controls;
using Nicotine_Stop.Services;
using Nicotine_Stop.ViewModels;
using Nicotine_Stop.Views.Tabs;

namespace Nicotine_Stop.Views;

/// <summary>
/// The main app shell: a swappable content region for the four tabs plus the custom bottom nav
/// with the overhanging SOS button. Loads shared state and starts the clock on first appearance.
/// </summary>
public class MainTabsPage : ContentPage
{
    private readonly MainShellViewModel _shell;
    private readonly AppState _state;
    private readonly ClockService _clock;
    private readonly GoalsViewModel _goalsVm;
    private readonly IServiceProvider _services;
    private readonly NotificationService _notifications;
    private readonly WidgetUpdateService _widgets;
    private readonly View[] _tabs;
    private bool _loaded;

    public MainTabsPage(
        MainShellViewModel shell, AppState state, ClockService clock, GoalsViewModel goalsVm,
        IServiceProvider services, NotificationService notifications, WidgetUpdateService widgets,
        HomeView home, GoalsView goals, HealthView health, JourneyView journey)
    {
        _shell = shell;
        _state = state;
        _clock = clock;
        _goalsVm = goalsVm;
        _services = services;
        _notifications = notifications;
        _widgets = widgets;
        _state.Changed += (_, _) => _widgets.Update(_state.Profile, _state.StatsNow(), _state.CravingsWon);
        BindingContext = shell;

        _tabs = new View[] { home, goals, health, journey };

        var content = new Grid();
        foreach (var t in _tabs) content.Add(t);

        var nav = new BottomNavBar();
        nav.SetBinding(BottomNavBar.ActiveIndexProperty, new Binding(nameof(MainShellViewModel.CurrentTab)));
        nav.TabSelected += (_, index) => _shell.CurrentTab = index;
        nav.SosTapped += OnSos;

        var root = new Grid { RowDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        root.Add(content, 0, 0);
        root.Add(nav, 0, 1);
        Content = root;
        Helpers.SafeArea.ApplyInsets(this, top: true, bottom: true);

        _shell.PropertyChanged += OnShellChanged;
        UpdateTabs();
    }

    private void OnShellChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainShellViewModel.CurrentTab)) UpdateTabs();
    }

    private void UpdateTabs()
    {
        for (int i = 0; i < _tabs.Length; i++)
            _tabs[i].IsVisible = _shell.CurrentTab == i;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;

        await _state.LoadAsync();
        _clock.Start();
        await _goalsVm.LoadAsync();
        await _notifications.ApplyAllAsync(_state.Profile);
        _widgets.Update(_state.Profile, _state.StatsNow(), _state.CravingsWon);
    }

    private async void OnSos(object? sender, EventArgs e)
    {
        var sos = _services.GetRequiredService<Sos.SosTakeoverPage>();
        await Navigation.PushModalAsync(new NavigationPage(sos));
    }
}
