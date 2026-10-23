using ConCancha.Application.Abstractions;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

public sealed class MembershipVerifier(ConCanchaDbContext db) : IMembershipVerifier
{
    public async Task<bool> HasMembershipAsync(string userId, Guid clubId, string role, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ClubRole>(role, out var clubRole)) return false;

        return await db.ClubMembers.IgnoreQueryFilters()
            .AnyAsync(member => member.ClubId == clubId && member.UserId == userId && member.Role == clubRole, cancellationToken)
            .ConfigureAwait(false);
    }
}
