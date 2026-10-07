using CommunityToolkit.Maui.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using Nicotine_Stop.Services;
using Nicotine_Stop.Views.Sos;
using SnusStop.Core.Games;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Views.Games;

public partial class MinesweeperPage : ContentPage
{
    private const int Size = 8;
    private const int Mines = 10;
    private const int Xp = XpService.GameMinesweeperXp;

    /// <summary>Hold time to flag, in ms: quick enough to feel instant, long enough to not fire on a dig.</summary>
    private const int LongPressHold = 350;

    /// <summary>How long after a hold a tap on that same cell is treated as the hold's own release.</summary>
    private static readonly TimeSpan TapAfterHoldWindow = TimeSpan.FromMilliseconds(600);

    /// <summary>
    /// Widest the page column may get. Every phone is narrower than this, so phones keep the
    /// full-width layout untouched; on a tablet the rows centre instead of stretching edge to edge.
    /// </summary>
    private const double MaxContentWidth = 520;

    /// <summary>
    /// Widest the square board may get. A landscape tablet would otherwise hand it ~600dp and make
    /// every cell more than twice its size on a phone.
    /// </summary>
    private const double MaxBoardSide = 460;

    /// <summary>The board frame's Padding="10", counted on both sides.</summary>
    private const double BoardFramePadding = 20;

    private readonly AppState _state;
    private readonly IServiceProvider _services;
    private readonly Border[,] _cells = new Border[Size, Size];
    private readonly Label[,] _labels = new Label[Size, Size];

    private MinesweeperEngine _engine = new(Size, Mines);
    private IDispatcherTimer? _timer;
    private int _seconds;
    private bool _flagMode;
    private bool _awarded;
    private (int Row, int Col) _lastLongPressCell = (-1, -1);
    private DateTime _lastLongPressAt = DateTime.MinValue;

    public MinesweeperPage(AppState state, IServiceProvider services)
    {
        InitializeComponent();
        _state = state;
        _services = services;

        // Root is centred, so it needs an explicit width: on a phone that resolves to the full page
        // width (unchanged layout), on a tablet to a centred column.
        SizeChanged += (_, _) =>
        {
            if (Width > 0) Root.WidthRequest = Math.Min(Width, MaxContentWidth);
        };

        BuildBoard();
        NewGame();
    }

    private void BuildBoard()
    {
        for (int i = 0; i < Size; i++)
        {
            Board.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            Board.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }
        // The board is square and has to fit the space left over after the header, stat cards,
        // buttons and status line have taken theirs. Sizing it off width alone overflowed the row
        // on anything wider than it is tall — a landscape tablet drove the square to full width.
        BoardArea.SizeChanged += (_, _) => ResizeBoard();

        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                var lbl = new Label { FontFamily = "NunitoBlack", FontSize = 15, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
                var cell = new Border { StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 9 }, Content = lbl };
                int rr = r, cc = c;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => OnCell(rr, cc);
                cell.GestureRecognizers.Add(tap);

                // Second, quicker way to flag, alongside the Dig/Flag toggle: hold a cell. Both
                // routes call ToggleFlagAt, so there is only ever one flag state and one counter.
                cell.Behaviors.Add(new TouchBehavior
                {
                    LongPressDuration = LongPressHold,
                    LongPressCommand = new Command(() => OnCellLongPress(rr, cc)),
                });

                _cells[r, c] = cell;
                _labels[r, c] = lbl;
                Board.Add(cell, c, r);
            }
    }

    /// <summary>
    /// Sizes the board to the largest square that fits its row, capped so tablet cells stay in the
    /// same ballpark as phone cells. On a phone the row is taller than it is wide, so the width wins
    /// and the result matches the old width-only sizing exactly.
    /// </summary>
    private void ResizeBoard()
    {
        if (BoardArea.Width <= 0 || BoardArea.Height <= 0) return;

        double side = Math.Min(BoardArea.Width, BoardArea.Height) - BoardFramePadding;
        side = Math.Min(side, MaxBoardSide);
        if (side <= 0 || Math.Abs(Board.WidthRequest - side) <= 1) return;

        Board.WidthRequest = side;
        Board.HeightRequest = side;
    }

    private void NewGame()
    {
        _engine = new MinesweeperEngine(Size, Mines);
        _seconds = 0;
        _awarded = false;
        TimerLabel.Text = "00:00";
        StatusLabel.IsVisible = true;
        Refresh();

        _timer?.Stop();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) =>
        {
            _seconds++;
            TimerLabel.Text = TimeSpan.FromSeconds(_seconds).ToString(@"mm\:ss");
        };
        _timer.Start();
    }

    private async void OnCell(int r, int c)
    {
        if (_engine.GameOver) return;

        // A hold already flagged this cell; the tap that lands when the finger lifts must not dig it.
        if (ConsumedByLongPress(r, c)) return;

        if (_flagMode)
        {
            ToggleFlagAt(r, c);
            return;
        }

        _engine.Reveal(r, c);
        Refresh();

        if (_engine.Lost)
        {
            _timer?.Stop();
        }
        else if (_engine.Won)
        {
            _timer?.Stop();
            await WinAsync();
        }
    }

    /// <summary>The one flag code path — used by Flag mode and by long-press alike. The engine
    /// ignores revealed cells and un-flags an already-flagged one, so both rules come for free.</summary>
    private void ToggleFlagAt(int r, int c)
    {
        _engine.ToggleFlag(r, c);
        Refresh();
    }

    private void OnCellLongPress(int r, int c)
    {
        if (_engine.GameOver) return;

        _lastLongPressCell = (r, c);
        _lastLongPressAt = DateTime.UtcNow;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.LongPress); } catch { }
        ToggleFlagAt(r, c);
    }

    /// <summary>
    /// True while the tap that follows a hold on this same cell is still arriving. Scoped to the
    /// cell and to a short window, so it can never swallow a genuine tap elsewhere on the board.
    /// </summary>
    private bool ConsumedByLongPress(int r, int c) =>
        _lastLongPressCell == (r, c) && DateTime.UtcNow - _lastLongPressAt < TapAfterHoldWindow;

    private void Refresh()
    {
        MinesLabel.Text = _engine.MinesLeft.ToString();
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                var cell = _engine.Grid[r, c];
                var border = _cells[r, c];
                var lbl = _labels[r, c];
                bool alt = (r + c) % 2 == 0;

                switch (cell.State)
                {
                    case CellState.Hidden:
                        border.BackgroundColor = Color.FromArgb(alt ? "#8B7CF6" : "#7C6CEC");
                        lbl.Text = "";
                        break;
                    case CellState.Flagged:
                        border.BackgroundColor = Color.FromArgb(alt ? "#8B7CF6" : "#7C6CEC");
                        lbl.Text = "🚩";
                        break;
                    default:
                        if (cell.IsMine)
                        {
                            border.BackgroundColor = Color.FromArgb("#3A2749");
                            lbl.Text = "💣";
                        }
                        else
                        {
                            border.BackgroundColor = Color.FromArgb(alt ? "#2E2B52" : "#2A2749");
                            lbl.Text = cell.Adjacent == 0 ? "" : cell.Adjacent.ToString();
                            lbl.TextColor = cell.Adjacent switch
                            {
                                1 => Color.FromArgb("#8FB8F8"),
                                2 => Color.FromArgb("#7EE0A9"),
                                3 => Color.FromArgb("#FF9D94"),
                                4 => Color.FromArgb("#D3B4FF"),
                                _ => Colors.White,
                            };
                        }
                        break;
                }
            }
    }

    private void OnDigMode(object? sender, EventArgs e) => SetMode(false);
    private void OnFlagMode(object? sender, EventArgs e) => SetMode(true);

    private void SetMode(bool flag)
    {
        _flagMode = flag;
        DigBtn.BackgroundColor = Color.FromArgb(flag ? "#1AFFFFFF" : "#8B7CF6");
        FlagBtn.BackgroundColor = Color.FromArgb(flag ? "#8B7CF6" : "#1AFFFFFF");
    }

    private void OnRestart(object? sender, EventArgs e) => NewGame();

    private async void OnBack(object? sender, EventArgs e)
    {
        _timer?.Stop();
        await Navigation.PopAsync();
    }

    private async void OnOkay(object? sender, EventArgs e) => await WinAsync();

    private async Task WinAsync()
    {
        if (_awarded) return;
        _awarded = true;
        _timer?.Stop();

        // Only the first win of each local day pays XP; the craving still counts on replays.
        int xp = _services.GetRequiredService<IDailyXpService>().ClaimXp(ActivityIds.Minesweeper, Xp);

        await _state.AddEventAsync(EventLog.CravingWon(DateTime.UtcNow, "game", xp));
        var celebrate = _services.GetRequiredService<CravingDefeatedPage>();
        celebrate.Init(xp);
        await Navigation.PushAsync(celebrate);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }
}
