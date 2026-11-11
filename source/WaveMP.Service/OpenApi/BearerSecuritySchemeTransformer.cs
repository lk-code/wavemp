using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using WaveMP.Service.Authentication;

namespace WaveMP.Service.OpenApi;

/// <summary>
/// Describes the session token as HTTP bearer security scheme of the OpenAPI document.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[TokenDefaults.SecuritySchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token of a session, created with POST /api/v1/sessions.",
        };

        return Task.CompletedTask;
    }
}
