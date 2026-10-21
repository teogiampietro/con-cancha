namespace ConCancha.Domain.Courts;

/// <summary>Free slots are computed, never stored: they start at opening and every slot ends by closing.</summary>
public static class SlotGenerator
{
    public static IReadOnlyList<int> GetStartMinutes(OperatingHours hours, int slotMinutes)
    {
        var starts = new List<int>();
        for (var start = hours.OpeningMinuteOfDay; start + slotMinutes <= hours.ClosingMinuteOfDay; start += slotMinutes)
            starts.Add(start);
        return starts;
    }
}
