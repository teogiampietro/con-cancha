using ConCancha.Api.Auth;
using ConCancha.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ConCancha.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();
        services.AddSingleton<ISessionTokenIssuer, SessionTokenIssuer>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddSingleton<IAuthorizationHandler, PlatformAdminHandler>();
        services.AddHostedService<PlatformCredentialsWarning>();
        services.AddAuthorization(options => options.AddPolicy(PlatformPolicies.Admin, policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new PlatformAdminRequirement())));
        return services;
    }
}
