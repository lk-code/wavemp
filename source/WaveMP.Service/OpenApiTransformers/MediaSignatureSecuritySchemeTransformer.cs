using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using WaveMP.Service.Signatures;

namespace WaveMP.Service.OpenApiTransformers;

/// <summary>
/// Describes the signed media URLs as API key security scheme in the query of the OpenAPI document.
/// </summary>
internal sealed class MediaSignatureSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[MediaSignatureDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Query,
            Name = MediaSignatureDefaults.SignatureParameter,
            Description = "Signed media URL for players that cannot send a token header. Use the URL as issued "
                + "(for example streamUrl of GetTrackPlayback): its query carries uid, sid, exp and sig. It is only "
                + "valid for one kind of media of one track, until it expires and while the session is active.",
        };

        return Task.CompletedTask;
    }
}
