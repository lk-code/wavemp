using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WaveMP.Service.OpenApiTransformers;

/// <summary>
/// Describes every enum as a named, non-nullable string enum. Enums are serialized as strings; stating that explicitly
/// makes every generator create a string enum. A named enum schema never contains <c>null</c>, also when it is built
/// from a nullable property: otherwise generators add a null or sentinel member to the enum. The property that uses the
/// enum keeps its nullability as <c>oneOf</c> with a <c>null</c> schema.
/// </summary>
internal sealed class StringEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        Type type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (!type.IsEnum
            || schema.Enum is not { Count: > 0 } values
            || (schema.Type is { } schemaType && !schemaType.HasFlag(JsonSchemaType.String)))
        {
            return Task.CompletedTask;
        }

        if (values.Any(IsNull))
        {
            schema.Enum = [.. values.Where(value => !IsNull(value))];
        }

        schema.Type = JsonSchemaType.String;
        return Task.CompletedTask;
    }

    private static bool IsNull(JsonNode? value)
    {
        return value is null || value.GetValueKind() == JsonValueKind.Null;
    }
}
