using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Nicotine_Stop.Controls;

/// <summary>
/// A game-like primary action: flat fill with a chunky <c>0 4px 0</c> bottom-shadow that depresses
/// on press. No blurred drop shadows — matches the design's button language.
/// </summary>
public class ChunkyButton : ContentView
{
    private const double ShadowDepth = 4;

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(ChunkyButton), string.Empty, propertyChanged: (b, _, v) => ((ChunkyButton)b)._label.Text = (string)v);

    public static readonly BindableProperty FillProperty = BindableProperty.Create(
        nameof(Fill), typeof(Color), typeof(ChunkyButton), Color.FromArgb("#14B36B"), propertyChanged: (b, _, _) => ((ChunkyButton)b).UpdateColors());

    public static readonly BindableProperty ShadowColorProperty = BindableProperty.Create(
        nameof(ShadowColor), typeof(Color), typeof(ChunkyButton), Color.FromArgb("#0E8A50"), propertyChanged: (b, _, _) => ((ChunkyButton)b).UpdateColors());

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(ChunkyButton), Colors.White, propertyChanged: (b, _, v) => ((ChunkyButton)b)._label.TextColor = (Color)v);

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(ChunkyButton), 18d, propertyChanged: (b, _, _) => ((ChunkyButton)b).UpdateShape());

    public static readonly BindableProperty ButtonHeightProperty = BindableProperty.Create(
        nameof(ButtonHeight), typeof(double), typeof(ChunkyButton), 56d, propertyChanged: (b, _, _) => ((ChunkyButton)b).UpdateShape());

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(ChunkyButton), 17d, propertyChanged: (b, _, v) => ((ChunkyButton)b)._label.FontSize = (double)v);

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(ChunkyButton), null);

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(ChunkyButton), null);

    private readonly Border _shadow;
    private readonly Border _face;
    private readonly Label _label;
    private bool _pressed;

    public ChunkyButton()
    {
        _label = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            FontFamily = "NunitoBlack",
            FontSize = 17,
            TextColor = Colors.White,
        };

        _shadow = new Border { StrokeThickness = 0, VerticalOptions = LayoutOptions.Start, TranslationY = ShadowDepth };
        _face = new Border { StrokeThickness = 0, VerticalOptions = LayoutOptions.Start, Content = _label };

        Content = new Grid { Children = { _shadow, _face } };

        UpdateShape();
        UpdateColors();

        var pointer = new PointerGestureRecognizer();
        pointer.PointerPressed += (_, _) => Depress(true);
        pointer.PointerReleased += (_, _) => Depress(false);
        pointer.PointerExited += (_, _) => Depress(false);
        GestureRecognizers.Add(pointer);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Command?.Execute(CommandParameter);
        GestureRecognizers.Add(tap);
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public Color Fill { get => (Color)GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public Color ShadowColor { get => (Color)GetValue(ShadowColorProperty); set => SetValue(ShadowColorProperty, value); }
    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double ButtonHeight { get => (double)GetValue(ButtonHeightProperty); set => SetValue(ButtonHeightProperty, value); }
    public new double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private void UpdateShape()
    {
        var shape = new RoundRectangle { CornerRadius = new CornerRadius(CornerRadius) };
        _face.StrokeShape = shape;
        _shadow.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(CornerRadius) };
        _face.HeightRequest = ButtonHeight;
        _shadow.HeightRequest = ButtonHeight;
        HeightRequest = ButtonHeight + ShadowDepth;
    }

    private void UpdateColors()
    {
        _face.BackgroundColor = Fill;
        _shadow.BackgroundColor = ShadowColor;
    }

    private async void Depress(bool down)
    {
        if (down == _pressed) return;
        _pressed = down;
        await _face.TranslateTo(0, down ? ShadowDepth : 0, 60, Easing.CubicOut);
    }
}
