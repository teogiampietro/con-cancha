namespace ConCancha.Application.Abstractions;

/// <summary>The request's "current" club. Set by authentication (panel) or by slug (public site).</summary>
public interface ITenantContext
{
    Guid? ClubId { get; }

    void SetClub(Guid clubId);
}
