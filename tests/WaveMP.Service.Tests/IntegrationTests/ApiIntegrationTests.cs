using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Tests.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class ApiIntegrationTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private WaveMpTestServer _server = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _server = await WaveMpTestServer.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _server.DisposeAsync();
    }

    [Test]
    public async Task GetHealth_WithDatabase_ReturnsHealthy()
    {
        HttpResponseMessage response = await _server.Client.GetAsync(new Uri("/api/v1/health", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        HealthDto? health = await response.Content.ReadFromJsonAsync<HealthDto>(_jsonOptions);
        health!.Status.Should().Be(HealthStatusDto.Healthy);
        health.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task GetHealth_WithMissingDatabase_ReturnsServiceUnavailableProblemWithCode()
    {
        await using WaveMpTestServer server = await WaveMpTestServer.StartAsync(
            new Dictionary<string, string?> { ["DB_NAME"] = WaveMpTestServer.TestDatabaseName + "_missing" },
            initializeDatabase: false);

        HttpResponseMessage response = await server.Client.GetAsync(new Uri("/api/v1/health", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        JsonNode problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        problem["code"]!.GetValue<string>().Should().Be("health.unhealthy");
        problem["status"]!.GetValue<int>().Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Test]
    public async Task GetUnknownApiRoute_Always_ReturnsNotFoundProblemWithCode()
    {
        HttpResponseMessage response = await _server.Client.GetAsync(
            new Uri("/api/v1/does-not-exist", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        JsonNode problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        problem["code"]!.GetValue<string>().Should().Be("request.not_found");
        problem["status"]!.GetValue<int>().Should().Be(StatusCodes.Status404NotFound);
    }

    [Test]
    public async Task GetOpenApiDocument_Always_HasUniqueOperationIds()
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        List<string?> operationIds = [.. document["paths"]!.AsObject()
            .SelectMany(path => path.Value!.AsObject())
            .Select(operation => operation.Value!["operationId"]?.GetValue<string>())];

        operationIds.Should().NotBeEmpty();
        operationIds.Should().NotContainNulls();
        operationIds.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public async Task GetOpenApiDocument_Always_DescribesProblemCodeAsRequiredNamedEnum()
    {
        string[] expectedCodes = [.. Enum.GetValues<ProblemCodeDto>()
            .Select(code => JsonSerializer.Serialize(code).Trim('"'))];

        JsonNode document = await GetOpenApiDocumentAsync();

        JsonNode schemas = document["components"]!["schemas"]!;
        JsonNode problemDetails = schemas["ProblemDetails"]!;
        problemDetails["required"]!.AsArray().Select(node => node!.GetValue<string>()).Should().Contain("code");
        problemDetails["properties"]!["code"]!["$ref"]!.GetValue<string>()
            .Should().Be("#/components/schemas/ProblemCode");
        JsonNode problemCode = schemas["ProblemCode"]!;
        problemCode["type"]!.GetValue<string>().Should().Be("string");
        problemCode["enum"]!.AsArray().Select(node => node!.GetValue<string>())
            .Should().BeEquivalentTo(expectedCodes);
    }

    [Test]
    public async Task GetOpenApiDocument_Always_UsesUnambiguousTypes()
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        List<JsonObject> schemas = [.. FindSchemas(document["components"]!["schemas"]!)];

        schemas.Should().NotBeEmpty();
        schemas.Should().AllSatisfy(schema =>
        {
            string[] types = GetTypes(schema);
            types.Should().NotContain(
                type => type == "string" && types.Contains("integer"),
                "numbers must not be described as integer/string unions");
            if (schema["enum"] is not null)
            {
                types.Should().Contain("string", "enums are named string enums");
            }
        });
    }

    private async Task<JsonNode> GetOpenApiDocumentAsync()
    {
        HttpResponseMessage response = await _server.Client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    private static IEnumerable<JsonObject> FindSchemas(JsonNode schemas)
    {
        foreach (JsonNode? schema in schemas.AsObject().Select(entry => entry.Value))
        {
            if (schema is not JsonObject schemaObject)
            {
                continue;
            }

            yield return schemaObject;
            if (schemaObject["properties"] is JsonObject properties)
            {
                foreach (JsonObject property in FindSchemas(properties))
                {
                    yield return property;
                }
            }
        }
    }

    private static string[] GetTypes(JsonObject schema)
    {
        return schema["type"] switch
        {
            JsonArray array => [.. array.Select(node => node!.GetValue<string>())],
            JsonValue value => [value.GetValue<string>()],
            _ => [],
        };
    }
}
