using ConCancha.Application.Abstractions;
using ConCancha.Domain.Accounts;
using ConCancha.Infrastructure.Email;
using ConCancha.Infrastructure.Identity;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Services;
using ConCancha.Infrastructure.Storage;
using ConCancha.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ConCancha.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Default";
    private const string LeakedPasswordsApiUrl = "https://api.pwnedpasswords.com/";
    private const int LeakedPasswordsTimeoutSeconds = 3;
    private const int MaxFailedAccessAttempts = 5;
    private const int LockoutMinutes = 10;
    private const string AddPaddingHeader = "Add-Padding";
    private const int EmailProviderTimeoutSeconds = 10;
    private const string IncompleteEmailSettings =
        "Email:From and Email:ResendApiKey are required unless Email:Provider is Log.";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddOptions<WebAppOptions>().BindConfiguration(WebAppOptions.SectionName);
        services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.SectionName);
        services.AddOptions<PlatformOptions>().BindConfiguration(PlatformOptions.SectionName);
        services.AddOptions<FileStorageOptions>().BindConfiguration(FileStorageOptions.SectionName);
        services.AddOptions<RecurringBookingOptions>().BindConfiguration(RecurringBookingOptions.SectionName);

        return services
            .AddPersistence()
            .AddIdentityServices()
            .AddEmail()
            .AddApplicationServices();
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services) =>
        services.AddDbContext<ConCanchaDbContext>((provider, options) =>
        {
            // Read when the context is created (not when the container is built) so tests can override it.
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Missing connection string 'ConnectionStrings:{ConnectionStringName}'.");
            options.UseNpgsql(PostgresConnectionString.Normalize(connectionString));
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

    private static IServiceCollection AddEmail(this IServiceCollection services)
    {
        services.AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName)
            .Validate(options => options.IsComplete, IncompleteEmailSettings)
            .ValidateOnStart();
        services.AddHttpClient(ResendEmailTransport.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(ResendEmailTransport.ApiUrl);
            client.Timeout = TimeSpan.FromSeconds(EmailProviderTimeoutSeconds);
        });
        services.AddSingleton<ResendEmailTransport>();
        services.AddSingleton<LoggingEmailTransport>();
        services.AddSingleton<IEmailTransport>(SelectEmailTransport);
        services.AddSingleton<EmailQueue>();
        services.AddSingleton<IEmailSender, QueuedEmailSender>();
        services.AddHostedService<EmailDispatcher>();
        return services;
    }

    private static IEmailTransport SelectEmailTransport(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<EmailOptions>>().Value.Provider == EmailProvider.Resend
            ? provider.GetRequiredService<ResendEmailTransport>()
            : provider.GetRequiredService<LoggingEmailTransport>();

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<SessionFactory>();
        services.AddScoped<EmailVerifier>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IClubService, ClubService>();
        services.AddScoped<IClubAppearanceService, ClubAppearanceService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IInvitationAcceptor, InvitationAcceptor>();
        services.AddScoped<IMembershipVerifier, MembershipVerifier>();
        services.AddScoped<TeamLinks>();
        services.AddSingleton<IFileStore, LocalDiskFileStore>();
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
        services.AddScoped<RecurringBookingScheduler>();
        services.AddScoped<RecurringBookingReconciler>();
        services.AddScoped<IRecurringBookingService, RecurringBookingService>();
        services.AddSingleton<RecurringBookingRenewal>();
        services.AddHostedService<RecurringBookingRenewalWorker>();
        services.AddScoped<CustomerResolver>();
        services.AddScoped<VisitorBookings>();
        services.AddScoped<IPublicBookingService, PublicBookingService>();
        services.AddSingleton<PlatformCredentials>();
        services.AddSingleton<PlatformLoginThrottle>();
        services.AddScoped<IPlatformSignInService, PlatformSignInService>();
        services.AddScoped<ClubUsageReader>();
        services.AddScoped<ClubBreakdownReader>();
        services.AddScoped<ClubCustomerReader>();
        services.AddScoped<IPlatformUsageService, PlatformUsageService>();
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
