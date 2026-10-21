namespace ConCancha.Contracts.Agenda;

/// <param name="OccupancyPercent">Taken slots (reservations and blocks) over all slots of the day.</param>
/// <param name="Collected">What already came in from the day's turns ("Entró").</param>
/// <param name="Missed">Price of the free slots whose time already passed ("Se escapa").</param>
/// <param name="ToCollect">What the day's turns still owe.</param>
public record AgendaStatsDto(int OccupiedSlots, int TotalSlots, int OccupancyPercent, decimal Collected, decimal Missed, decimal ToCollect);
