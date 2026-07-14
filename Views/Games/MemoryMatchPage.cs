using Microsoft.Maui.Controls.Shapes;
using Nicotine_Stop.Services;
using SnusStop.Core.Games;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Views.Games;

public class MemoryMatchPage : GameHostPage
{
    /// <summary>A selectable board. Rows fall out of Pairs·2 ÷ Cols.</summary>
    private sealed record BoardSize(string Name, string Detail, int Pairs, int Cols, int Seconds)
    {
        public int Cards => Pairs * 2;
        public int Rows => Cards / Cols;
    }

    private static readonly BoardSize[] Sizes =
    {
        new("Small", "4 × 4 · 8 pairs", 8, 4, 120),
        new("Medium", "4 × 6 · 12 pairs", 12, 4, 180),
        new("Large", "6 × 6 · 18 pairs", 18, 6, 240),
    };

    /// <summary>Seconds every round opens with, all cards face-up.</summary>
    private const int PreviewSeconds = 5;

    private const double CardSpacing = 8;

    private BoardSize _size = Sizes[0];
    private MemoryMatchEngine _engine = new(8);
    private readonly List<Border> _borders = new();
    private readonly List<Label> _labels = new();

    private Label _statusLabel = null!;
    private Label _hintLabel = null!;
    private Grid _board = null!;
    private Grid _boardHost = null!;
    private VerticalStackLayout _chooser = null!;
    private Grid _content = null!;

    private IDispatcherTimer? _timer;
    private CancellationTokenSource? _round;
    private int _seconds;
    private bool _busy;

    /// <summary>True while the opening preview is on screen: every card shows, no taps count.</summary>
    private bool _previewing;

    public MemoryMatchPage(AppState state, IServiceProvider services) : base(state, services)
    {
        GameXp = 10;
        ActivityId = ActivityIds.Memory;
        Build();
    }

    private void Build()
    {
        _statusLabel = new Label
        {
            Text = "Pick a size",
            FontFamily = "NunitoBlack",
            FontSize = 16,
            TextColor = Color.FromArgb("#F2F1FA"),
            HorizontalTextAlignment = TextAlignment.Center,
        };
        _hintLabel = new Label
        {
            Text = "Bigger boards take longer — good for a stubborn craving.",
            FontFamily = "NunitoBold",
            FontSize = 13.5,
            TextColor = Color.FromArgb("#B9B4E3"),
            HorizontalTextAlignment = TextAlignment.Center,
        };

        var caption = new VerticalStackLayout
        {
            Spacing = 4,
            Padding = new Thickness(0, 12, 0, 0),
            Children = { _statusLabel, _hintLabel },
        };

        _chooser = BuildChooser();

        _board = new Grid { RowSpacing = CardSpacing, ColumnSpacing = CardSpacing };
        _boardHost = new Grid { IsVisible = false, Children = { _board } };
        _boardHost.SizeChanged += (_, _) => LayoutBoard();

        _content = new Grid { Children = { _chooser, _boardHost } };

        var outer = new Grid
        {
            Padding = new Thickness(22, 0, 22, 18),
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
        };
        outer.Add(Header("MEMORY MATCH", (_, _) => Restart()), 0, 0);
        outer.Add(caption, 0, 1);
        outer.Add(_content, 0, 2);
        outer.Add(OkayFooter(), 0, 3);
        Content = outer;
    }

    private VerticalStackLayout BuildChooser()
    {
        var stack = new VerticalStackLayout { Spacing = 12, VerticalOptions = LayoutOptions.Center };

        foreach (var size in Sizes)
        {
            var card = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#8B7CF6"),
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                Padding = new Thickness(20, 16),
                Content = new VerticalStackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        new Label { Text = size.Name, FontFamily = "NunitoBlack", FontSize = 17, TextColor = Colors.White },
                        new Label { Text = size.Detail, FontFamily = "NunitoBold", FontSize = 12.5, TextColor = Color.FromArgb("#E4E0FB") },
                    },
                },
            };

            var chosen = size;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => StartRound(chosen);
            card.GestureRecognizers.Add(tap);

            stack.Add(card);
        }

        return stack;
    }

    /// <summary>Rebuilds the card grid for the chosen size and deals a fresh shuffle.</summary>
    private void BuildBoard()
    {
        _borders.Clear();
        _labels.Clear();
        _board.Children.Clear();
        _board.RowDefinitions.Clear();
        _board.ColumnDefinitions.Clear();

        for (int c = 0; c < _size.Cols; c++)
            _board.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (int r = 0; r < _size.Rows; r++)
            _board.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        // Faces shrink as the board grows, so scale the glyph with the card.
        double fontSize = _size.Cols >= 6 ? 22 : 30;

        for (int i = 0; i < _size.Cards; i++)
        {
            int idx = i;
            var lbl = new Label
            {
                FontSize = fontSize,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };
            var border = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Content = lbl,
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => OnCard(idx);
            border.GestureRecognizers.Add(tap);

            _borders.Add(border);
            _labels.Add(lbl);
            _board.Add(border, i % _size.Cols, i / _size.Cols);
        }

        LayoutBoard();
    }

    /// <summary>
    /// Sizes the board to whichever of width/height runs out first, so a 6×6 still fits — and
    /// stays square — on a short or narrow phone instead of overflowing.
    /// </summary>
    private void LayoutBoard()
    {
        if (_boardHost.Width <= 0 || _boardHost.Height <= 0) return;

        double byWidth = (_boardHost.Width - CardSpacing * (_size.Cols - 1)) / _size.Cols;
        double byHeight = (_boardHost.Height - CardSpacing * (_size.Rows - 1)) / _size.Rows;
        double card = Math.Max(1, Math.Min(byWidth, byHeight));

        _board.WidthRequest = card * _size.Cols + CardSpacing * (_size.Cols - 1);
        _board.HeightRequest = card * _size.Rows + CardSpacing * (_size.Rows - 1);
        _board.HorizontalOptions = LayoutOptions.Center;
        _board.VerticalOptions = LayoutOptions.Center;
    }

    private async void StartRound(BoardSize size)
    {
        _size = size;
        _engine = new MemoryMatchEngine(size.Pairs);
        _busy = false;

        _chooser.IsVisible = false;
        _boardHost.IsVisible = true;
        BuildBoard();

        _round?.Cancel();
        _round = new CancellationTokenSource();
        var token = _round.Token;

        // Preview: every card face-up, counting down, before the board goes dark.
        _previewing = true;
        UpdateCards();
        _hintLabel.Text = "Remember where the pairs are.";

        try
        {
            for (int s = PreviewSeconds; s >= 1; s--)
            {
                _statusLabel.Text = $"Memorise… {s}";
                await Task.Delay(1000, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;   // left the page, or restarted mid-preview
        }

        _previewing = false;
        UpdateCards();
        _hintLabel.Text = "Flip two cards — find every pair.";
        StartClock();
    }

    private void StartClock()
    {
        _timer?.Stop();
        _seconds = _size.Seconds;
        _statusLabel.Text = TimeSpan.FromSeconds(_seconds).ToString(@"mm\:ss");

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += async (_, _) =>
        {
            _seconds--;
            _statusLabel.Text = TimeSpan.FromSeconds(Math.Max(0, _seconds)).ToString(@"mm\:ss");
            if (_seconds <= 0)
            {
                _timer?.Stop();
                await WinAsync();
            }
        };
        _timer.Start();
    }

    /// <summary>Restart deals a new board at the same size — and previews it again.</summary>
    private void Restart()
    {
        _timer?.Stop();
        StartRound(_size);
    }

    private async void OnCard(int i)
    {
        if (_previewing || _busy) return;

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
        for (int i = 0; i < _borders.Count; i++)
        {
            var card = _engine.Cards[i];
            var border = _borders[i];
            var lbl = _labels[i];

            bool up = _previewing || card.FaceUp || card.Matched;
            border.BackgroundColor = up ? Color.FromArgb("#2E2B52") : Color.FromArgb("#8B7CF6");
            lbl.Text = up ? card.Face : "";
            border.Opacity = card.Matched ? 0.55 : 1;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _round?.Cancel();
        _timer?.Stop();
    }
}
