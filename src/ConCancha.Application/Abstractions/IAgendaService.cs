using ConCancha.Contracts.Agenda;

namespace ConCancha.Application.Abstractions;

public interface IAgendaService
{
    Task<AgendaDayDto> GetDayAsync(DateOnly date, CancellationToken cancellationToken);
}
