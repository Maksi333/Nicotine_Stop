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

    private readonly AppState _state;
    private readonly IServiceProvider _services;
    private readonly Border[,] _cells = new Border[Size, Size];
    private readonly Label[,] _labels = new Label[Size, Size];

    private MinesweeperEngine _engine = new(Size, Mines);
    private IDispatcherTimer? _timer;
    private int _seconds;
    private bool _flagMode;
    private bool _awarded;

    public MinesweeperPage(AppState state, IServiceProvider services)
    {
        InitializeComponent();
        _state = state;
        _services = services;
        BuildBoard();
        NewGame();
        Helpers.SafeArea.ApplyInsets(this, top: true, bottom: true);
    }

    private void BuildBoard()
    {
        for (int i = 0; i < Size; i++)
        {
            Board.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            Board.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }
        Board.SizeChanged += (_, _) =>
        {
            if (Board.Width > 0 && Math.Abs(Board.HeightRequest - Board.Width) > 1)
                Board.HeightRequest = Board.Width;
        };

        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                var lbl = new Label { FontFamily = "NunitoBlack", FontSize = 15, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
                var cell = new Border { StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 9 }, Content = lbl };
                int rr = r, cc = c;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => OnCell(rr, cc);
                cell.GestureRecognizers.Add(tap);
                _cells[r, c] = cell;
                _labels[r, c] = lbl;
                Board.Add(cell, c, r);
            }
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
        if (_flagMode) _engine.ToggleFlag(r, c);
        else _engine.Reveal(r, c);
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
        await _state.AddEventAsync(EventLog.CravingWon(DateTime.UtcNow, "game", Xp));
        var celebrate = _services.GetRequiredService<CravingDefeatedPage>();
        celebrate.Init(Xp);
        await Navigation.PushAsync(celebrate);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }
}
