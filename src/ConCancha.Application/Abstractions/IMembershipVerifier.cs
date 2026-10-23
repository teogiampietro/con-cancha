namespace ConCancha.Application.Abstractions;

/// <summary>A token outlives a membership, so requests are checked against the current one.</summary>
public interface IMembershipVerifier
{
    Task<bool> HasMembershipAsync(string userId, Guid clubId, string role, CancellationToken cancellationToken);
}
