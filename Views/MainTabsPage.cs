using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Nicotine_Stop.Controls;
using Nicotine_Stop.Services;
using Nicotine_Stop.ViewModels;
using Nicotine_Stop.Views.Tabs;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

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
    private readonly BadgeToast _toast = new();
    private readonly Queue<(string Emoji, string Title, int Xp)> _toastQueue = new();
    private bool _draining;
    private bool _readyForToasts;
    private DateTime _lastBadgeCheckUtc = DateTime.MinValue;
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
        _state.Changed += (_, _) => _widgets.Update(_state.Profile, _state.LastSlipUtc, _state.CravingsWon);
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
        root.Add(_toast, 0, 0);         // last, so the badge banner draws over the tab content
        Content = root;

        // Keep content clear of the status bar and the gesture bar. .NET 10 flipped ContentPage's
        // default to None (edge-to-edge) on Android, so this has to be asked for explicitly.
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container);

        _shell.PropertyChanged += OnShellChanged;
        UpdateTabs();

        _state.BadgesEarned += OnBadgesEarned;

        // Every data change is a chance a badge just unlocked (check-in, craving won, slip). The
        // sync guards its own re-entrancy, so the events it writes don't retrigger it.
        _state.Changed += (_, _) => SyncBadges();

        // Day-based badges cross while the app sits open, with no data change to hang off. A minute
        // is the coarsest tick that still feels immediate for a day boundary.
        _clock.Tick += (_, _) =>
        {
            if (DateTime.UtcNow - _lastBadgeCheckUtc < TimeSpan.FromMinutes(1)) return;
            _lastBadgeCheckUtc = DateTime.UtcNow;
            SyncBadges();
        };

        WidgetNavigation.SosRequested += OnSosRequested;
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

        if (!_loaded)
        {
            _loaded = true;

            // Normally already loaded by RootPage before this page was built; this covers the
            // onboarding hand-off and is a no-op when it isn't needed.
            await _state.EnsureLoadedAsync();
            _clock.Start();

            // A widget SOS tap during cold start set a pending flag before this page existed. Open
            // the takeover now — before the notification prompt — so it isn't stuck behind that dialog.
            if (WidgetNavigation.ConsumePending())
                await OpenSosAsync();

            await _goalsVm.LoadAsync();
            await _notifications.ApplyAllAsync(_state.Profile);
            _widgets.Update(_state.Profile, _state.LastSlipUtc, _state.CravingsWon);

            // Only now may banners show. Onboarding hands over a freshly saved profile, so the very
            // first badge payout is already queued by the time this method starts — and the line
            // above puts the OS notification dialog on screen, which no in-app guard can see.
            _readyForToasts = true;
        }

        // Every appearance, not just the first: coming back from a modal is exactly when a banner
        // held back by DrainToastsAsync gets its chance. Kept last in the method because everything
        // above can put a modal or the OS notification prompt on screen, and a banner behind either
        // of those is a banner nobody sees.
        await _state.SyncBadgesAsync();
        await DrainToastsAsync();
    }

    // Fire-and-forget: a failed write is retried by the next trigger, and a badge banner is never
    // worth taking the app down for.
    private async void SyncBadges()
    {
        try { await _state.SyncBadgesAsync(); }
        catch { /* retried on the next trigger */ }
    }

    private void OnBadgesEarned(object? sender, IReadOnlyList<Badge> badges)
    {
        // A backdated quit date can unlock half a dozen at once; a six-banner parade is worse than
        // one honest summary.
        if (badges.Count >= 3)
            _toastQueue.Enqueue(("🏆", $"{badges.Count} badges unlocked", badges.Count * XpService.BadgeXp));
        else
            foreach (var b in badges)
                _toastQueue.Enqueue((b.Emoji, $"{b.Label} unlocked", XpService.BadgeXp));

        MainThread.BeginInvokeOnMainThread(async () => await DrainToastsAsync());
    }

    private async Task DrainToastsAsync()
    {
        if (_draining || !_readyForToasts) return;
        _draining = true;
        try
        {
            // Never fire behind a modal — a craving takeover is the last place to celebrate at.
            // OnAppearing drains whatever is left once the modal closes.
            while (_toastQueue.Count > 0 && Navigation.ModalStack.Count == 0)
            {
                var t = _toastQueue.Dequeue();
                await _toast.ShowAsync(t.Emoji, t.Title, t.Xp);
            }
        }
        finally { _draining = false; }
    }

    private async void OnSos(object? sender, EventArgs e) => await OpenSosAsync();

    private void OnSosRequested()
    {
        // Fired from a widget deep link (may be off the UI thread). Marshal to UI; only act once
        // loaded — the cold-start case is picked up by ConsumePending() in OnAppearing.
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_loaded && WidgetNavigation.ConsumePending())
                await OpenSosAsync();
        });
    }

    private async Task OpenSosAsync()
    {
        if (Navigation.ModalStack.Count > 0) return;   // SOS (or another modal) already showing
        var sos = _services.GetRequiredService<Sos.SosTakeoverPage>();
        await Navigation.PushModalAsync(new NavigationPage(sos));
    }
}
