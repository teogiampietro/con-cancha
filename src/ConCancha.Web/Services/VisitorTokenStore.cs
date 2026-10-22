using System.Security.Cryptography;
using Microsoft.JSInterop;

namespace ConCancha.Web.Services;

/// <summary>Keeps in this browser the secret that lets the club page recognise the visitor's own bookings.</summary>
public sealed class VisitorTokenStore(IJSRuntime js)
{
    private const string StorageKey = "concancha.visitor";
    private const string GetItemFunction = "localStorage.getItem";
    private const string SetItemFunction = "localStorage.setItem";
    private const int TokenBytes = 32;

    private string? _token;

    public async Task<string?> GetAsync(CancellationToken cancellationToken)
    {
        if (_token is not null) return _token;

        try
        {
            _token = await js.InvokeAsync<string?>(GetItemFunction, cancellationToken, StorageKey);
        }
        catch (JSException)
        {
            return null;
        }
        return _token;
    }

    public async Task<string?> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        if (await GetAsync(cancellationToken) is { } existing) return existing;

        var created = Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenBytes)).ToLowerInvariant();
        try
        {
            await js.InvokeVoidAsync(SetItemFunction, cancellationToken, StorageKey, created);
        }
        catch (JSException)
        {
            return null;
        }
        return _token = created;
    }
}
