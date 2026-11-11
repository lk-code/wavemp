using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WaveMP.Service.OpenApiTransformers;

/// <summary>
/// Joins the wrapped lines of XML comments in the texts of operations and schemas with a space; an empty line between
/// paragraphs stays as a paragraph break. Lines in XML comments are wrapped for the source code only, and the XML
/// comment generator keeps them with the line break of the build platform, so the document would otherwise differ
/// between Windows and Linux and generated clients would glue the words at the line break together. Runs after the
/// XML comment transformers, which the generator registers before the options of the document.
/// </summary>
internal sealed partial class XmlCommentLineBreakTransformer : IOpenApiOperationTransformer, IOpenApiSchemaTransformer
{
    /// <summary>Metadata key in which the XML comment generator keeps the description of a schema reference.</summary>
    private const string ReferenceDescriptionKey = "x-ref-description";

    private const string ParagraphBreak = "\n\n";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        operation.Summary = JoinLines(operation.Summary);
        operation.Description = JoinLines(operation.Description);
        foreach (OpenApiParameter parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            parameter.Description = JoinLines(parameter.Description);
        }

        if (operation.RequestBody is OpenApiRequestBody requestBody)
        {
            requestBody.Description = JoinLines(requestBody.Description);
        }

        foreach (OpenApiResponse response in operation.Responses?.Values.OfType<OpenApiResponse>() ?? [])
        {
            response.Description = JoinLines(response.Description);
        }

        return Task.CompletedTask;
    }

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        schema.Description = JoinLines(schema.Description);
        if (schema.Metadata is { } metadata
            && metadata.TryGetValue(ReferenceDescriptionKey, out object? value)
            && value is string description)
        {
            metadata[ReferenceDescriptionKey] = JoinLines(description);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Replaces every line break and the white space around it with a space, and every run of empty lines with one
    /// paragraph break (<c>\n\n</c>). Text without line breaks stays unchanged.
    /// </summary>
    [return: NotNullIfNotNull(nameof(text))]
    internal static string? JoinLines(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return LineBreaks().Replace(
            text.ReplaceLineEndings("\n"),
            match => match.ValueSpan.Count('\n') > 1 ? ParagraphBreak : " ");
    }

    [GeneratedRegex(@"\s*\n\s*", RegexOptions.CultureInvariant)]
    private static partial Regex LineBreaks();
}
