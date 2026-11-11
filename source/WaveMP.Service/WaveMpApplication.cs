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

        // Unknown API routes return Problem Details instead of the web app.
        app.MapFallback(
                "/api/{**path}",
                () => Results.Problem(statusCode: StatusCodes.Status404NotFound))
            .ExcludeFromDescription();
        app.MapFallbackToFile("index.html");

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
