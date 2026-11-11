using Microsoft.Extensions.Options;
using WaveMP.Core.Users.Extensions;
using WaveMP.Data;
using WaveMP.Data.Extensions;
using WaveMP.Data.Options;
using WaveMP.Service.Extensions;
using WaveMP.Service.Tokens;

namespace WaveMP.Service;

/// <summary>
/// Sets up the WaveMP web application for <c>Program.cs</c>. The integration tests start <c>Program.cs</c> itself
/// through <c>WebApplicationFactory</c>, so they run exactly the same configuration and startup.
/// </summary>
public static class WaveMpApplication
{
    // The web app routes on the client, so it answers the start page and every path whose first segment is not "api",
    // except paths that look like files (e.g. "/missing.js"). The first segment is a plain parameter on purpose: unlike
    // a catch-all, routing then leaves the fallback out of the "api" branch completely, so API routes keep the
    // framework's 404 and 405 (with an exact Allow header) responses. For the same reason, appsettings.json sets
    // "DisableStaticAssetNotFoundRuntimeFallback": it removes the catch-all that MapStaticAssets adds when running from
    // the build output (dotnet run, integration tests); the published image never has it.
    private const string WebAppStartPagePattern = "/";
    private const string WebAppFallbackPattern = "{segment:regex(^(?!api$)):nonfile}/{*path:nonfile}";
    private const string WebAppFile = "index.html";

    /// <summary>
    /// Registers all services, builds the application and configures the request pipeline.
    /// </summary>
    public static WebApplication Build(WebApplicationBuilder builder)
    {
        builder.Services.AddWaveMpJson();
        builder.Services.AddWaveMpProblemDetails();
        builder.Services.AddWaveMpControllers();
        builder.Services.AddWaveMpOpenApi();
        builder.Services.AddWaveMpData();
        builder.Services.AddWaveMpUsers();
        builder.Services.AddWaveMpAuthentication();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<WaveMpDbContext>("database");

        WebApplication app = builder.Build();

        app.UseExceptionHandler();
        // Before the authentication, so that its empty 401 and 403 responses become Problem Details as well.
        app.UseStatusCodePages();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapOpenApi();
        // Every action declares [Authorize] or [AllowAnonymous] itself; this is the safety net for a forgotten one.
        // No fallback policy: it would also apply to unknown API routes (404), 405 responses and the web app.
        app.MapControllers().RequireAuthorization();

        // Unknown API routes (404) and unsupported methods on API routes (405) are written as Problem Details by the
        // status code pages; all other routes return the web app.
        app.MapFallbackToFile(WebAppStartPagePattern, WebAppFile);
        app.MapFallbackToFile(WebAppFallbackPattern, WebAppFile);

        return app;
    }

    /// <summary>
    /// Validates the database configuration, applies pending migrations, creates the built-in admin profile on the
    /// first start and loads the key that signs the session tokens.
    /// Returns <c>false</c> if the configuration is invalid; the reason is logged.
    /// </summary>
    public static async Task<bool> InitializeAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        }
        catch (OptionsValidationException exception)
        {
            app.Logger.LogCritical("Invalid database configuration: {Errors}", string.Join(" ", exception.Failures));
            return false;
        }

        await app.Services.MigrateWaveMpDatabaseAsync(cancellationToken);
        await app.Services.InitializeWaveMpUsersAsync(cancellationToken);
        await app.Services.GetRequiredService<SigningKeyStore>().InitializeAsync(cancellationToken);
        return true;
    }
}
