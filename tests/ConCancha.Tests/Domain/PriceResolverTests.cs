using ConCancha.Domain.Pricing;

namespace ConCancha.Tests.Domain;

public class PriceResolverTests
{
    private const int Hour = 60;
    private const int EndOfDay = 24 * Hour;

    private static PriceRule Rule(decimal price, int from, int to, Weekdays? days = null) =>
        new() { Price = price, FromMinuteOfDay = from, ToMinuteOfDay = to, Days = days };

    [Fact]
    public void Resolve_NightRuleBeatsBaseRule_ByNarrowerWindow()
    {
        var rules = new[] { Rule(100, 0, EndOfDay), Rule(150, 18 * Hour, EndOfDay) };

        Assert.Equal(100, PriceResolver.Resolve(rules, DayOfWeek.Tuesday, 10 * Hour));
        Assert.Equal(150, PriceResolver.Resolve(rules, DayOfWeek.Tuesday, 19 * Hour));
    }

    [Fact]
    public void Resolve_SpecificDaysBeatEveryDay_EvenWithAWiderWindow()
    {
        var weekend = Weekdays.Friday | Weekdays.Saturday | Weekdays.Sunday;
        var rules = new[] { Rule(150, 18 * Hour, EndOfDay), Rule(200, 0, EndOfDay, weekend) };

        Assert.Equal(200, PriceResolver.Resolve(rules, DayOfWeek.Saturday, 19 * Hour));
        Assert.Equal(150, PriceResolver.Resolve(rules, DayOfWeek.Monday, 19 * Hour));
    }

    [Fact]
    public void Resolve_NoRuleCoversTheMoment_ReturnsNull()
    {
        var rules = new[] { Rule(100, 18 * Hour, EndOfDay) };

        Assert.Null(PriceResolver.Resolve(rules, DayOfWeek.Monday, 10 * Hour));
        Assert.Null(PriceResolver.Resolve([], DayOfWeek.Monday, 10 * Hour));
    }

    [Fact]
    public void Resolve_WindowEndIsExclusive()
    {
        var rules = new[] { Rule(100, 8 * Hour, 12 * Hour) };

        Assert.Equal(100, PriceResolver.Resolve(rules, DayOfWeek.Monday, 12 * Hour - 1));
        Assert.Null(PriceResolver.Resolve(rules, DayOfWeek.Monday, 12 * Hour));
    }

    [Theory]
    [InlineData(0, 0, 100, false)]
    [InlineData(null, 600, 600, false)]
    [InlineData(null, 0, 1500, false)]
    [InlineData(null, 0, 1440, true)]
    [InlineData(1, 0, 1440, true)]
    public void Validate_RejectsBadRules(int? days, int from, int to, bool expectedValid)
    {
        var error = PriceRuleValidator.Validate(days is null ? null : (Weekdays)days, from, to, 100);

        Assert.Equal(expectedValid, error is null);
    }
}
