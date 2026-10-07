using System.Globalization;

namespace SnusStop.Core.Services;

public static class GoalPrice
{
    /// <summary>
    /// A goal's price, read in the phone's number format ("2.500" is 2500 on a Danish phone). Required
    /// and above zero: a 0-price goal can never be funded, so it would sit at the head of the queue
    /// forever reading "0% · 0 days to go".
    /// </summary>
    public static bool TryParse(string? text, CultureInfo culture, out decimal price) =>
        decimal.TryParse(text?.Trim(), NumberStyles.Number, culture, out price) && price > 0m;
}
