using ConCancha.Application.Abstractions;
using ConCancha.Application.Messages;
using ConCancha.Application.Results;
using ConCancha.Contracts.Clubs;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConCancha.Infrastructure.Services;

public sealed class ClubAppearanceService(
    ConCanchaDbContext db,
    ITenantContext tenant,
    IFileStore files,
    ILogger<ClubAppearanceService> logger) : IClubAppearanceService
{
    private const string LogoField = "logo";
    private const string PrimaryColorField = "primaryColor";
    private const string SecondaryColorField = "secondaryColor";
    private const string ModulesField = "enabledModules";
    private const string LogoFolderPrefix = "logos";
    private const string InvalidModulesMessage = "Hay secciones que no existen.";

    public async Task<Result<ClubDto>> UpdateColorsAsync(UpdateClubColorsRequest request, CancellationToken cancellationToken)
    {
        var primary = ClubBranding.Normalize(request.PrimaryColor);
        var secondary = ClubBranding.Normalize(request.SecondaryColor);
        var errors = ColorErrors(primary, secondary);
        if (errors.Count > 0)
            return Result<ClubDto>.Failure(new ServiceError(ErrorKind.Validation, errors.First().Value[0], errors));

        return await UpdateCurrentClubAsync(club =>
        {
            club.PrimaryColor = primary;
            club.SecondaryColor = secondary;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<ClubDto>> UpdateLogoAsync(Stream content, CancellationToken cancellationToken)
    {
        var buffer = await ReadBoundedAsync(content, cancellationToken).ConfigureAwait(false);
        if (buffer.Length > LogoRules.MaxBytes)
            return Result<ClubDto>.Failure(ServiceError.Validation(LogoField, LogoRules.FileTooLargeMessage));
        if (LogoRules.Detect(buffer) is not { } format)
            return Result<ClubDto>.Failure(ServiceError.Validation(LogoField, LogoRules.NotAnImageMessage));

        var previousKey = default(string);
        var result = await UpdateCurrentClubAsync(async club =>
        {
            previousKey = club.LogoPath;
            club.LogoPath = await SaveLogoAsync(club.Id, format, buffer, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

        await DeletePreviousLogoAsync(result, previousKey, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<Result<ClubDto>> RemoveLogoAsync(CancellationToken cancellationToken)
    {
        var previousKey = default(string);
        var result = await UpdateCurrentClubAsync(club =>
        {
            previousKey = club.LogoPath;
            club.LogoPath = null;
        }, cancellationToken).ConfigureAwait(false);

        await DeletePreviousLogoAsync(result, previousKey, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<Result<ClubDto>> UpdateModulesAsync(UpdateClubModulesRequest request, CancellationToken cancellationToken)
    {
        if ((request.EnabledModules & ~ClubModules.All) != ClubModules.None)
            return Result<ClubDto>.Failure(ServiceError.Validation(ModulesField, InvalidModulesMessage));

        var result = await UpdateCurrentClubAsync(club => club.EnabledModules = request.EnabledModules, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess) logger.LogInformation("Club {ClubId} sections set to {Modules}", result.Value!.Id, request.EnabledModules);
        return result;
    }

    public async Task<Result<LogoFile>> OpenPublicLogoAsync(string slug, CancellationToken cancellationToken)
    {
        var key = await db.Clubs.AsNoTracking()
            .Where(club => club.Slug == slug.ToLowerInvariant())
            .Select(club => club.LogoPath)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var stream = key is null ? null : await files.OpenReadAsync(key, cancellationToken).ConfigureAwait(false);
        return stream is null
            ? Result<LogoFile>.Failure(ServiceError.NotFound(ClubMessages.LogoNotFound))
            : Result<LogoFile>.Success(new LogoFile(stream, ContentTypeOf(key!)));
    }

    public async Task<bool> IsModuleEnabledAsync(ClubModules module, CancellationToken cancellationToken)
    {
        if (tenant.ClubId is not { } clubId) return false;

        var enabled = await db.Clubs.AsNoTracking()
            .Where(club => club.Id == clubId)
            .Select(club => (ClubModules?)club.EnabledModules)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return enabled is { } modules && modules.HasFlag(module);
    }

    private static Dictionary<string, string[]> ColorErrors(string primary, string secondary)
    {
        var errors = new Dictionary<string, string[]>();
        if (ClubBranding.Validate(primary) is { } primaryReason) errors[PrimaryColorField] = [primaryReason];
        if (ClubBranding.Validate(secondary) is { } secondaryReason) errors[SecondaryColorField] = [secondaryReason];
        return errors;
    }

    /// <summary>Reads at most one byte more than the limit, enough to know the file is too large without loading all of it.</summary>
    private static async Task<byte[]> ReadBoundedAsync(Stream content, CancellationToken cancellationToken)
    {
        var buffer = new byte[LogoRules.MaxBytes + 1];
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await content.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            total += read;
        }
        return buffer[..total];
    }

    private async Task<string> SaveLogoAsync(Guid clubId, LogoFormat format, byte[] content, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(content);
        return await files.SaveAsync($"{LogoFolderPrefix}/{clubId:N}", LogoRules.Extension(format), stream, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task DeletePreviousLogoAsync(Result<ClubDto> result, string? previousKey, CancellationToken cancellationToken)
    {
        if (!result.IsSuccess || previousKey is null) return;
        try
        {
            await files.DeleteAsync(previousKey, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Previous logo {LogoKey} could not be deleted", previousKey);
        }
    }

    private static string ContentTypeOf(string key) =>
        LogoRules.ContentType(key.EndsWith($".{LogoRules.Extension(LogoFormat.Png)}", StringComparison.Ordinal) ? LogoFormat.Png : LogoFormat.Jpeg);

    private Task<Result<ClubDto>> UpdateCurrentClubAsync(Action<Club> apply, CancellationToken cancellationToken) =>
        UpdateCurrentClubAsync(club =>
        {
            apply(club);
            return Task.CompletedTask;
        }, cancellationToken);

    private async Task<Result<ClubDto>> UpdateCurrentClubAsync(Func<Club, Task> apply, CancellationToken cancellationToken)
    {
        if (tenant.ClubId is not { } clubId)
            return Result<ClubDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));

        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null) return Result<ClubDto>.Failure(ServiceError.NotFound(ClubMessages.ClubNotFound));

        await apply(club).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<ClubDto>.Success(ClubMapper.ToDto(club));
    }
}
