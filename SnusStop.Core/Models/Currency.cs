namespace SnusStop.Core.Models;

/// <summary>Supported currencies. Symbol follows the design: EUR → €, everything else → kr.</summary>
public enum Currency
{
    DKK,
    SEK,
    NOK,
    EUR,
}

public static class CurrencyExtensions
{
    public static string Symbol(this Currency c) => c == Currency.EUR ? "€" : "kr";

    public static string Code(this Currency c) => c.ToString();

    public static Currency Parse(string code) =>
        Enum.TryParse<Currency>(code, ignoreCase: true, out var c) ? c : Currency.DKK;
}
