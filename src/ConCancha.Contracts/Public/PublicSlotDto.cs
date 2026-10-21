namespace ConCancha.Contracts.Public;

public record PublicSlotDto(int StartMinuteOfDay, int EndMinuteOfDay, decimal? Price, PublicSlotState State);
