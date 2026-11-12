namespace WaveMP.Service.OpenApiTransformers;

/// <summary>
/// Marks an action that reads its request body itself as raw bytes, e.g. an uploaded picture, and names the content
/// types it accepts. Model binding knows no such body, so <see cref="BinaryRequestBodyTransformer"/> describes it in
/// the OpenAPI document.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BinaryRequestBodyAttribute(params string[] contentTypes) : Attribute
{
    public IReadOnlyList<string> ContentTypes { get; } = contentTypes;
}
