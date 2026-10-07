using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace SnusStop.Core.Tests;

public class SlipAmountTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData("0", 0)]
    [InlineData("45", 45)]
    [InlineData("45.50", 45.5)]
    [InlineData("45,50", 45.5)]
    [InlineData(" 12,75 ", 12.75)]
    [InlineData(".50", 0.5)]
    public void Accepts_optional_amount_and_both_decimal_separators(string? input, double expected)
    {
        Assert.True(SlipAmount.TryParse(input, out var amount));
        Assert.Equal((decimal)expected, amount);
    }

    [Theory]
    [InlineData("-10")]
    [InlineData("abc")]
    [InlineData("1.234,56")]
    [InlineData("1,234.56")]
    [InlineData("1,234")]
    [InlineData("1 000")]
    [InlineData("1.2.3")]
    [InlineData("1e2")]
    [InlineData("NaN")]
    [InlineData("0.001")]
    [InlineData("99999999999999999999999999999999999999")]
    public void Rejects_invalid_negative_overprecise_or_ambiguous_amounts(string input)
        => Assert.False(SlipAmount.TryParse(input, out _));

    [Fact]
    public void Negative_slip_spending_cannot_be_logged()
        => Assert.Throws<ArgumentOutOfRangeException>(() => EventLog.Slip(DateTime.UtcNow, Trigger.None, null, -1m));
}
