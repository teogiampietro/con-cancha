using ConCancha.Application.Results;
using ConCancha.Contracts.Cash;

namespace ConCancha.Application.Abstractions;

public interface ICashService
{
    Task<CashDayDto> GetDayAsync(DateOnly date, CancellationToken cancellationToken);

    Task<CashDayDto> CloseDayAsync(DateOnly date, CancellationToken cancellationToken);

    Task<CashDayDto> ReopenDayAsync(DateOnly date, CancellationToken cancellationToken);
}
