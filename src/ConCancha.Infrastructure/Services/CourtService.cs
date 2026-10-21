using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Courts;
using ConCancha.Domain.Courts;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Services.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

public sealed class CourtService(ConCanchaDbContext db) : ICourtService
{
    private const string NameField = "name";
    private const string SlotMinutesField = "slotMinutes";
    private const string OpeningField = "openingMinuteOfDay";

    public async Task<IReadOnlyList<CourtDto>> ListAsync(CancellationToken cancellationToken)
    {
        var courts = await db.Courts.AsNoTracking()
            .OrderBy(court => court.Sport).ThenBy(court => court.CreatedAt).ThenBy(court => court.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return courts.Select(CourtMapper.ToDto).ToList();
    }

    public async Task<Result<CourtDto>> CreateAsync(CourtRequest request, CancellationToken cancellationToken)
    {
        if (Validate(request) is { } invalid) return Result<CourtDto>.Failure(invalid);

        var name = request.Name.Trim();
        if (await NameExistsAsync(name, excludedId: null, cancellationToken).ConfigureAwait(false))
            return Result<CourtDto>.Failure(ServiceError.Conflict(CourtMessages.DuplicateName));

        var court = Apply(new Court(), request, name);
        db.Courts.Add(court);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<CourtDto>.Success(CourtMapper.ToDto(court));
    }

    public async Task<Result<CourtDto>> UpdateAsync(Guid courtId, CourtRequest request, CancellationToken cancellationToken)
    {
        if (Validate(request) is { } invalid) return Result<CourtDto>.Failure(invalid);

        var court = await db.Courts.FirstOrDefaultAsync(c => c.Id == courtId, cancellationToken).ConfigureAwait(false);
        if (court is null) return Result<CourtDto>.Failure(ServiceError.NotFound(CourtMessages.CourtNotFound));

        var name = request.Name.Trim();
        if (await NameExistsAsync(name, courtId, cancellationToken).ConfigureAwait(false))
            return Result<CourtDto>.Failure(ServiceError.Conflict(CourtMessages.DuplicateName));

        Apply(court, request, name);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<CourtDto>.Success(CourtMapper.ToDto(court));
    }

    private Task<bool> NameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken) =>
        db.Courts.AnyAsync(court => court.Name == name && court.Id != excludedId, cancellationToken);

    private static Court Apply(Court court, CourtRequest request, string name)
    {
        court.Name = name;
        court.Sport = request.Sport;
        court.SlotMinutes = request.SlotMinutes;
        court.OpeningMinuteOfDay = request.OpeningMinuteOfDay;
        court.ClosingMinuteOfDay = request.ClosingMinuteOfDay;
        court.IsActive = request.IsActive;
        return court;
    }

    private static ServiceError? Validate(CourtRequest request)
    {
        if (!Enum.IsDefined(request.Sport))
            return ServiceError.Validation(nameof(request.Sport).ToLowerInvariant(), "Elegí un deporte.");
        if (!CourtLimits.AllowedSlotMinutes.Contains(request.SlotMinutes))
            return ServiceError.Validation(SlotMinutesField, CourtMessages.InvalidSlotMinutes);

        return ValidateHours(request);
    }

    private static ServiceError? ValidateHours(CourtRequest request)
    {
        var hasOpening = request.OpeningMinuteOfDay is not null;
        if (hasOpening != (request.ClosingMinuteOfDay is not null))
            return ServiceError.Validation(OpeningField, CourtMessages.HoursPairRequired);
        if (!hasOpening) return null;

        return OperatingHours.Validate(request.OpeningMinuteOfDay!.Value, request.ClosingMinuteOfDay!.Value) is { } reason
            ? ServiceError.Validation(OpeningField, reason)
            : null;
    }
}
