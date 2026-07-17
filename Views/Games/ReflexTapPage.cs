using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using Nicotine_Stop.Services;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Views.Games;

public class ReflexTapPage : GameHostPage
{
    private readonly GameScoreStore _scores;

    private AbsoluteLayout _field = null!;
    private Label _scoreLabel = null!;
    private Label _bestLabel = null!;
    private Label _timerLabel = null!;
    private Grid _celebration = null!;
    private Label _celebrationScore = null!;
    private int _score;
    private int _seconds = 60;
    private IDispatcherTimer? _gameTimer;
    private IDispatcherTimer? _spawnTimer;
    private readonly Random _rng = new();
    private bool _ended;

    public ReflexTapPage(AppState state, IServiceProvider services, GameScoreStore scores) : base(state, services)
    {
        GameXp = 10;
        ActivityId = ActivityIds.Reflex;
        _scores = scores;
        Build();
        Start();
    }

    private void Build()
    {
        _scoreLabel = new Label { Text = "0", FontFamily = "NunitoBlack", FontSize = 17, TextColor = Color.FromArgb("#F2F1FA") };
        _bestLabel = new Label { Text = _scores.GetHighScore(GameScoreStore.ReflexTap).ToString(), FontFamily = "NunitoBlack", FontSize = 17, TextColor = Color.FromArgb("#FFD98A") };
        _timerLabel = new Label { Text = "01:00", FontFamily = "NunitoBlack", FontSize = 17, TextColor = Color.FromArgb("#F2F1FA") };

        var chips = new Grid
        {
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 10,
            Padding = new Thickness(0, 14, 0, 0),
        };
        chips.Add(Chip("🎯", "SCORE", _scoreLabel), 0);
        chips.Add(Chip("🏅", "BEST", _bestLabel), 1);
        chips.Add(Chip("⏱️", "TIME LEFT", _timerLabel), 2);

        _field = new AbsoluteLayout();
        // Margin keeps the play field from butting up against the chip row above it.
        var panel = new Border { StrokeThickness = 0, BackgroundColor = Color.FromArgb("#14FFFFFF"), StrokeShape = new RoundRectangle { CornerRadius = 20 }, Content = _field, Padding = 6, Margin = new Thickness(0, 14, 0, 0) };

        var outer = new Grid
        {
            Padding = new Thickness(22, 0, 22, 18),
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
        };
        outer.Add(Header("REFLEX TAP"), 0, 0);
        outer.Add(chips, 0, 1);
        outer.Add(panel, 0, 2);
        outer.Add(OkayFooter(), 0, 3);

        Content = new Grid { Children = { outer, BuildCelebration() } };
    }

    /// <summary>Full-bleed overlay shown only when the personal best is actually beaten.</summary>
    private Grid BuildCelebration()
    {
        _celebrationScore = new Label
        {
            FontFamily = "NunitoBlack",
            FontSize = 44,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
        };

        _celebration = new Grid
        {
            IsVisible = false,
            Opacity = 0,
            BackgroundColor = Color.FromArgb("#E6232140"),
            Children =
            {
                new Controls.ConfettiView(),
                new VerticalStackLayout
                {
                    Spacing = 2,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = "🏅", FontSize = 56, HorizontalTextAlignment = TextAlignment.Center },
                        new Label
                        {
                            Text = "NEW BEST!",
                            FontFamily = "NunitoBlack",
                            FontSize = 15,
                            CharacterSpacing = 2,
                            TextColor = Color.FromArgb("#FFD98A"),
                            HorizontalTextAlignment = TextAlignment.Center,
                            Margin = new Thickness(0, 12, 0, 0),
                        },
                        _celebrationScore,
                        new Label
                        {
                            Text = "taps — your fastest hands yet.",
                            FontFamily = "NunitoBold",
                            FontSize = 13.5,
                            TextColor = Color.FromArgb("#B9B4E3"),
                            HorizontalTextAlignment = TextAlignment.Center,
                        },
                    },
                },
            },
        };

        return _celebration;
    }

    /// <summary>
    /// A centered vertical stat tile: value on top, emoji + caption below. Vertical (not the old
    /// side-by-side row) so even "TIME LEFT" fits within a third of the screen without clipping.
    /// </summary>
    private static Border Chip(string emoji, string caption, Label value)
    {
        value.HorizontalOptions = LayoutOptions.Center;
        value.HorizontalTextAlignment = TextAlignment.Center;

        return new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb("#14FFFFFF"),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Padding = new Thickness(6, 12),
            Content = new VerticalStackLayout
            {
                Spacing = 3,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    value,
                    new Label
                    {
                        Text = $"{emoji} {caption}",
                        FontFamily = "NunitoExtraBold",
                        FontSize = 10,
                        TextColor = Color.FromArgb("#B9B4E3"),
                        HorizontalTextAlignment = TextAlignment.Center,
                        LineBreakMode = LineBreakMode.NoWrap,
                    },
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
                await EndRoundAsync();
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

    /// <summary>
    /// Time's up. A strictly higher score than the stored best is a new record — celebrate it and
    /// save it. Equalling or missing the best does neither.
    /// </summary>
    private async Task EndRoundAsync()
    {
        if (_scores.TrySetHighScore(GameScoreStore.ReflexTap, _score))
        {
            _bestLabel.Text = _score.ToString();
            await CelebrateNewBestAsync();
        }

        await WinAsync();
    }

    private async Task CelebrateNewBestAsync()
    {
        _celebrationScore.Text = _score.ToString();
        _celebration.IsVisible = true;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }

        await _celebration.FadeTo(1, 220, Easing.CubicOut);
        await Task.Delay(1600);
        await _celebration.FadeTo(0, 200, Easing.CubicIn);
        _celebration.IsVisible = false;
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
