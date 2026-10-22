using ConCancha.Web.Services;

namespace ConCancha.Web.Extensions;

public static class ClubLogoEndpoint
{
    private const string CacheControlValue = "public, max-age=86400";

    /// <summary>Serves a club's logo from this host so the browser never needs the API's address.</summary>
    public static WebApplication MapClubLogo(this WebApplication app)
    {
        app.MapGet(PageRoutes.ClubLogoPattern, async (string slug, ApiClient api, HttpContext context, CancellationToken cancellationToken) =>
        {
            var logo = await api.GetPublicLogoAsync(slug, cancellationToken);
            if (logo is null) return Results.NotFound();

            context.Response.Headers.CacheControl = CacheControlValue;
            return Results.File(logo.Content, logo.ContentType);
        });
        return app;
    }
}
