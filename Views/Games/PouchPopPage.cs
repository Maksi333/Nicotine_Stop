using Microsoft.Maui.Controls.Shapes;
using Nicotine_Stop.Services;

namespace Nicotine_Stop.Views.Games;

public class PouchPopPage : GameHostPage
{
    private const int Cols = 6;
    private const int Rows = 8;

    private int _popped;
    private Label _counter = null!;

    public PouchPopPage(AppState state, IServiceProvider services) : base(state, services)
    {
        GameXp = 10;
        Build();
    }

    private void Build()
    {
        _counter = new Label { Text = "0 popped", FontFamily = "NunitoBlack", FontSize = 16, TextColor = Color.FromArgb("#F2F1FA"), HorizontalTextAlignment = TextAlignment.Center };

        var caption = new VerticalStackLayout
        {
            Spacing = 4,
            Padding = new Thickness(0, 12, 0, 0),
            Children =
            {
                _counter,
                new Label { Text = "Pop the urge — one bubble at a time.", FontFamily = "NunitoBold", FontSize = 13.5, TextColor = Color.FromArgb("#B9B4E3"), HorizontalTextAlignment = TextAlignment.Center },
            },
        };

        var grid = new Grid { RowSpacing = 8, ColumnSpacing = 8, VerticalOptions = LayoutOptions.Center };
        for (int c = 0; c < Cols; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (int r = 0; r < Rows; r++) grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        grid.SizeChanged += (_, _) => { if (grid.Width > 0) grid.HeightRequest = grid.Width * Rows / Cols; };

        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                grid.Add(MakeBubble(), c, r);

        var outer = new Grid
        {
            Padding = new Thickness(22, 0, 22, 18),
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
        };
        outer.Add(Header("POUCH POP"), 0, 0);
        outer.Add(caption, 0, 1);
        outer.Add(grid, 0, 2);
        outer.Add(OkayFooter("Craving faded — I'm okay"), 0, 3);
        Content = outer;
    }

    private Border MakeBubble()
    {
        var b = new Border
        {
            BackgroundColor = Color.FromArgb("#8B7CF6"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 100 },
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            Margin = 3,
            Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#6C5CE0")), Offset = new Point(0, 3), Radius = 1, Opacity = 1f },
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await Pop(b);
        b.GestureRecognizers.Add(tap);
        return b;
    }

    private async Task Pop(Border b)
    {
        if (b.Scale < 0.9) return; // mid-pop
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        _popped++;
        _counter.Text = $"{_popped} popped";

        await b.ScaleTo(0.4, 90, Easing.CubicIn);
        b.Opacity = 0.22;
        await Task.Delay(500);
        b.Opacity = 1;
        await b.ScaleTo(1.0, 140, Easing.CubicOut);
    }
}
