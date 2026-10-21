using ConCancha.Application.Abstractions;
using ConCancha.Domain.Accounts;
using ConCancha.Infrastructure.Email;
using ConCancha.Infrastructure.Identity;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Services;
using ConCancha.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConCancha.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Default";
    private const string LeakedPasswordsApiUrl = "https://api.pwnedpasswords.com/";
    private const int LeakedPasswordsTimeoutSeconds = 3;
    private const int MaxFailedAccessAttempts = 5;
    private const int LockoutMinutes = 10;
    private const string AddPaddingHeader = "Add-Padding";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddOptions<WebAppOptions>().BindConfiguration(WebAppOptions.SectionName);
        services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.SectionName);

        return services
            .AddPersistence()
            .AddIdentityServices()
            .AddApplicationServices();
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services) =>
        services.AddDbContext<ConCanchaDbContext>((provider, options) =>
        {
            // Read when the context is created (not when the container is built) so tests can override it.
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Missing connection string 'ConnectionStrings:{ConnectionStringName}'.");
            options.UseNpgsql(connectionString);
        });

    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddHttpClient(LeakedPasswordValidator.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(LeakedPasswordsApiUrl);
            client.Timeout = TimeSpan.FromSeconds(LeakedPasswordsTimeoutSeconds);
            client.DefaultRequestHeaders.Add(AddPaddingHeader, bool.TrueString.ToLowerInvariant());
        });

        services.AddIdentityCore<AppUser>(ConfigureIdentity)
            .AddEntityFrameworkStores<ConCanchaDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<SpanishIdentityErrorDescriber>()
            .AddPasswordValidator<LeakedPasswordValidator>();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<SessionFactory>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IClubService, ClubService>();
        services.AddScoped<CurrentClubProvider>();
        services.AddScoped<ICourtService, CourtService>();
        services.AddScoped<IPriceRuleService, PriceRuleService>();
        services.AddScoped<IAgendaService, AgendaService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ISetupService, SetupService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<ICustomerDirectoryService, CustomerDirectoryService>();
        services.AddScoped<ICashService, CashService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<SlotBooker>();
        services.AddScoped<CustomerResolver>();
        services.AddScoped<IPublicBookingService, PublicBookingService>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        return services;
    }

    private static void ConfigureIdentity(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = AccountLimits.PasswordMinLength;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = MaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(LockoutMinutes);
    }
}
