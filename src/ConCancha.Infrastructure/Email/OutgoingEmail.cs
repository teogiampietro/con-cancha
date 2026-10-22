namespace ConCancha.Infrastructure.Email;

public sealed record OutgoingEmail(string To, string Subject, string Body);
