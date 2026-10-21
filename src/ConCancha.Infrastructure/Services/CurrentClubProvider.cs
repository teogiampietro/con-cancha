using ConCancha.Application.Abstractions;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

/// <summary>The request's club, loaded at most once per request.</summary>
public sealed class CurrentClubProvider(ConCanchaDbContext db, ITenantContext tenant)
{
    private Club? _club;

    public async Task<Club?> GetAsync(CancellationToken cancellationToken)
    {
        if (_club is not null || tenant.ClubId is not { } clubId) return _club;

        _club = await db.Clubs.FirstOrDefaultAsync(club => club.Id == clubId, cancellationToken).ConfigureAwait(false);
        return _club;
    }
}
