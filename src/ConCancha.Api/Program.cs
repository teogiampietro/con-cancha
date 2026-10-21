using ConCancha.Api.Extensions;
using ConCancha.Api.Tenancy;
using ConCancha.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure()
    .AddJwtAuthentication()
    .AddIpRateLimiting()
    .AddProxyForwardedHeaders()
    .AddSwaggerWithBearer()
    .AddDatabaseHealthCheck();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

var app = builder.Build();

await app.MigrateDatabaseIfEnabledAsync();

app.UseForwardedHeaders();
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
