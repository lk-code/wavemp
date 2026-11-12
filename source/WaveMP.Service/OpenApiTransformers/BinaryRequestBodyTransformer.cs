using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WaveMP.Service.OpenApiTransformers;

/// <summary>
/// Describes the request body of actions with <see cref="BinaryRequestBodyAttribute"/> as required binary content
/// (<c>type: string, format: binary</c>) of each accepted content type.
/// </summary>
internal sealed class BinaryRequestBodyTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        BinaryRequestBodyAttribute? attribute = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<BinaryRequestBodyAttribute>()
            .FirstOrDefault();
        if (attribute is null)
        {
            return Task.CompletedTask;
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = attribute.ContentTypes.ToDictionary(
                contentType => contentType,
                _ => new OpenApiMediaType
                {
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" },
                }),
        };
        return Task.CompletedTask;
    }
}
