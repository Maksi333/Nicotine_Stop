using Microsoft.Maui.Controls.Shapes;

namespace Nicotine_Stop.Controls;

/// <summary>A pill switch (frames 1l split-savings, 1x notification rows).</summary>
public class PillToggle : ContentView
{
    public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(
        nameof(IsToggled), typeof(bool), typeof(PillToggle), false,
        BindingMode.TwoWay, propertyChanged: (b, _, _) => ((PillToggle)b).Apply(true));

    public static readonly BindableProperty OnColorProperty = BindableProperty.Create(
        nameof(OnColor), typeof(Color), typeof(PillToggle), Color.FromArgb("#14B36B"), propertyChanged: (b, _, _) => ((PillToggle)b).Apply(false));

    public static readonly BindableProperty OffColorProperty = BindableProperty.Create(
        nameof(OffColor), typeof(Color), typeof(PillToggle), Color.FromArgb("#D7E6DC"), propertyChanged: (b, _, _) => ((PillToggle)b).Apply(false));

    private const double Travel = 18;
    private readonly Border _track;
    private readonly Border _knob;

    public event EventHandler<bool>? Toggled;

    public PillToggle()
    {
        _knob = new Border
        {
            WidthRequest = 20,
            HeightRequest = 20,
            StrokeThickness = 0,
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            TranslationX = 3,
            Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.25f, Radius = 3, Offset = new Point(0, 1) },
        };
        _track = new Border
        {
            WidthRequest = 44,
            HeightRequest = 26,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 13 },
            Content = _knob,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
        };
        Content = _track;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            IsToggled = !IsToggled;
            Toggled?.Invoke(this, IsToggled);
        };
        GestureRecognizers.Add(tap);

        Apply(false);
    }

    public bool IsToggled { get => (bool)GetValue(IsToggledProperty); set => SetValue(IsToggledProperty, value); }
    public Color OnColor { get => (Color)GetValue(OnColorProperty); set => SetValue(OnColorProperty, value); }
    public Color OffColor { get => (Color)GetValue(OffColorProperty); set => SetValue(OffColorProperty, value); }

    private void Apply(bool animate)
    {
        _track.BackgroundColor = IsToggled ? OnColor : OffColor;
        double target = IsToggled ? 3 + Travel : 3;
        if (animate) _knob.TranslateTo(target, 0, 120, Easing.CubicOut);
        else _knob.TranslationX = target;
    }
}
