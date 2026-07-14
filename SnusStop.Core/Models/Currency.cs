using System.Globalization;

namespace SnusStop.Core.Models;

/// <summary>Supported currencies. The user picks one during onboarding and can change it in Settings.</summary>
public enum Currency
{
    DKK,
    SEK,
    NOK,
    EUR,
    USD,
}

public static class Currencies
{
    /// <summary>Every currency, in picker order. Add a new one here and both pickers pick it up.</summary>
    public static readonly IReadOnlyList<Currency> All = new[]
    {
        Currency.DKK, Currency.SEK, Currency.NOK, Currency.EUR, Currency.USD,
    };
}

public static class CurrencyExtensions
{
    private static readonly CultureInfo Nordic = CultureInfo.GetCultureInfo("da-DK");   // 1.234,56
    private static readonly CultureInfo Anglo = CultureInfo.GetCultureInfo("en-US");    // 1,234.56

    public static string Symbol(this Currency c) => c switch
    {
        Currency.EUR => "€",
        Currency.USD => "$",
        _ => "kr",      // DKK / SEK / NOK
    };

    /// <summary>True when the symbol belongs before the amount ("$45"), false when after ("45 kr").</summary>
    public static bool SymbolLeads(this Currency c) => c == Currency.USD;

    /// <summary>Grouping/decimal convention for this currency.</summary>
    public static CultureInfo NumberCulture(this Currency c) =>
        c == Currency.USD ? Anglo : Nordic;

    public static string Code(this Currency c) => c.ToString();

    public static Currency Parse(string code) =>
        Enum.TryParse<Currency>(code, ignoreCase: true, out var c) ? c : Currency.DKK;
}
