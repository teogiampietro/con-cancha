using ConCancha.Domain.Courts;

namespace ConCancha.Tests.Domain;

public class SlotGeneratorTests
{
    private const int Hour = 60;

    [Fact]
    public void GetStartMinutes_NinetyMinuteSlots_StartAtOpeningAndEndByClosing()
    {
        var starts = SlotGenerator.GetStartMinutes(new OperatingHours(9 * Hour, 24 * Hour), 90);

        Assert.Equal(9 * Hour, starts[0]);
        Assert.Equal(22 * Hour + 30, starts[^1]);
        Assert.All(starts, start => Assert.True(start + 90 <= 24 * Hour));
    }

    [Fact]
    public void GetStartMinutes_SixtyMinuteSlots_FillTheWholeDay()
    {
        var starts = SlotGenerator.GetStartMinutes(new OperatingHours(8 * Hour, 24 * Hour), 60);

        Assert.Equal(16, starts.Count);
        Assert.Equal(23 * Hour, starts[^1]);
    }

    [Fact]
    public void GetStartMinutes_LeftoverTimeShorterThanASlot_IsDropped()
    {
        var starts = SlotGenerator.GetStartMinutes(new OperatingHours(8 * Hour, 10 * Hour), 90);

        Assert.Equal([8 * Hour], starts);
    }

    [Fact]
    public void OperatingHours_CourtOverrideWinsOverClubHours()
    {
        var court = new Court { OpeningMinuteOfDay = 10 * Hour, ClosingMinuteOfDay = 20 * Hour };

        Assert.Equal(new OperatingHours(10 * Hour, 20 * Hour), OperatingHours.For(court, 8 * Hour, 24 * Hour));
        Assert.Equal(new OperatingHours(8 * Hour, 24 * Hour), OperatingHours.For(new Court(), 8 * Hour, 24 * Hour));
    }
}
