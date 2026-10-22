namespace ConCancha.Infrastructure.Email;

/// <summary>Keeps addresses out of the logs while leaving enough to tell deliveries apart.</summary>
public static class EmailRecipientMask
{
    private const char At = '@';
    private const string Hidden = "***";

    public static string Mask(string address)
    {
        var at = address.IndexOf(At);
        if (at <= 0)
            return Hidden;

        return $"{address[0]}{Hidden}{address[at..]}";
    }
}
