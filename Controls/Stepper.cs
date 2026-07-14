namespace Nicotine_Stop.Controls;

/// <summary>−/+ stepper with a big value in the middle (frame 1c cards, Settings edits).</summary>
public class Stepper : ContentView
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(int), typeof(Stepper), 0, BindingMode.TwoWay, propertyChanged: (b, _, _) => ((Stepper)b).Sync());

    public static readonly BindableProperty MinValueProperty = BindableProperty.Create(
        nameof(MinValue), typeof(int), typeof(Stepper), 0);

    public static readonly BindableProperty MaxValueProperty = BindableProperty.Create(
        nameof(MaxValue), typeof(int), typeof(Stepper), 99);

    public static readonly BindableProperty StepSizeProperty = BindableProperty.Create(
        nameof(StepSize), typeof(int), typeof(Stepper), 1);

    private readonly Label _value;

    public Stepper()
    {
        var minus = new ChunkyButton
        {
            Text = "−",
            Fill = Color.FromArgb("#E9F3EC"),
            ShadowColor = Color.FromArgb("#D0E2D6"),
            TextColor = Color.FromArgb("#14B36B"),
            CornerRadius = 14,
            ButtonHeight = 44,
            FontSize = 22,
            WidthRequest = 44,
            Command = new Command(() => Value = Math.Max(MinValue, Value - StepSize)),
        };
        var plus = new ChunkyButton
        {
            Text = "+",
            Fill = Color.FromArgb("#14B36B"),
            ShadowColor = Color.FromArgb("#0E8A50"),
            TextColor = Colors.White,
            CornerRadius = 14,
            ButtonHeight = 44,
            FontSize = 22,
            WidthRequest = 44,
            Command = new Command(() => Value = Math.Min(MaxValue, Value + StepSize)),
        };
        _value = new Label
        {
            FontFamily = "NunitoBlack",
            FontSize = 26,
            WidthRequest = 52,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _value.SetDynamicResource(Label.TextColorProperty, "Ink");

        Content = new HorizontalStackLayout
        {
            Spacing = 6,
            VerticalOptions = LayoutOptions.Center,
            Children = { minus, _value, plus },
        };
        Sync();
    }

    public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public int MinValue { get => (int)GetValue(MinValueProperty); set => SetValue(MinValueProperty, value); }
    public int MaxValue { get => (int)GetValue(MaxValueProperty); set => SetValue(MaxValueProperty, value); }
    public int StepSize { get => (int)GetValue(StepSizeProperty); set => SetValue(StepSizeProperty, value); }

    private void Sync() => _value.Text = Value.ToString();
}
