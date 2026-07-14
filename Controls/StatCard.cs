using Microsoft.Maui.Controls.Shapes;

namespace Nicotine_Stop.Controls;

/// <summary>A small stat tile: emoji, big value, caption (Home stat row, frame 1h).</summary>
public class StatCard : ContentView
{
    public static readonly BindableProperty EmojiProperty = BindableProperty.Create(
        nameof(Emoji), typeof(string), typeof(StatCard), "", propertyChanged: (b, _, v) => ((StatCard)b)._emoji.Text = (string)v);

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(string), typeof(StatCard), "", propertyChanged: (b, _, v) => ((StatCard)b)._value.Text = (string)v);

    public static readonly BindableProperty CaptionProperty = BindableProperty.Create(
        nameof(Caption), typeof(string), typeof(StatCard), "", propertyChanged: (b, _, v) => ((StatCard)b)._caption.Text = (string)v);

    public static readonly BindableProperty ValueColorProperty = BindableProperty.Create(
        nameof(ValueColor), typeof(Color), typeof(StatCard), null, propertyChanged: (b, _, v) => ((StatCard)b)._value.TextColor = (Color?)v ?? Color.FromArgb("#17322B"));

    private readonly Label _emoji;
    private readonly Label _value;
    private readonly Label _caption;

    public StatCard()
    {
        _emoji = new Label { FontSize = 16 };
        _value = new Label { FontFamily = "NunitoBlack", FontSize = 17 };
        _caption = new Label { FontFamily = "NunitoExtraBold", FontSize = 11 };
        _caption.SetDynamicResource(Label.TextColorProperty, "Faint");

        var card = new Border
        {
            StrokeThickness = 1.5,
            Padding = new Thickness(14, 12),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Content = new VerticalStackLayout { Spacing = 2, Children = { _emoji, _value, _caption } },
        };
        card.SetDynamicResource(BackgroundColorProperty, "Card");
        card.SetDynamicResource(Border.StrokeProperty, "CardBorder");
        Content = card;
    }

    public string Emoji { get => (string)GetValue(EmojiProperty); set => SetValue(EmojiProperty, value); }
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string Caption { get => (string)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }
    public Color? ValueColor { get => (Color?)GetValue(ValueColorProperty); set => SetValue(ValueColorProperty, value); }
}
