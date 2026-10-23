using ConCancha.Api.Commands;
using ConCancha.Api.ErrorHandling;
using ConCancha.Api.Extensions;
using ConCancha.Api.Subscriptions;
using ConCancha.Api.Tenancy;
using ConCancha.Infrastructure;

if (HashPasswordCommand.IsRequested(args))
{
    HashPasswordCommand.Run();
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure()
    .AddJwtAuthentication()
    .AddIpRateLimiting()
    .AddProxyForwardedHeaders()
    .AddSwaggerWithBearer()
    .AddDatabaseHealthCheck();
builder.Services.AddControllers(options => options.Filters.Add<SubscriptionAccessFilter>());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();

var app = builder.Build();

await app.MigrateDatabaseIfEnabledAsync();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<ClubTenancyMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthEndpoints();

app.Run();

public partial class Program;
