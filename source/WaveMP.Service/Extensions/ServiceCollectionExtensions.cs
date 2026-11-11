using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using WaveMP.Service.Dtos;
using WaveMP.Service.Problems;

namespace WaveMP.Service.Extensions;

public static class ServiceCollectionExtensions
{
    private const string DtoSuffix = "Dto";

    // Schemas of all Problem Details types the framework may describe.
    private static readonly string[] _problemSchemaIds =
    [
        nameof(ProblemDetails),
        nameof(ValidationProblemDetails),
        nameof(HttpValidationProblemDetails),
    ];

    /// <summary>
    /// Configures JSON for minimal APIs and controllers alike: numbers are only read as JSON numbers (so the OpenAPI
    /// document describes them as plain integers) and enums are always written as strings.
    /// </summary>
    public static IServiceCollection AddWaveMpJson(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options => ConfigureSerializer(options.SerializerOptions));
        services.Configure<JsonOptions>(options => ConfigureSerializer(options.JsonSerializerOptions));
        return services;
    }

    /// <summary>
    /// Registers Problem Details (RFC 9457). Every Problem Details response carries a machine-readable
    /// <see cref="ProblemCodeDto"/> in its member <c>code</c>, by default derived from the status code.
    /// </summary>
    public static IServiceCollection AddWaveMpProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            IDictionary<string, object?> extensions = context.ProblemDetails.Extensions;
            if (!extensions.ContainsKey(ProblemCodes.ExtensionName))
            {
                int statusCode = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
                extensions[ProblemCodes.ExtensionName] = ProblemCodes.ForStatusCode(statusCode);
            }
        });

        return services;
    }

    /// <summary>
    /// Registers the OpenAPI document <c>v1</c>, served at <c>/openapi/v1.json</c>.
    /// Keep XML summaries of API types on one line: the generator joins lines with the platform's line break.
    /// </summary>
    public static IServiceCollection AddWaveMpOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            // Clients see "Health" instead of "HealthDto".
            options.CreateSchemaReferenceId = typeInfo => RemoveDtoSuffix(
                OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo));

            options.AddSchemaTransformer(DescribeEnumsAsStrings);
            options.AddDocumentTransformer(AddProblemCodeAsync);

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

    private static void ConfigureSerializer(JsonSerializerOptions options)
    {
        options.NumberHandling = JsonNumberHandling.Strict;
        options.Converters.Add(new JsonStringEnumConverter());
    }

    private static string? RemoveDtoSuffix(string? id)
    {
        return id is not null && id.EndsWith(DtoSuffix, StringComparison.Ordinal) && id.Length > DtoSuffix.Length
            ? id[..^DtoSuffix.Length]
            : id;
    }

    /// <summary>
    /// Enums are serialized as strings; states that explicitly so every generator creates a string enum.
    /// </summary>
    private static Task DescribeEnumsAsStrings(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        Type type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (!type.IsEnum || schema.Enum is not { Count: > 0 } values || schema.Type is not null)
        {
            return Task.CompletedTask;
        }

        schema.Type = values.Any(value => value is null)
            ? JsonSchemaType.String | JsonSchemaType.Null
            : JsonSchemaType.String;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds the required member <c>code</c> to the Problem Details schemas, referencing the named enum schema of
    /// <see cref="ProblemCodeDto"/>. Runs on the finished document, so the reference is kept as <c>$ref</c>.
    /// </summary>
    private static async Task AddProblemCodeAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        IDictionary<string, IOpenApiSchema>? schemas = document.Components?.Schemas;
        if (schemas is null)
        {
            return;
        }

        OpenApiSchema[] problemSchemas = [.. _problemSchemaIds
            .Select(id => schemas.TryGetValue(id, out IOpenApiSchema? schema) ? schema : null)
            .OfType<OpenApiSchema>()];
        if (problemSchemas.Length == 0)
        {
            return;
        }

        string codeSchemaId = RemoveDtoSuffix(nameof(ProblemCodeDto))!;
        if (!schemas.ContainsKey(codeSchemaId))
        {
            OpenApiSchema codeSchema = await context.GetOrCreateSchemaAsync(
                typeof(ProblemCodeDto),
                cancellationToken: cancellationToken);
            document.AddComponent(codeSchemaId, codeSchema);
        }

        foreach (OpenApiSchema schema in problemSchemas)
        {
            schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
            schema.Properties[ProblemCodes.ExtensionName] = new OpenApiSchemaReference(codeSchemaId, document);
            schema.Required ??= new HashSet<string>();
            schema.Required.Add(ProblemCodes.ExtensionName);
        }
    }
}
