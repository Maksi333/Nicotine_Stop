using Microsoft.Maui.Controls.Shapes;

namespace Nicotine_Stop.Controls;

/// <summary>
/// The app's bottom navigation: Home · Goals · [SOS] · Health · Journey. The coral SOS button
/// overhangs the bar and slowly pulses; tapping it raises <see cref="SosTapped"/>.
/// </summary>
public class BottomNavBar : ContentView
{
    public static readonly BindableProperty ActiveIndexProperty = BindableProperty.Create(
        nameof(ActiveIndex), typeof(int), typeof(BottomNavBar), 0, BindingMode.TwoWay,
        propertyChanged: (b, _, _) => ((BottomNavBar)b).UpdateActive());

    public event EventHandler<int>? TabSelected;
    public event EventHandler? SosTapped;

    private readonly (Label Emoji, Label Text)[] _tabs = new (Label, Label)[4];
    private readonly Border _sos;
    private bool _pulsing;

    public BottomNavBar()
    {
        var bar = new Grid
        {
            Padding = new Thickness(8, 4, 8, 6),
            ColumnDefinitions =
            {
                new(GridLength.Star), new(GridLength.Star), new(GridLength.Star),
                new(GridLength.Star), new(GridLength.Star),
            },
        };
        bar.SetDynamicResource(BackgroundColorProperty, "Nav");

        AddTab(bar, 0, "🏠", "Home", 0);
        AddTab(bar, 1, "🎯", "Goals", 1);
        AddTab(bar, 3, "❤️", "Health", 2);
        AddTab(bar, 4, "🏅", "Journey", 3);

        _sos = new Border
        {
            WidthRequest = 66,
            HeightRequest = 66,
            StrokeThickness = 4,
            BackgroundColor = Color.FromArgb("#FF6B5E"),
            StrokeShape = new RoundRectangle { CornerRadius = 33 },
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Start,
            TranslationY = -18,
            Content = new Label
            {
                Text = "SOS",
                FontFamily = "NunitoBlack",
                FontSize = 14,
                CharacterSpacing = 0.5,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            },
            Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#D94F44")), Offset = new Point(0, 4), Radius = 2, Opacity = 1f },
        };
        _sos.SetDynamicResource(Border.StrokeProperty, "Bg");
        var sosTap = new TapGestureRecognizer();
        sosTap.Tapped += (_, _) => SosTapped?.Invoke(this, EventArgs.Empty);
        _sos.GestureRecognizers.Add(sosTap);

        var hairline = new BoxView { HeightRequest = 1.5, VerticalOptions = LayoutOptions.Start };
        hairline.SetDynamicResource(BoxView.ColorProperty, "NavBorder");

        // Order matters: the SOS button is added last so it renders on top of the hairline — its
        // 4px page-coloured border masks the line instead of the line cutting across the button.
        var root = new Grid();
        root.Add(bar);
        root.Add(hairline);
        root.Add(_sos);
        Content = root;

        Loaded += (_, _) => StartPulse();
        Unloaded += (_, _) => _pulsing = false;
        UpdateActive();
    }

    public int ActiveIndex { get => (int)GetValue(ActiveIndexProperty); set => SetValue(ActiveIndexProperty, value); }

    private void AddTab(Grid bar, int column, string emoji, string label, int index)
    {
        var e = new Label { Text = emoji, FontSize = 21, HorizontalOptions = LayoutOptions.Center };
        var t = new Label { Text = label, FontSize = 10.5, FontFamily = "NunitoExtraBold", HorizontalOptions = LayoutOptions.Center };
        var v = new VerticalStackLayout
        {
            Spacing = 1,
            Padding = new Thickness(0, 6),
            HorizontalOptions = LayoutOptions.Center,
            Children = { e, t },
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => TabSelected?.Invoke(this, index);
        v.GestureRecognizers.Add(tap);
        bar.Add(v, column);
        _tabs[index] = (e, t);
    }

    private void UpdateActive()
    {
        for (int i = 0; i < _tabs.Length; i++)
        {
            var (emoji, text) = _tabs[i];
            bool active = i == ActiveIndex;
            emoji.Opacity = active ? 1 : 0.45;
            text.FontFamily = active ? "NunitoBlack" : "NunitoExtraBold";
            text.SetDynamicResource(Label.TextColorProperty, active ? "PrimaryText" : "Faint");
        }
    }

    private async void StartPulse()
    {
        if (_pulsing) return;
        _pulsing = true;
        try
        {
            while (_pulsing)
            {
                await _sos.ScaleTo(1.05, 1200, Easing.SinInOut);
                await _sos.ScaleTo(1.0, 1200, Easing.SinInOut);
            }
        }
        catch { /* torn down */ }
    }
}
