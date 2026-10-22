namespace ConCancha.Infrastructure.Email;

public sealed class EmailDeliveryException(string message, bool isTransient, Exception? innerException = null)
    : Exception(message, innerException)
{
    public bool IsTransient { get; } = isTransient;
}
