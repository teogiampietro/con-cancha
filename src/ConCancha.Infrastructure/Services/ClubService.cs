using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Courts;
using ConCancha.Infrastructure.Identity;
using ConCancha.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

public sealed class ClubService(
    ConCanchaDbContext db,
    UserManager<AppUser> users,
    ITenantContext tenant,
    SessionFactory sessionFactory,
    RecurringBookingReconciler recurringReconciler,
    ILogger<ClubService> logger) : IClubService
{
    private const string SlugField = "slug";
    private const string HoursField = "openingMinuteOfDay";
    private const string CancellationWindowField = "windowHours";

    public async Task<Result<SessionResponse>> CreateClubForUserAsync(
        string userId, CreateClubRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            return Result<SessionResponse>.Failure(ServiceError.Unauthorized(ClubMessages.UserNotFound));

        if (await UserHasClubAsync(userId, cancellationToken).ConfigureAwait(false))
            return Result<SessionResponse>.Failure(ServiceError.Conflict(ClubMessages.AccountAlreadyHasClub));

        var slug = ResolveSlug(request);
        if (SlugRules.Validate(slug) is { } reason)
            return Result<SessionResponse>.Failure(ServiceError.Validation(SlugField, reason));

        if (await IsSlugTakenAsync(slug, cancellationToken).ConfigureAwait(false))
            return Result<SessionResponse>.Failure(ServiceError.Conflict(ClubMessages.SlugAlreadyTaken));

        return await SaveClubWithOwnerAsync(user, request, slug, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SlugAvailabilityResponse> GetSlugAvailabilityAsync(string slug, CancellationToken cancellationToken)
    {
        var normalized = (slug ?? string.Empty).Trim().ToLowerInvariant();
        if (SlugRules.Validate(normalized) is { } reason)
            return new SlugAvailabilityResponse(normalized, false, reason);

        return await IsSlugTakenAsync(normalized, cancellationToken).ConfigureAwait(false)
            ? new SlugAvailabilityResponse(normalized, false, ClubMessages.SlugAlreadyTaken)
            : new SlugAvailabilityResponse(normalized, true, null);
    }

    public async Task<Result<ClubDto>> GetCurrentClubAsync(CancellationToken cancellationToken)
    {
        if (tenant.ClubId is not { } clubId)
            return Result<ClubDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));

        var club = await db.Clubs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);

        return club is null
            ? Result<ClubDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound))
            : Result<ClubDto>.Success(ClubMapper.ToDto(club));
    }

    public async Task<Result<ClubDto>> UpdateHoursAsync(UpdateClubHoursRequest request, CancellationToken cancellationToken)
    {
        if (OperatingHours.Validate(request.OpeningMinuteOfDay, request.ClosingMinuteOfDay) is { } reason)
            return Result<ClubDto>.Failure(ServiceError.Validation(HoursField, reason));

        var result = await UpdateCurrentClubAsync(club =>
        {
            club.OpeningMinuteOfDay = request.OpeningMinuteOfDay;
            club.ClosingMinuteOfDay = request.ClosingMinuteOfDay;
        }, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess) await recurringReconciler.ReconcileClubHoursAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<Result<ClubDto>> UpdateProfileAsync(UpdateClubProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await UpdateCurrentClubAsync(club =>
        {
            club.Name = request.Name.Trim();
            club.Phone = NormalizePhone(request.Phone);
        }, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess) logger.LogInformation("Club {ClubId} profile updated", result.Value!.Id);
        return result;
    }

    public async Task<Result<ClubDto>> UpdateCancellationAsync(UpdateCancellationRequest request, CancellationToken cancellationToken)
    {
        if (BookingPolicy.ValidateCancellationWindow(request.WindowHours) is { } reason)
            return Result<ClubDto>.Failure(ServiceError.Validation(CancellationWindowField, reason));

        var result = await UpdateCurrentClubAsync(club => club.CancellationWindowHours = request.WindowHours, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
            logger.LogInformation("Club {ClubId} cancellation window set to {Hours} hours", result.Value!.Id, request.WindowHours);
        return result;
    }

    /// <summary>Also makes the club the request's current club, so later queries are scoped to it.</summary>
    public async Task<Result<PublicClubDto>> ResolvePublicClubBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var normalized = slug.ToLowerInvariant();
        var club = await db.Clubs.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == normalized, cancellationToken).ConfigureAwait(false);
        if (club is null)
            return Result<PublicClubDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));

        tenant.SetClub(club.Id);
        return Result<PublicClubDto>.Success(ClubMapper.ToPublicDto(club));
    }

    private async Task<Result<ClubDto>> UpdateCurrentClubAsync(Action<Club> apply, CancellationToken cancellationToken)
    {
        if (tenant.ClubId is not { } clubId)
            return Result<ClubDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));

        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null) return Result<ClubDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));

        apply(club);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<ClubDto>.Success(ClubMapper.ToDto(club));
    }

    private async Task<Result<SessionResponse>> SaveClubWithOwnerAsync(
        AppUser user, CreateClubRequest request, string slug, CancellationToken cancellationToken)
    {
        var club = new Club { Name = request.Name.Trim(), Slug = slug, Phone = NormalizePhone(request.Phone) };
        db.Clubs.Add(club);
        db.ClubMembers.Add(new ClubMember { ClubId = club.Id, UserId = user.Id, Role = ClubRole.Owner });

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception)
        {
            // Race: another club took the same slug between the check and the save (unique index).
            logger.LogWarning(exception, "Club {Slug} could not be saved; assuming the slug was taken in between", slug);
            return Result<SessionResponse>.Failure(ServiceError.Conflict(ClubMessages.SlugAlreadyTaken));
        }

        logger.LogInformation("Club {ClubId} ({Slug}) created by user {UserId}", club.Id, slug, user.Id);
        return Result<SessionResponse>.Success(sessionFactory.Create(user, club, ClubRole.Owner));
    }

    private Task<bool> UserHasClubAsync(string userId, CancellationToken cancellationToken) =>
        db.ClubMembers.IgnoreQueryFilters().AnyAsync(member => member.UserId == userId, cancellationToken);

    private Task<bool> IsSlugTakenAsync(string slug, CancellationToken cancellationToken) =>
        db.Clubs.AnyAsync(club => club.Slug == slug, cancellationToken);

    private static string ResolveSlug(CreateClubRequest request) =>
        string.IsNullOrWhiteSpace(request.Slug)
            ? SlugRules.Normalize(request.Name)
            : request.Slug.Trim().ToLowerInvariant();

    private static string? NormalizePhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
}
