using ConCancha.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace ConCancha.Tests.Infrastructure;

public class PlatformCredentialsTests
{
    private const string Admin = "admin-prueba";
    private const string Password = "clave-admin-de-pruebas-larga";

    [Fact]
    public void OnlyTheConfiguredUserAndPasswordMatch()
    {
        var credentials = Configured(Admin, PlatformPasswords.Hash(Password));

        Assert.True(credentials.Matches(Admin, Password));
        Assert.False(credentials.Matches("otra", Password));
        Assert.False(credentials.Matches(Admin, "otra-clave"));
    }

    [Fact]
    public void NothingMatchesWhenThePlatformIsNotConfigured()
    {
        var missingHash = new PlatformCredentials(new Monitor(new PlatformOptions { AdminUser = Admin }));
        var missingUser = new PlatformCredentials(new Monitor(new PlatformOptions { AdminPasswordHash = PlatformPasswords.Hash(Password) }));

        Assert.False(missingHash.Matches(Admin, Password));
        Assert.False(missingUser.Matches(Admin, Password));
        Assert.Null(missingHash.CurrentFingerprint);
    }

    [Fact]
    public void FingerprintFollowsTheHashWithoutRevealingIt()
    {
        var hash = PlatformPasswords.Hash(Password);
        var fingerprint = Configured(Admin, hash).CurrentFingerprint;

        Assert.Equal(fingerprint, Configured(Admin, hash).CurrentFingerprint);
        Assert.NotEqual(fingerprint, Configured(Admin, PlatformPasswords.Hash(Password)).CurrentFingerprint);
        Assert.DoesNotContain(hash, fingerprint);
    }

    [Fact]
    public void HashesAreSaltedButBothVerify()
    {
        var first = PlatformPasswords.Hash(Password);
        var second = PlatformPasswords.Hash(Password);

        Assert.NotEqual(first, second);
        Assert.True(PlatformPasswords.Verify(first, Password));
        Assert.True(PlatformPasswords.Verify(second, Password));
        Assert.False(PlatformPasswords.Verify(first, "otra-clave"));
    }

    private static PlatformCredentials Configured(string user, string hash) =>
        new(new Monitor(new PlatformOptions { AdminUser = user, AdminPasswordHash = hash }));

    private sealed class Monitor(PlatformOptions value) : IOptionsMonitor<PlatformOptions>
    {
        public PlatformOptions CurrentValue => value;

        public PlatformOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<PlatformOptions, string?> listener) => null;
    }
}
