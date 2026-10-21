using ConCancha.Application.Results;
using ConCancha.Contracts.Reminders;

namespace ConCancha.Application.Abstractions;

public interface IReminderService
{
    Task<ReminderListDto> ListAsync(ReminderDay day, CancellationToken cancellationToken);

    Task<Result> MarkSentAsync(Guid bookingId, CancellationToken cancellationToken);
}
