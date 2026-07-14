using Microsoft.Maui.Controls.Shapes;

namespace Nicotine_Stop.Controls;

/// <summary>Two-option segmented toggle with a sliding white pill (frame 1e).</summary>
public class SegmentedControl : ContentView
{
    public static readonly BindableProperty Option1Property = BindableProperty.Create(
        nameof(Option1), typeof(string), typeof(SegmentedControl), "", propertyChanged: (b, _, v) => ((SegmentedControl)b)._label0.Text = (string)v);

    public static readonly BindableProperty Option2Property = BindableProperty.Create(
        nameof(Option2), typeof(string), typeof(SegmentedControl), "", propertyChanged: (b, _, v) => ((SegmentedControl)b)._label1.Text = (string)v);

    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex), typeof(int), typeof(SegmentedControl), 0, BindingMode.TwoWay, propertyChanged: (b, _, _) => ((SegmentedControl)b).Apply());

    private readonly Border _seg0;
    private readonly Border _seg1;
    private readonly Label _label0;
    private readonly Label _label1;

    public SegmentedControl()
    {
        _label0 = MakeLabel();
        _label1 = MakeLabel();
        _seg0 = MakeSegment(_label0);
        _seg1 = MakeSegment(_label1);

        AddTap(_seg0, 0);
        AddTap(_seg1, 1);

        var grid = new Grid { ColumnSpacing = 4, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } };
        grid.Add(_seg0, 0);
        grid.Add(_seg1, 1);

        var track = new Border
        {
            StrokeThickness = 0,
            Padding = 5,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Content = grid,
        };
        track.SetDynamicResource(BackgroundColorProperty, "Track");
        Content = track;
        Apply();
    }

    public string Option1 { get => (string)GetValue(Option1Property); set => SetValue(Option1Property, value); }
    public string Option2 { get => (string)GetValue(Option2Property); set => SetValue(Option2Property, value); }
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }

    private static Label MakeLabel() => new()
    {
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center,
        FontFamily = "NunitoExtraBold",
        FontSize = 14,
    };

    private static Border MakeSegment(Label content) => new()
    {
        StrokeThickness = 0,
        HeightRequest = 42,
        StrokeShape = new RoundRectangle { CornerRadius = 12 },
        Content = content,
    };

    private void AddTap(Border seg, int index)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => SelectedIndex = index;
        seg.GestureRecognizers.Add(tap);
    }

    private void Apply()
    {
        Style(_seg0, _label0, SelectedIndex == 0);
        Style(_seg1, _label1, SelectedIndex == 1);
    }

    private static void Style(Border seg, Label label, bool selected)
    {
        if (selected)
        {
            seg.SetDynamicResource(BackgroundColorProperty, "Card");
            seg.Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.10f, Radius = 6, Offset = new Point(0, 2) };
            label.SetDynamicResource(Label.TextColorProperty, "PrimaryText");
            label.FontFamily = "NunitoBlack";
        }
        else
        {
            seg.BackgroundColor = Colors.Transparent;
            seg.Shadow = null;
            label.SetDynamicResource(Label.TextColorProperty, "Muted");
            label.FontFamily = "NunitoExtraBold";
        }
    }
}
