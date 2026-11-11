using System.Reflection;
using WaveMP.Service;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = WaveMpApplication.Build(builder);

// The build-time OpenAPI generator starts this program as well; it must not need a database.
bool isOpenApiGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
if (!isOpenApiGeneration && !await WaveMpApplication.InitializeAsync(app))
{
    return 1;
}

await app.RunAsync();
return 0;
