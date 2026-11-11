using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.OpenApiTransformers;

/// <summary>
/// Marks every operation that needs a session with the bearer security requirement; operations that allow anonymous
/// access stay without one.
/// </summary>
internal sealed class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        IList<object> metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any())
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(TokenDefaults.SecuritySchemeName, context.Document)] = [],
                },
            ];
        }

        return Task.CompletedTask;
    }
}
