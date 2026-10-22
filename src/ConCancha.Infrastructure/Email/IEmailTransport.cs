namespace ConCancha.Infrastructure.Email;

public interface IEmailTransport
{
    Task DeliverAsync(OutgoingEmail email, CancellationToken cancellationToken);
}
