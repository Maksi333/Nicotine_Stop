using System.Globalization;

namespace SnusStop.Core.Services;

public static class SlipAmount
{
    /// <summary>Optional, nonnegative spending; accepts either decimal separator, without grouping.</summary>
    public static bool TryParse(string? text, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(text)) return true;
        return decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture, out amount)
               && amount >= 0m && decimal.Round(amount, 2) == amount;
    }
}
