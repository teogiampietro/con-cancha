using System.Threading.RateLimiting;
using ConCancha.Api.RateLimiting;
using Microsoft.Extensions.Options;

namespace ConCancha.Api.Extensions;

public static class RateLimitingExtensions
{
    private const string UnknownIp = "no-ip";
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>Per-IP limit on sensitive endpoints (sign-up, login, password recovery, slug availability).</summary>
    public static IServiceCollection AddIpRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitOptions>().BindConfiguration(RateLimitOptions.SectionName);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Auth, context => LimitByIp(context, RateLimitPolicies.Auth, o => o.AuthPerMinute));
            options.AddPolicy(RateLimitPolicies.PublicBooking, context => LimitByIp(context, RateLimitPolicies.PublicBooking, o => o.PublicBookingPerMinute));
            options.AddPolicy(RateLimitPolicies.Query, context => LimitByIp(context, RateLimitPolicies.Query, o => o.QueryPerMinute));
        });
        return services;
    }

    private static RateLimitPartition<string> LimitByIp(
        HttpContext context, string policyName, Func<RateLimitOptions, int> selectLimit)
    {
        var limit = selectLimit(context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value);
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? UnknownIp;

        return RateLimitPartition.GetFixedWindowLimiter(
            $"{policyName}:{ip}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = Window });
    }
}
