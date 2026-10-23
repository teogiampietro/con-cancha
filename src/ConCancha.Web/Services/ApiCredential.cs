namespace ConCancha.Web.Services;

/// <summary>Which session's token a request carries. Sign-in requests carry none.</summary>
public enum ApiCredential
{
    Club,
    Platform,
    None,
}
