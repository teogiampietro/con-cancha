namespace ConCancha.Api.Subscriptions;

/// <summary>Keeps an endpoint open while the club's trial is over: the panel needs it to explain why it is blocked.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowWithoutSubscriptionAttribute : Attribute;
