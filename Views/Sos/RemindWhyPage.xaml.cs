using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using Nicotine_Stop.Data;
using Nicotine_Stop.Services;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Views.Sos;

public partial class RemindWhyPage : ContentPage
{
    private readonly AppState _state;
    private readonly IGoalRepository _goals;
    private readonly IServiceProvider _services;
    private bool _completed;
    private bool _built;

    public RemindWhyPage(AppState state, IGoalRepository goals, IServiceProvider services)
    {
        InitializeComponent();
        _state = state;
        _goals = goals;
        _services = services;
        OkayBtn.Command = new Command(async () => await OkayAsync());
        Helpers.SafeArea.ApplyInsets(this, top: true, bottom: true);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_built) return;
        _built = true;
        await BuildAsync();
    }

    private async Task BuildAsync()
    {
        ReasonsHost.Children.Clear();

        var motivations = _state.Profile.Motivations;
        if (motivations.Count == 0)
            motivations = new List<string> { "I want to feel in control again.", "I want my health back." };

        foreach (var m in motivations.Take(3))
            ReasonsHost.Children.Add(ReasonCard(EmojiFor(m), m));

        var goals = await _goals.AllAsync();
        decimal money = _state.StatsNow().Money;
        string sym = _state.Profile.Currency.Symbol();
        decimal canPrice = _state.Profile.CanPrice;

        var next = goals.OrderBy(g => g.SortOrder).FirstOrDefault(g => money < g.Price && g.Price > 0);
        if (next is not null)
        {
            decimal alloc = Math.Min(money, next.Price);
            decimal toGo = next.Price - alloc;
            double frac = next.Price > 0 ? Math.Clamp((double)(alloc / next.Price), 0, 1) : 0;
            ReasonsHost.Children.Add(GoalCard(
                next.Name,
                $"You're {StatsCalculator.FormatMoney(toGo, false)} {sym} away",
                frac,
                $"One can skipped = {StatsCalculator.FormatMoney(canPrice, false)} {sym} closer. This craving is worth money."));
        }
        else
        {
            ReasonsHost.Children.Add(SavingsCard(
                $"One can skipped = {StatsCalculator.FormatMoney(canPrice, false)} {sym} closer to whatever you want. This craving is worth money."));
        }
    }

    private static string EmojiFor(string m)
    {
        var s = m.ToLowerInvariant();
        if (s.Contains("health") || s.Contains("heart")) return "❤️";
        if (s.Contains("money") || s.Contains("save")) return "💰";
        if (s.Contains("focus") || s.Contains("brain")) return "🧠";
        if (s.Contains("fit") || s.Contains("run") || s.Contains("10k")) return "🏃";
        if (s.Contains("family") || s.Contains("kid")) return "👨‍👩‍👧";
        return "💭";
    }

    private static View ReasonCard(string emoji, string text)
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) },
            ColumnSpacing = 14,
        };
        grid.Add(new Label { Text = emoji, FontSize = 26, VerticalOptions = LayoutOptions.Center }, 0);
        grid.Add(new Label
        {
            Text = $"“{text}”",
            FontFamily = "NunitoExtraBold",
            FontSize = 16.5,
            TextColor = Color.FromArgb("#ECF6F0"),
            VerticalOptions = LayoutOptions.Center,
        }, 1);
        return new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#14FFFFFF"),
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Padding = new Thickness(20, 18),
            Content = grid,
        };
    }

    private static View GoalCard(string name, string away, double frac, string line)
    {
        var track = new Border
        {
            HeightRequest = 14,
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#E9F3EC"),
            StrokeShape = new RoundRectangle { CornerRadius = 7 },
        };
        var fill = new Border { BackgroundColor = Color.FromArgb("#14B36B"), StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 7 }, HorizontalOptions = LayoutOptions.Start };
        var fillGrid = new Grid();
        fillGrid.Add(fill);
        track.Content = fillGrid;
        track.SizeChanged += (_, _) => fill.WidthRequest = track.Width * frac;

        var header = new Grid { ColumnDefinitions = { new(GridLength.Star) } };
        header.Add(new VerticalStackLayout
        {
            Spacing = 1,
            Children =
            {
                new Label { Text = name, FontFamily = "NunitoBlack", FontSize = 15, TextColor = Color.FromArgb("#17322B") },
                new Label { Text = away, FontFamily = "NunitoExtraBold", FontSize = 12, TextColor = Color.FromArgb("#0E8A50") },
            },
        }, 0);

        return new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Padding = 18,
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    header,
                    track,
                    new Label { Text = line, FontFamily = "NunitoExtraBold", FontSize = 12.5, TextColor = Color.FromArgb("#5E7A6F") },
                },
            },
        };
    }

    private static View SavingsCard(string line)
    {
        return new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Padding = new Thickness(20, 18),
            Content = new Label { Text = line, FontFamily = "NunitoExtraBold", FontSize = 15, TextColor = Color.FromArgb("#17322B") },
        };
    }

    private async void OnBack(object? sender, EventArgs e) => await Navigation.PopAsync();

    private async Task OkayAsync()
    {
        if (_completed) return;
        _completed = true;
        await _state.AddEventAsync(EventLog.CravingWon(DateTime.UtcNow, "reasons", XpService.CravingXp));
        var celebrate = _services.GetRequiredService<CravingDefeatedPage>();
        celebrate.Init(XpService.CravingXp);
        await Navigation.PushAsync(celebrate);
    }
}
