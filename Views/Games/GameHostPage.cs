using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using Nicotine_Stop.Services;
using Nicotine_Stop.Views.Sos;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Views.Games;

/// <summary>Shared chrome + win/celebration flow for the distraction mini-games.</summary>
public abstract class GameHostPage : ContentPage
{
    protected readonly AppState State;
    protected readonly IServiceProvider Services;
    protected int GameXp = 10;

    /// <summary>Key this game claims its once-per-day XP under (see <see cref="ActivityIds"/>).</summary>
    protected string ActivityId = "";

    private bool _awarded;

    protected GameHostPage(AppState state, IServiceProvider services)
    {
        State = state;
        Services = services;
        BackgroundColor = Color.FromArgb("#232140");
        NavigationPage.SetHasNavigationBar(this, false);
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container);
    }

    protected Grid Header(string title, EventHandler<TappedEventArgs>? restart = null)
    {
        var g = new Grid
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            Padding = new Thickness(0, 10, 0, 0),
        };
        g.Add(Chip("‹", async (_, _) => await Navigation.PopAsync()), 0);
        g.Add(new Label
        {
            Text = title,
            FontFamily = "NunitoBlack",
            FontSize = 14,
            CharacterSpacing = 1,
            TextColor = Color.FromArgb("#B9B4E3"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        }, 1);
        if (restart is not null) g.Add(Chip("↺", restart), 2);
        return g;
    }

    protected static Border Chip(string glyph, EventHandler<TappedEventArgs> handler)
    {
        var b = new Border
        {
            WidthRequest = 38,
            HeightRequest = 38,
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#1AFFFFFF"),
            StrokeShape = new RoundRectangle { CornerRadius = 19 },
            Content = new Label { Text = glyph, FontFamily = "NunitoExtraBold", FontSize = 17, TextColor = Color.FromArgb("#F2F1FA"), HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center },
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += handler;
        b.GestureRecognizers.Add(tap);
        return b;
    }

    protected Label OkayFooter(string text = "The craving passed — I'm okay")
    {
        var lbl = new Label
        {
            Text = text + " ›",
            FontFamily = "NunitoExtraBold",
            FontSize = 12.5,
            TextColor = Color.FromArgb("#B9B4E3"),
            HorizontalTextAlignment = TextAlignment.Center,
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await WinAsync();
        lbl.GestureRecognizers.Add(tap);
        return lbl;
    }

    protected async Task WinAsync()
    {
        if (_awarded) return;
        _awarded = true;

        // XP is capped to the first win of each local day; the craving-won event is logged either
        // way, so replays still credit the streak, badges and calendar — they just earn 0 XP.
        int xp = Services.GetRequiredService<IDailyXpService>().ClaimXp(ActivityId, GameXp);

        await State.AddEventAsync(EventLog.CravingWon(DateTime.UtcNow, "game", xp));
        var celebrate = Services.GetRequiredService<CravingDefeatedPage>();
        celebrate.Init(xp);
        await Navigation.PushAsync(celebrate);
    }
}
