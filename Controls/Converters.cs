using System.Globalization;

namespace Nicotine_Stop.Controls;

/// <summary>True when the bound int equals the ConverterParameter (used for step visibility).</summary>
public class IntEqualConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && int.TryParse(parameter?.ToString(), out var p) && i == p;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Maps a 0..1 fraction to a proportional star GridLength (ConverterParameter="rem" = 1−f).</summary>
public class StarFractionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double f = value is double d ? d : System.Convert.ToDouble(value ?? 0);
        bool remainder = parameter?.ToString() == "rem";
        double v = remainder ? 1 - f : f;
        return new GridLength(Math.Max(0.0001, v), GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Inverts a bool.</summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}
