using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Tests.Integration;

[TestFixture]
[Category("Integration")]
public class ApiIntegrationTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WaveMpWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetHealth_WithDatabase_ReturnsHealthy()
    {
        HttpResponseMessage response = await _client.GetAsync(new Uri("/api/v1/health", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        HealthDto? health = await response.Content.ReadFromJsonAsync<HealthDto>(_jsonOptions);
        health!.Status.Should().Be(HealthStatusDto.Healthy);
        health.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task GetOpenApiDocument_Always_HasUniqueOperationIds()
    {
        HttpResponseMessage response = await _client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        List<string?> operationIds = [.. document!["paths"]!.AsObject()
            .SelectMany(path => path.Value!.AsObject())
            .Select(operation => operation.Value!["operationId"]?.GetValue<string>())];

        operationIds.Should().NotBeEmpty();
        operationIds.Should().NotContainNulls();
        operationIds.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public async Task GetUnknownApiRoute_Always_ReturnsNotFoundProblemDetails()
    {
        HttpResponseMessage response = await _client.GetAsync(new Uri("/api/v1/does-not-exist", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }
}
