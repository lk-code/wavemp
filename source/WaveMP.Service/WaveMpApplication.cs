using Microsoft.Extensions.Options;
using WaveMP.Data;
using WaveMP.Data.Extensions;
using WaveMP.Data.Options;
using WaveMP.Service.Extensions;

namespace WaveMP.Service;

/// <summary>
/// Sets up the WaveMP web application. Used by <c>Program.cs</c> and by the integration tests, so both run exactly
/// the same configuration.
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
        builder.Services.AddControllers();
        builder.Services.AddWaveMpOpenApi();
        builder.Services.AddWaveMpData();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<WaveMpDbContext>("database");

        WebApplication app = builder.Build();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapStaticAssets();
        app.MapOpenApi();
        app.MapControllers();

        // Unknown API routes (404) and unsupported methods on API routes (405) are written as Problem Details by the
        // status code pages; all other routes return the web app.
        app.MapFallbackToFile(WebAppStartPagePattern, WebAppFile);
        app.MapFallbackToFile(WebAppFallbackPattern, WebAppFile);

        return app;
    }

    /// <summary>
    /// Validates the database configuration and applies pending migrations.
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
        return true;
    }
}
