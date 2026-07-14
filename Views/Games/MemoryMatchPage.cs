using Microsoft.Maui.Controls.Shapes;
using Nicotine_Stop.Services;
using SnusStop.Core.Games;

namespace Nicotine_Stop.Views.Games;

public class MemoryMatchPage : GameHostPage
{
    private MemoryMatchEngine _engine = new(8);
    private readonly List<Border> _borders = new();
    private readonly List<Label> _labels = new();
    private Label _timerLabel = null!;
    private IDispatcherTimer? _timer;
    private int _seconds = 120;
    private bool _busy;

    public MemoryMatchPage(AppState state, IServiceProvider services) : base(state, services)
    {
        GameXp = 10;
        Build();
        Start();
    }

    private void Build()
    {
        _timerLabel = new Label { Text = "02:00", FontFamily = "NunitoBlack", FontSize = 16, TextColor = Color.FromArgb("#F2F1FA"), HorizontalTextAlignment = TextAlignment.Center };
        var caption = new VerticalStackLayout
        {
            Spacing = 4,
            Padding = new Thickness(0, 12, 0, 0),
            Children =
            {
                _timerLabel,
                new Label { Text = "Flip two cards — find every pair.", FontFamily = "NunitoBold", FontSize = 13.5, TextColor = Color.FromArgb("#B9B4E3"), HorizontalTextAlignment = TextAlignment.Center },
            },
        };

        var grid = new Grid { RowSpacing = 8, ColumnSpacing = 8, VerticalOptions = LayoutOptions.Center };
        for (int i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        }
        grid.SizeChanged += (_, _) => { if (grid.Width > 0) grid.HeightRequest = grid.Width; };

        for (int i = 0; i < 16; i++)
        {
            int idx = i;
            var lbl = new Label { FontSize = 30, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            var border = new Border { StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 14 }, Content = lbl };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => OnCard(idx);
            border.GestureRecognizers.Add(tap);
            _borders.Add(border);
            _labels.Add(lbl);
            grid.Add(border, i % 4, i / 4);
        }

        var outer = new Grid
        {
            Padding = new Thickness(22, 0, 22, 18),
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
        };
        outer.Add(Header("MEMORY MATCH", (_, _) => Restart()), 0, 0);
        outer.Add(caption, 0, 1);
        outer.Add(grid, 0, 2);
        outer.Add(OkayFooter(), 0, 3);
        Content = outer;

        UpdateCards();
    }

    private void Start()
    {
        _timer?.Stop();
        _seconds = 120;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += async (_, _) =>
        {
            _seconds--;
            _timerLabel.Text = TimeSpan.FromSeconds(Math.Max(0, _seconds)).ToString(@"mm\:ss");
            if (_seconds <= 0)
            {
                _timer?.Stop();
                await WinAsync();
            }
        };
        _timer.Start();
    }

    private void Restart()
    {
        _engine = new MemoryMatchEngine(8);
        _busy = false;
        UpdateCards();
        Start();
    }

    private async void OnCard(int i)
    {
        if (_busy) return;
        var card = _engine.Cards[i];
        if (card.Matched || card.FaceUp) return;

        bool matched = _engine.Flip(i);
        UpdateCards();

        if (_engine.HasPendingMismatch)
        {
            _busy = true;
            await Task.Delay(750);
            _engine.ResetUnmatched();
            UpdateCards();
            _busy = false;
        }
        else if (matched && _engine.Won)
        {
            _timer?.Stop();
            await WinAsync();
        }
    }

    private void UpdateCards()
    {
        for (int i = 0; i < 16; i++)
        {
            var card = _engine.Cards[i];
            var border = _borders[i];
            var lbl = _labels[i];
            bool up = card.FaceUp || card.Matched;
            border.BackgroundColor = up ? Color.FromArgb("#2E2B52") : Color.FromArgb("#8B7CF6");
            lbl.Text = up ? card.Face : "";
            border.Opacity = card.Matched ? 0.55 : 1;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }
}
