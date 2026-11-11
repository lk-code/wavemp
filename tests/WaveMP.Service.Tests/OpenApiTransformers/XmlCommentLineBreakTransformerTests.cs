using AwesomeAssertions;
using Microsoft.OpenApi;
using WaveMP.Service.OpenApiTransformers;

namespace WaveMP.Service.Tests.OpenApiTransformers;

[TestFixture]
public class XmlCommentLineBreakTransformerTests
{
    [TestCase("first line\r\nsecond line", "first line second line")]
    [TestCase("first line\nsecond line", "first line second line")]
    [TestCase("first line\rsecond line", "first line second line")]
    [TestCase("first line  \n   second line\nthird line", "first line second line third line")]
    public void JoinLines_WithWrappedLines_JoinsThemWithASpace(string text, string expected)
    {
        string result = XmlCommentLineBreakTransformer.JoinLines(text);

        result.Should().Be(expected);
    }

    [TestCase("first paragraph\r\n\r\nsecond paragraph", "first paragraph\n\nsecond paragraph")]
    [TestCase("first paragraph\n \n\n second paragraph", "first paragraph\n\nsecond paragraph")]
    public void JoinLines_WithEmptyLines_KeepsOneParagraphBreak(string text, string expected)
    {
        string result = XmlCommentLineBreakTransformer.JoinLines(text);

        result.Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("One line with `code`.")]
    public void JoinLines_WithoutLineBreaks_ReturnsTheText(string? text)
    {
        string? result = XmlCommentLineBreakTransformer.JoinLines(text);

        result.Should().Be(text);
    }

    [Test]
    public async Task TransformAsync_OperationWithWrappedTexts_JoinsTheLinesOfAllTexts()
    {
        OpenApiOperation operation = new()
        {
            Summary = "Lists the\r\nruns.",
            Description = "Removes old\r\nruns.",
            Parameters = [new OpenApiParameter { Name = "limit", Description = "Maximum\r\nnumber." }],
            RequestBody = new OpenApiRequestBody { Description = "The\r\nchanges." },
            Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "One\r\npage." } },
        };

        await new XmlCommentLineBreakTransformer().TransformAsync(operation, null!, CancellationToken.None);

        operation.Summary.Should().Be("Lists the runs.");
        operation.Description.Should().Be("Removes old runs.");
        operation.Parameters[0].Description.Should().Be("Maximum number.");
        operation.RequestBody.Description.Should().Be("The changes.");
        operation.Responses["200"].Description.Should().Be("One page.");
    }

    [Test]
    public async Task TransformAsync_SchemaWithWrappedTexts_JoinsTheLinesOfDescriptionAndReferenceDescription()
    {
        OpenApiSchema schema = new()
        {
            Description = "A run\r\nof a job.",
            Metadata = new Dictionary<string, object> { ["x-ref-description"] = "State of\r\nthe run." },
        };

        await new XmlCommentLineBreakTransformer().TransformAsync(schema, null!, CancellationToken.None);

        schema.Description.Should().Be("A run of a job.");
        schema.Metadata["x-ref-description"].Should().Be("State of the run.");
    }
}
