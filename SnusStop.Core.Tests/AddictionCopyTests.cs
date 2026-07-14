using SnusStop.Core.Models;

namespace SnusStop.Core.Tests;

public class AddictionCopyTests
{
    [Fact]
    public void Existing_profiles_default_to_snus()
    {
        Assert.Equal(AddictionType.Snus, new Profile().Addiction);
    }

    [Fact]
    public void Snus_uses_pouches_and_cans()
    {
        var c = AddictionCopy.For(AddictionType.Snus);

        Assert.Equal("pouches", c.Units);
        Assert.Equal("can", c.Container);
        Assert.Equal("pouches skipped", c.SkippedCaption);
        Assert.Equal("skipped", c.SkippedShort);
        Assert.Equal("Pouches per day", c.UnitsPerDayLabel);
        Assert.Equal("Pouches per can", c.UnitsPerContainerLabel);
        Assert.Equal("Price per can", c.PricePerContainerLabel);
    }

    [Fact]
    public void Cigarettes_use_cigarettes_and_packs()
    {
        var c = AddictionCopy.For(AddictionType.Cigarettes);

        Assert.Equal("cigarettes", c.Units);
        Assert.Equal("pack", c.Container);
        Assert.Equal("cigarettes not smoked", c.SkippedCaption);
        Assert.Equal("not smoked", c.SkippedShort);
        Assert.Equal("Cigarettes per day", c.UnitsPerDayLabel);
        Assert.Equal("Cigarettes per pack", c.UnitsPerContainerLabel);
        Assert.Equal("Price per pack", c.PricePerContainerLabel);
    }

    [Fact]
    public void Every_health_milestone_has_copy_for_both_addictions()
    {
        foreach (var type in new[] { AddictionType.Snus, AddictionType.Cigarettes })
        {
            var c = AddictionCopy.For(type);
            foreach (var m in Milestones.Health)
            {
                string body = c.HealthBody(m.Key);
                Assert.False(string.IsNullOrWhiteSpace(body),
                    $"{type} is missing health copy for milestone '{m.Key}'");
            }
        }
    }

    [Fact]
    public void Health_copy_is_addiction_appropriate()
    {
        var snus = AddictionCopy.For(AddictionType.Snus);
        var cigs = AddictionCopy.For(AddictionType.Cigarettes);

        // The snus timeline talks about gums and pouches; the cigarette one must not.
        Assert.Contains("gum", snus.HealthBody("2w"), StringComparison.OrdinalIgnoreCase);
        foreach (var m in Milestones.Health)
        {
            string body = cigs.HealthBody(m.Key);
            Assert.DoesNotContain("pouch", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("gum", body, StringComparison.OrdinalIgnoreCase);
        }

        // ...and it does mention what actually recovers for a smoker.
        string all = string.Join(" ", Milestones.Health.Select(m => cigs.HealthBody(m.Key)));
        Assert.Contains("lung", all, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("breath", all, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("smell", all, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unit_badge_labels_follow_the_addiction()
    {
        var snus = Badges.For(AddictionType.Snus, Currency.DKK).First(b => b.Key == "pouches500");
        var cigs = Badges.For(AddictionType.Cigarettes, Currency.DKK).First(b => b.Key == "pouches500");

        Assert.Equal("500 pouches skipped", snus.Label);
        Assert.Equal("500 cigarettes not smoked", cigs.Label);
    }

    [Fact]
    public void Savings_badge_labels_follow_the_currency()
    {
        string Label(Currency c) =>
            Badges.For(AddictionType.Snus, c).First(b => b.Key == "save250").Label;

        Assert.Equal("250 kr saved", Label(Currency.DKK));
        Assert.Equal("250 € saved", Label(Currency.EUR));
        Assert.Equal("$250 saved", Label(Currency.USD));
    }

    [Fact]
    public void No_badge_label_leaks_an_unresolved_token()
    {
        foreach (var type in new[] { AddictionType.Snus, AddictionType.Cigarettes })
            foreach (var c in Currencies.All)
                foreach (var b in Badges.For(type, c))
                    Assert.DoesNotContain("{", b.Label);
    }
}
