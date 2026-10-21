using ConCancha.Application.Abstractions;

namespace ConCancha.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public Guid? ClubId { get; private set; }

    public void SetClub(Guid clubId) => ClubId = clubId;
}
