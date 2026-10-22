using ConCancha.Web.Components;
using ConCancha.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services
    .AddSessionServices()
    .AddApiClient()
    .AddProxyForwardedHeaders();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapClubLogo();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
