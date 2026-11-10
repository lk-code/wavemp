using System.Reflection;
using Microsoft.Extensions.Options;
using WaveMP.Data;
using WaveMP.Data.Extensions;
using WaveMP.Data.Options;
using WaveMP.Service.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddWaveMpOpenApi();
builder.Services.AddWaveMpData();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WaveMpDbContext>("database");

WebApplication app = builder.Build();

// The build-time OpenAPI generator starts this program as well; it must not need a database.
bool isOpenApiGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
if (!isOpenApiGeneration)
{
    try
    {
        _ = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    }
    catch (OptionsValidationException exception)
    {
        app.Logger.LogCritical("Invalid database configuration: {Errors}", string.Join(" ", exception.Failures));
        return 1;
    }

    await app.Services.MigrateWaveMpDatabaseAsync();
}

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

await app.RunAsync();
return 0;
