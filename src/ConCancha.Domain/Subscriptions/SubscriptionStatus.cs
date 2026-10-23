namespace ConCancha.Domain.Subscriptions;

public enum SubscriptionStatus
{
    /// <summary>The setup is not finished, so the trial has not started yet.</summary>
    TrialNotStarted = 1,

    /// <summary>Inside the trial.</summary>
    Trial = 2,

    /// <summary>The trial is over and there is no subscription: the panel is closed.</summary>
    TrialExpired = 3,

    /// <summary>Paid up: the current month is covered.</summary>
    Subscribed = 4,

    /// <summary>The month ran out and the subscription renewed by itself; the payment still has to be recorded.</summary>
    PaymentPending = 5,
}
