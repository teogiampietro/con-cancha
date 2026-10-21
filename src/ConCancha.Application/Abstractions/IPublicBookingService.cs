using ConCancha.Application.Results;
using ConCancha.Contracts.Public;

namespace ConCancha.Application.Abstractions;

/// <summary>What a player without an account can do on a club's public page. Every call is scoped to the club's slug.</summary>
public interface IPublicBookingService
{
    Task<Result<PublicDayDto>> GetDayAsync(string slug, DateOnly date, CancellationToken cancellationToken);

    Task<Result<PublicBookingCreatedDto>> BookAsync(string slug, PublicBookingRequest request, CancellationToken cancellationToken);

    Task<Result<PublicBookingDto>> GetBookingAsync(string slug, string token, CancellationToken cancellationToken);

    Task<Result<PublicBookingDto>> CancelAsync(string slug, string token, CancellationToken cancellationToken);
}
