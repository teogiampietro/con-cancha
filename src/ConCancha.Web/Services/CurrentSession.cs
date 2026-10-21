using System.Security.Cryptography;
using ConCancha.Contracts.Accounts;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace ConCancha.Web.Services;

/// <summary>
/// Session of the person using the panel. Stored encrypted in the browser (survives a page reload).
/// Only the API validates the token; here it is used to show the right interface.
/// </summary>
public sealed class CurrentSession(ProtectedLocalStorage storage)
{
    private const string StorageKey = "concancha.session";
    private bool _isLoaded;

    public SessionResponse? Current { get; private set; }
    public bool HasClub => Current?.Club is not null;
    public event Action? Changed;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (_isLoaded) return;

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var stored = await storage.GetAsync<SessionResponse>(StorageKey);
            Current = stored.Success && stored.Value is { } session && session.ExpiresAt > DateTimeOffset.UtcNow ? session : null;
            _isLoaded = true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or CryptographicException)
        {
            // No JavaScript yet (prerender) or unreadable data: treated as no session.
            Current = null;
        }
    }

    public async Task SaveAsync(SessionResponse session, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Current = session;
        _isLoaded = true;
        await storage.SetAsync(StorageKey, session);
        Changed?.Invoke();
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Current = null;
        _isLoaded = true;
        await storage.DeleteAsync(StorageKey);
        Changed?.Invoke();
    }
}
