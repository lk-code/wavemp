using Microsoft.AspNetCore.OpenApi;

namespace WaveMP.Service.Extensions;

public static class ServiceCollectionExtensions
{
    private const string DtoSuffix = "Dto";

    /// <summary>
    /// Registers the OpenAPI document <c>v1</c>, served at <c>/openapi/v1.json</c>.
    /// </summary>
    public static IServiceCollection AddWaveMpOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            // Clients see "Health" instead of "HealthDto".
            options.CreateSchemaReferenceId = typeInfo =>
            {
                string? id = OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
                return id is not null && id.EndsWith(DtoSuffix, StringComparison.Ordinal) && id.Length > DtoSuffix.Length
                    ? id[..^DtoSuffix.Length]
                    : id;
            };

            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "WaveMP API";
                document.Info.Version = "v1";
                document.Info.Description = "REST API of WaveMP, a nostalgic media player.";
                return Task.CompletedTask;
            });
        });

        return services;
    }
}
