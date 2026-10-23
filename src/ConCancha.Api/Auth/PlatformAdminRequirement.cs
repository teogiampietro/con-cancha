using Microsoft.AspNetCore.Authorization;

namespace ConCancha.Api.Auth;

/// <summary>
/// The platform role, and a fingerprint of the password hash in use: changing the hash closes every platform session
/// right away, without waiting for the tokens to expire.
/// </summary>
public sealed class PlatformAdminRequirement : IAuthorizationRequirement;
