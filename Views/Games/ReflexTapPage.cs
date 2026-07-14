using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using Nicotine_Stop.Services;

namespace Nicotine_Stop.Views.Games;

public class ReflexTapPage : GameHostPage
{
    private AbsoluteLayout _field = null!;
    private Label _scoreLabel = null!;
    private Label _timerLabel = null!;
    private int _score;
    private int _seconds = 60;
    private IDispatcherTimer? _gameTimer;
    private IDispatcherTimer? _spawnTimer;
    private readonly Random _rng = new();
    private bool _ended;

    public ReflexTapPage(AppState state, IServiceProvider services) : base(state, services)
    {
        GameXp = 10;
        Build();
        Start();
    }

    private void Build()
    {
        _scoreLabel = new Label { Text = "0", FontFamily = "NunitoBlack", FontSize = 17, TextColor = Color.FromArgb("#F2F1FA") };
        _timerLabel = new Label { Text = "01:00", FontFamily = "NunitoBlack", FontSize = 17, TextColor = Color.FromArgb("#F2F1FA") };

        var chips = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 10, Padding = new Thickness(0, 14, 0, 0) };
        chips.Add(Chip("🎯", "SCORE", _scoreLabel), 0);
        chips.Add(Chip("⏱️", "TIME LEFT", _timerLabel), 1);

        _field = new AbsoluteLayout();
        var panel = new Border { StrokeThickness = 0, BackgroundColor = Color.FromArgb("#14FFFFFF"), StrokeShape = new RoundRectangle { CornerRadius = 20 }, Content = _field, Padding = 6 };

        var outer = new Grid
        {
            Padding = new Thickness(22, 0, 22, 18),
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
        };
        outer.Add(Header("REFLEX TAP"), 0, 0);
        outer.Add(chips, 0, 1);
        outer.Add(panel, 0, 2);
        outer.Add(OkayFooter(), 0, 3);
        Content = outer;
    }

    private static Border Chip(string emoji, string caption, Label value)
    {
        return new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#14FFFFFF"),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Padding = new Thickness(14, 10),
            Content = new HorizontalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = emoji, FontSize = 16, VerticalOptions = LayoutOptions.Center },
                    new VerticalStackLayout { Children = { value, new Label { Text = caption, FontFamily = "NunitoExtraBold", FontSize = 10, TextColor = Color.FromArgb("#B9B4E3") } } },
                },
            },
        };
    }

    private void Start()
    {
        _seconds = 60;
        _score = 0;

        _gameTimer = Dispatcher.CreateTimer();
        _gameTimer.Interval = TimeSpan.FromSeconds(1);
        _gameTimer.Tick += async (_, _) =>
        {
            _seconds--;
            _timerLabel.Text = TimeSpan.FromSeconds(Math.Max(0, _seconds)).ToString(@"mm\:ss");
            if (_seconds <= 0)
            {
                Stop();
                await WinAsync();
            }
        };
        _gameTimer.Start();

        _spawnTimer = Dispatcher.CreateTimer();
        _spawnTimer.Interval = TimeSpan.FromMilliseconds(780);
        _spawnTimer.Tick += (_, _) => Spawn();
        _spawnTimer.Start();
    }

    private void Stop()
    {
        _ended = true;
        _gameTimer?.Stop();
        _spawnTimer?.Stop();
    }

    private void Spawn()
    {
        if (_ended || _field.Width <= 0) return;

        var colors = new[] { "#FFB627", "#8B7CF6", "#4C9EF5", "#2BD98A" };
        var dot = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb(colors[_rng.Next(colors.Length)]),
            StrokeShape = new RoundRectangle { CornerRadius = 30 },
            Opacity = 0,
        };
        double x = 0.03 + _rng.NextDouble() * 0.80;
        double y = 0.03 + _rng.NextDouble() * 0.82;
        AbsoluteLayout.SetLayoutFlags(dot, AbsoluteLayoutFlags.PositionProportional);
        AbsoluteLayout.SetLayoutBounds(dot, new Rect(x, y, 58, 58));

        bool tapped = false;
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            if (tapped) return;
            tapped = true;
            _score++;
            _scoreLabel.Text = _score.ToString();
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            await dot.ScaleTo(0, 80, Easing.CubicIn);
            if (_field.Contains(dot)) _field.Remove(dot);
        };
        dot.GestureRecognizers.Add(tap);

        _field.Add(dot);
        _ = dot.FadeTo(1, 120);
        _ = dot.ScaleTo(1, 120, Easing.CubicOut);

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(880), () =>
        {
            if (!tapped && _field.Contains(dot)) _field.Remove(dot);
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Stop();
    }
}
