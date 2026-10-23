using ConCancha.Application.Results;

namespace ConCancha.Application.Abstractions;

/// <summary>What ConCancha's own team does with a club's trial and payments. Reads across clubs, like the usage figures.</summary>
public interface IPlatformSubscriptionService
{
    Task<Result> ExtendTrialAsync(Guid clubId, int days, CancellationToken cancellationToken);

    /// <summary>Records a payment received outside the app.</summary>
    Task<Result> MarkPaidAsync(Guid clubId, CancellationToken cancellationToken);

    /// <summary>Emails the club's owner about an overdue trial or payment.</summary>
    Task<Result> SendRegularizationEmailAsync(Guid clubId, CancellationToken cancellationToken);
}
