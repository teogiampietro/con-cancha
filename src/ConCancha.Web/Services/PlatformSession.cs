using System.Security.Cryptography;
using ConCancha.Contracts.Accounts;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace ConCancha.Web.Services;

/// <summary>
/// Session of ConCancha's own panel. Kept under its own key, so a club session and the platform session can be open
/// in the same browser without replacing each other.
/// </summary>
public sealed class PlatformSession(ProtectedLocalStorage storage)
{
    private const string StorageKey = "concancha.platform-session";
    private bool _isLoaded;

    public SessionResponse? Current { get; private set; }

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
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Current = null;
        _isLoaded = true;
        await storage.DeleteAsync(StorageKey);
    }
}
