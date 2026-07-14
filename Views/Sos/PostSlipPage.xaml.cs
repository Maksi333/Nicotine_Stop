using Microsoft.Maui.Controls.Shapes;
using Nicotine_Stop.Services;
using SnusStop.Core.Models;
using SnusStop.Core.Services;
using Trigger = SnusStop.Core.Models.Trigger;

namespace Nicotine_Stop.Views.Sos;

public partial class PostSlipPage : ContentPage
{
    private readonly AppState _state;
    private bool _built;

    public PostSlipPage(AppState state)
    {
        InitializeComponent();
        _state = state;
        StartBtn.Command = new Command(async () => await Navigation.PopModalAsync());
        Helpers.SafeArea.ApplyInsets(this, top: true, bottom: true);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_built) return;
        _built = true;
        Build();
    }

    private void Build()
    {
        var s = _state.StatsNow();
        string money = StatsCalculator.FormatMoney(s.Money, _state.Profile.Currency, false);
        int best = BestStreak();
        RetainedLabel.Text =
            $"Your {s.Days} total clean days and {money} are still yours. Best streak to beat: {best} days.";

        var counts = _state.Events
            .Where(e => e.Type == EventType.Slip && e.Trigger != Trigger.None)
            .GroupBy(e => e.Trigger)
            .Select(g => (Trigger: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(3)
            .ToList();

        PatternsHost.Children.Clear();
        if (counts.Count == 0)
        {
            PatternsHost.Children.Add(new Label
            {
                Text = "No trigger tagged yet — next time, note what set it off.",
                FontFamily = "NunitoBold",
                FontSize = 13,
            });
            TipLabel.Text = "One slip is a data point, not a defeat. Keep the SOS button one tap away.";
            return;
        }

        int max = counts.Max(c => c.Count);
        for (int i = 0; i < counts.Count; i++)
        {
            var (trigger, count) = counts[i];
            var color = i == 0 ? Color.FromArgb("#FF6B5E") : Color.FromArgb("#FFB627");
            PatternsHost.Children.Add(PatternRow($"{trigger.Emoji()} {trigger.Label()}", count, (double)count / max, color, i == 0));
        }
        TipLabel.Text = TipFor(counts[0].Trigger);
    }

    private View PatternRow(string label, int count, double frac, Color color, bool worst)
    {
        var track = new Border
        {
            HeightRequest = 14,
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#F1F5F2"),
            StrokeShape = new RoundRectangle { CornerRadius = 7 },
        };
        var fill = new Border { BackgroundColor = color, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 7 }, HorizontalOptions = LayoutOptions.Start };
        var fg = new Grid();
        fg.Add(fill);
        track.Content = fg;
        track.SizeChanged += (_, _) => fill.WidthRequest = Math.Max(8, track.Width * frac);

        var grid = new Grid { ColumnDefinitions = { new(86), new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 10 };
        var name = new Label { Text = label, FontFamily = "NunitoExtraBold", FontSize = 12.5, VerticalOptions = LayoutOptions.Center };
        name.SetDynamicResource(Label.TextColorProperty, "Ink");
        grid.Add(name, 0);
        grid.Add(track, 1);
        grid.Add(new Label { Text = count.ToString(), FontFamily = "NunitoBlack", FontSize = 12, TextColor = worst ? Color.FromArgb("#D94F44") : Color.FromArgb("#B07E14"), VerticalOptions = LayoutOptions.Center }, 2);
        return grid;
    }

    private int BestStreak()
    {
        var points = new List<DateTime> { _state.Profile.QuitUtc };
        points.AddRange(_state.Events.Where(e => e.Type == EventType.Slip).Select(e => e.TimestampUtc).OrderBy(t => t));
        points.Add(DateTime.UtcNow);
        int best = 0;
        for (int i = 1; i < points.Count; i++)
            best = Math.Max(best, (int)(points[i] - points[i - 1]).TotalDays);
        return best;
    }

    private static string TipFor(Trigger t) => t switch
    {
        Trigger.Party => "Most of your slips happen at parties. Next one: bring gum, hold a drink in your snus hand, and set the SOS widget on your home screen.",
        Trigger.Stress => "Stress is your pattern. Try the 4-7-8 breathing the moment tension rises — before the craving takes over.",
        Trigger.Coffee => "Coffee cues it. Pair your next cup with a glass of water and a 60-second game instead.",
        Trigger.AfterMeal => "After meals hit hardest. Plan a walk or gum for the 10 minutes right after eating.",
        Trigger.Boredom => "Boredom is the trigger. Keep a game or a reason card one tap away for the dead moments.",
        Trigger.FriendsUsing => "Friends using is tough. Tell one of them you're quitting — an ally in the room changes everything.",
        _ => "Notice the pattern, plan for it, and keep the SOS button close.",
    };
}
