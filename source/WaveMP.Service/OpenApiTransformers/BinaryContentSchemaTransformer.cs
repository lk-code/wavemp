using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WaveMP.Service.OpenApiTransformers;

/// <summary>
/// Describes binary content (the pictures of the cover endpoints) inline as <c>type: string, format: binary</c>. The
/// framework describes streams with a reference to a component named after <c>IFormFile</c>, a name that means nothing
/// to the clients; the component is removed. Runs on the finished document, after the references are created.
/// </summary>
internal sealed class BinaryContentSchemaTransformer : IOpenApiDocumentTransformer
{
    private const string BinaryFormat = "binary";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        IDictionary<string, IOpenApiSchema>? schemas = document.Components?.Schemas;
        if (schemas is null)
        {
            return Task.CompletedTask;
        }

        HashSet<string> binary = [.. schemas
            .Where(entry => IsBinary(entry.Value))
            .Select(entry => entry.Key)];
        if (binary.Count == 0)
        {
            return Task.CompletedTask;
        }

        IEnumerable<OpenApiOperation> operations = document.Paths.Values
            .SelectMany(path => path.Operations?.Values.AsEnumerable() ?? [])
            .OfType<OpenApiOperation>();
        foreach (OpenApiOperation operation in operations)
        {
            IEnumerable<OpenApiMediaType> contents = (operation.Responses?.Values.AsEnumerable() ?? [])
                .SelectMany(response => response.Content?.Values ?? [])
                .Concat(operation.RequestBody?.Content?.Values ?? [])
                .OfType<OpenApiMediaType>();
            foreach (OpenApiMediaType content in contents)
            {
                if (content.Schema is OpenApiSchemaReference { Reference.Id: { } id } && binary.Contains(id))
                {
                    content.Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = BinaryFormat };
                }
            }
        }

        foreach (string id in binary)
        {
            schemas.Remove(id);
        }

        return Task.CompletedTask;
    }

    private static bool IsBinary(IOpenApiSchema schema)
    {
        return schema is OpenApiSchema
        {
            Type: JsonSchemaType.String,
            Format: BinaryFormat,
            Properties: null or { Count: 0 },
        };
    }
}
