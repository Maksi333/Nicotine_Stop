using System.Globalization;
using SnusStop.Core.Services;

namespace SnusStop.Core.Tests;

public class GoalPriceTests
{
    private static readonly CultureInfo Da = CultureInfo.GetCultureInfo("da-DK");
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0")]
    [InlineData("0,00")]
    [InlineData("-500")]
    [InlineData("abc")]
    [InlineData("500 kr")]
    public void Rejects_missing_zero_negative_or_unreadable_prices(string? input)
        => Assert.False(GoalPrice.TryParse(input, Da, out _));

    [Theory]
    [InlineData("500", 500)]
    [InlineData(" 500 ", 500)]
    [InlineData("2.500", 2500)]
    [InlineData("45,50", 45.5)]
    public void Reads_prices_in_the_phone_s_number_format_danish(string input, double expected)
    {
        Assert.True(GoalPrice.TryParse(input, Da, out var price));
        Assert.Equal((decimal)expected, price);
    }

    [Theory]
    [InlineData("2,500", 2500)]
    [InlineData("45.50", 45.5)]
    public void Reads_prices_in_the_phone_s_number_format_english(string input, double expected)
    {
        Assert.True(GoalPrice.TryParse(input, En, out var price));
        Assert.Equal((decimal)expected, price);
    }
}
