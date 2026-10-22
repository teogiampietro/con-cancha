using ConCancha.Application.Results;
using ConCancha.Contracts.Public;

namespace ConCancha.Application.Abstractions;

/// <summary>What a player without an account can do on a club's public page. Every call is scoped to the club's slug.</summary>
public interface IPublicBookingService
{
    /// <param name="visitorToken">Marks the slots this browser booked as <see cref="PublicSlotState.Mine"/>; optional.</param>
    Task<Result<PublicDayDto>> GetDayAsync(string slug, DateOnly date, string? visitorToken, CancellationToken cancellationToken);

    Task<Result<PublicBookingCreatedDto>> BookAsync(
        string slug, PublicBookingRequest request, string? visitorToken, CancellationToken cancellationToken);

    Task<Result<PublicBookingDto>> GetBookingAsync(string slug, string token, CancellationToken cancellationToken);

    Task<Result<PublicBookingDto>> CancelAsync(string slug, string token, CancellationToken cancellationToken);

    Task<Result<PublicMyBookingsDto>> GetMyBookingsAsync(string slug, string? visitorToken, CancellationToken cancellationToken);

    Task<Result<PublicMyBookingsDto>> CancelMineAsync(
        string slug, Guid bookingId, string? visitorToken, CancellationToken cancellationToken);
}
