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

    [Theory]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(120)]
    public void GetStartMinutes_NoSlotCrossesMidnight_AndNoSlotsOverlap(int slotMinutes)
    {
        var starts = SlotGenerator.GetStartMinutes(new OperatingHours(8 * Hour, 24 * Hour), slotMinutes);

        Assert.All(starts, start => Assert.True(start + slotMinutes <= 24 * Hour));
        Assert.All(starts.Zip(starts.Skip(1)), pair => Assert.Equal(slotMinutes, pair.Second - pair.First));
    }

    [Fact]
    public void OperatingHours_CourtOverrideWinsOverClubHours()
    {
        var court = new Court { OpeningMinuteOfDay = 10 * Hour, ClosingMinuteOfDay = 20 * Hour };

        Assert.Equal(new OperatingHours(10 * Hour, 20 * Hour), OperatingHours.For(court, 8 * Hour, 24 * Hour));
        Assert.Equal(new OperatingHours(8 * Hour, 24 * Hour), OperatingHours.For(new Court(), 8 * Hour, 24 * Hour));
    }
}
