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

    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WaveMpWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        // Also runs if the service did not start; the client is then missing.
        _client?.Dispose();
        await _factory.DisposeAsync();
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
    public async Task GetHealth_WithMissingDatabase_ReturnsServiceUnavailableProblemWithCode()
    {
        // The startup creates and migrates the database like in production, so the test drops it afterwards.
        await using WaveMpWebApplicationFactory factory =
            new(WaveMpWebApplicationFactory.TestDatabaseName + "_missing");
        using HttpClient client = factory.CreateClient();
        await factory.DeleteDatabaseAsync();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/health", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        JsonNode problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        problem["code"]!.GetValue<string>().Should().Be("health.unhealthy");
        problem["status"]!.GetValue<int>().Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [TestCase("/api")]
    [TestCase("/api/v1/does-not-exist")]
    [TestCase("/API/V1/DOES-NOT-EXIST")]
    public async Task GetUnknownApiRoute_Always_ReturnsNotFoundProblemWithCode(string path)
    {
        HttpResponseMessage response = await _client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        JsonNode problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        problem["code"]!.GetValue<string>().Should().Be("request.not_found");
        problem["status"]!.GetValue<int>().Should().Be(StatusCodes.Status404NotFound);
    }

    [TestCase("POST")]
    [TestCase("PUT")]
    [TestCase("PATCH")]
    [TestCase("DELETE")]
    public async Task SendToHealth_WithUnsupportedMethod_ReturnsMethodNotAllowedProblemWithCode(string method)
    {
        using HttpRequestMessage request = new(new HttpMethod(method), new Uri("/api/v1/health", UriKind.Relative));

        HttpResponseMessage response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.Allow.Should().Equal(HttpMethods.Get);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        JsonNode problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        problem["code"]!.GetValue<string>().Should().Be("request.failed");
        problem["status"]!.GetValue<int>().Should().Be(StatusCodes.Status405MethodNotAllowed);
    }

    [Test]
    public async Task HeadHealth_Always_ReturnsMethodNotAllowedWithAllowHeader()
    {
        using HttpRequestMessage request = new(HttpMethod.Head, new Uri("/api/v1/health", UriKind.Relative));

        HttpResponseMessage response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.Allow.Should().Equal(HttpMethods.Get);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
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
    public async Task GetOpenApiDocument_Always_DescribesNamedEnumsWithoutNull()
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        List<KeyValuePair<string, JsonNode?>> enums = [.. document["components"]!["schemas"]!.AsObject()
            .Where(entry => entry.Value?["enum"] is not null)];

        enums.Should().Contain(entry => entry.Key == "JobErrorCode");
        enums.Should().AllSatisfy(entry =>
        {
            entry.Value!["enum"]!.AsArray().Should().NotContainNulls($"enum {entry.Key} must not have a null member");
            GetTypes(entry.Value.AsObject()).Should().Equal(["string"], $"enum {entry.Key} must not be nullable");
        });
    }

    [TestCase("JobRun")]
    [TestCase("Scan")]
    public async Task GetOpenApiDocument_Always_KeepsNullableEnumPropertiesNullable(string schema)
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        JsonArray oneOf = document["components"]!["schemas"]![schema]!["properties"]!["errorCode"]!["oneOf"]!.AsArray();

        oneOf.Should().HaveCount(2);
        oneOf.Should().ContainSingle(node => node!["type"] != null && node["type"]!.GetValue<string>() == "null");
        oneOf.Should().ContainSingle(node => node!["$ref"] != null
            && node["$ref"]!.GetValue<string>() == "#/components/schemas/JobErrorCode");
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

    [Test]
    public async Task GetOpenApiDocument_Always_DescribesTheBearerSecurityScheme()
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        JsonNode scheme = document["components"]!["securitySchemes"]!["Bearer"]!;
        scheme["type"]!.GetValue<string>().Should().Be("http");
        scheme["scheme"]!.GetValue<string>().Should().Be("bearer");
        scheme["bearerFormat"]!.GetValue<string>().Should().Be("JWT");
    }

    [TestCase("ListUsers", true)]
    [TestCase("CreateUser", true)]
    [TestCase("GetCurrentSession", true)]
    [TestCase("DeleteCurrentSession", true)]
    [TestCase("GetMySettings", true)]
    [TestCase("UpdateMySettings", true)]
    [TestCase("StartScan", true)]
    [TestCase("GetCurrentScan", true)]
    [TestCase("ListJobs", true)]
    [TestCase("StartJobRun", true)]
    [TestCase("UpdateLibrarySettings", true)]
    [TestCase("GetCacheSettings", true)]
    [TestCase("UpdateCacheSettings", true)]
    [TestCase("GetCache", true)]
    [TestCase("ClearCache", true)]
    [TestCase("ListTracks", true)]
    [TestCase("SearchLibrary", true)]
    [TestCase("GetAlbumCover", true)]
    [TestCase("ListTrackFiles", true)]
    [TestCase("GetTrackPlayback", true)]
    [TestCase("CreateMyHistoryEntry", true)]
    [TestCase("ListMyHistory", true)]
    [TestCase("SetMyPlaylistCover", true)]
    [TestCase("ListMyFavorites", true)]
    [TestCase("AddMyFavoriteTrack", true)]
    [TestCase("SetMyTrackRating", true)]
    [TestCase("GetMyHome", true)]
    [TestCase("GetTrackVideoOffset", true)]
    [TestCase("SetTrackVideoOffset", true)]
    [TestCase("ListProfiles", false)]
    [TestCase("CreateSession", false)]
    [TestCase("GetHealth", false)]
    public async Task GetOpenApiDocument_Always_MarksOperationsThatNeedASession(string operationId, bool secured)
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        JsonNode operation = FindOperations(document)
            .Single(node => node["operationId"]!.GetValue<string>() == operationId);

        if (secured)
        {
            operation["security"]!.AsArray().Should().ContainSingle()
                .Which!["Bearer"].Should().NotBeNull();
        }
        else
        {
            operation["security"].Should().BeNull();
        }
    }

    [TestCase("Users", 5)]
    [TestCase("Scans", 4)]
    [TestCase("Jobs", 4)]
    [TestCase("Settings", 6)]
    [TestCase("Cache", 2)]
    [TestCase("Transcoding", 3)]
    public async Task GetOpenApiDocument_Always_DescribesUnauthorizedAndForbiddenResponses(string tag, int operations)
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        List<JsonNode> secured = [.. FindOperations(document).Where(operation => operation["security"] is not null)];

        secured.Should().NotBeEmpty();
        secured.Should().AllSatisfy(operation => operation["responses"]!["401"].Should().NotBeNull());
        FindOperations(document)
            .Where(operation => operation["tags"]![0]!.GetValue<string>() == tag)
            .Should().HaveCount(operations)
            .And.AllSatisfy(operation => operation["responses"]!["403"].Should().NotBeNull());
    }

    [TestCase("ProfilePage", "Profile")]
    [TestCase("UserPage", "User")]
    [TestCase("ScanPage", "Scan")]
    [TestCase("JobPage", "Job")]
    [TestCase("JobRunPage", "JobRun")]
    [TestCase("ArtistPage", "Artist")]
    [TestCase("AlbumPage", "Album")]
    [TestCase("TrackPage", "Track")]
    [TestCase("GenrePage", "Genre")]
    [TestCase("YearPage", "Year")]
    [TestCase("MediaFilePage", "MediaFile")]
    [TestCase("PlayHistoryEntryPage", "PlayHistoryEntry")]
    [TestCase("FavoritePage", "Favorite")]
    [TestCase("TranscodeJobPage", "TranscodeJob")]
    public async Task GetOpenApiDocument_Always_NamesPagesAfterTheirItems(string pageSchema, string itemSchema)
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        JsonNode page = document["components"]!["schemas"]![pageSchema]!;
        page["required"]!.AsArray().Select(node => node!.GetValue<string>())
            .Should().BeEquivalentTo(["items", "offset", "limit", "total"]);
        page["properties"]!["items"]!["items"]!["$ref"]!.GetValue<string>()
            .Should().Be($"#/components/schemas/{itemSchema}");
    }

    [Test]
    public async Task GetOpenApiDocument_Always_DescribesSignedMediaUrlsAsAlternativeToTheSession()
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        JsonNode scheme = document["components"]!["securitySchemes"]!["MediaSignature"]!;
        JsonNode operation = FindOperations(document)
            .Single(node => node["operationId"]!.GetValue<string>() == "StreamTrack");

        scheme["type"]!.GetValue<string>().Should().Be("apiKey");
        scheme["in"]!.GetValue<string>().Should().Be("query");
        scheme["name"]!.GetValue<string>().Should().Be("sig");
        operation["security"]!.AsArray().Select(requirement => requirement!.AsObject().Single().Key)
            .Should().Equal("Bearer", "MediaSignature");
        operation["parameters"]!.AsArray()
            .Where(parameter => parameter!["in"]!.GetValue<string>() == "query")
            .Select(parameter => parameter!["name"]!.GetValue<string>())
            .Should().Equal("audioFormats", "uid", "sid", "exp", "sig");
        operation["responses"]!["401"].Should().NotBeNull();
    }

    [Test]
    public async Task GetOpenApiDocument_Always_DescribesTheStreamAsBinaryAudioWithRanges()
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        JsonNode responses = FindOperations(document)
            .Single(operation => operation["operationId"]!.GetValue<string>() == "StreamTrack")["responses"]!;

        foreach (string status in new[] { "200", "206" })
        {
            JsonObject content = responses[status]!["content"]!.AsObject();
            content.Select(entry => entry.Key).Should().Contain(["audio/mpeg", "audio/flac", "audio/ogg"]);
            content.Should().AllSatisfy(entry =>
                entry.Value!["schema"]!["format"]!.GetValue<string>().Should().Be("binary"));
        }

        responses["406"]!["content"]!["application/problem+json"].Should().NotBeNull();
        responses["416"].Should().NotBeNull();
    }

    [TestCase("GetAlbumCover")]
    [TestCase("GetTrackCover")]
    public async Task GetOpenApiDocument_Always_DescribesCoversAsBinaryImages(string operationId)
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        JsonNode responses = FindOperations(document)
            .Single(operation => operation["operationId"]!.GetValue<string>() == operationId)["responses"]!;

        JsonObject content = responses["200"]!["content"]!.AsObject();
        content.Select(entry => entry.Key).Should().BeEquivalentTo(
            ["image/jpeg", "image/png", "image/webp", "image/gif", "image/bmp"]);
        content.Should().AllSatisfy(entry =>
        {
            entry.Value!["schema"]!["type"]!.GetValue<string>().Should().Be("string");
            entry.Value["schema"]!["format"]!.GetValue<string>().Should().Be("binary");
        });
        responses["404"]!["content"]!["application/problem+json"].Should().NotBeNull();
        document["components"]!["schemas"]!.AsObject().Should().NotContainKey("IFormFile");
    }

    [Test]
    public async Task GetOpenApiDocument_Always_DescribesTheUploadOfPlaylistCoversAsBinaryImages()
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        JsonNode upload = FindOperations(document)
            .Single(operation => operation["operationId"]!.GetValue<string>() == "SetMyPlaylistCover");
        JsonNode cover = FindOperations(document)
            .Single(operation => operation["operationId"]!.GetValue<string>() == "GetMyPlaylistCover");

        upload["requestBody"]!["required"]!.GetValue<bool>().Should().BeTrue();
        JsonObject content = upload["requestBody"]!["content"]!.AsObject();
        content.Select(entry => entry.Key).Should().BeEquivalentTo(["image/jpeg", "image/png", "image/webp"]);
        content.Should().AllSatisfy(entry =>
        {
            entry.Value!["schema"]!["type"]!.GetValue<string>().Should().Be("string");
            entry.Value["schema"]!["format"]!.GetValue<string>().Should().Be("binary");
        });
        upload["responses"]!.AsObject().Select(entry => entry.Key).Should().Contain(["413", "415", "422", "503"]);
        cover["responses"]!["200"]!["content"]!.AsObject().Select(entry => entry.Key)
            .Should().Equal("image/webp");
    }

    [TestCase("GetAlbumCover")]
    [TestCase("GetTrackCover")]
    [TestCase("GetMyPlaylistCover")]
    public async Task GetOpenApiDocument_Always_DescribesTheSizeAndHttpCachingOfCovers(string operationId)
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        JsonNode operation = FindOperations(document)
            .Single(node => node["operationId"]!.GetValue<string>() == operationId);

        JsonNode size = operation["parameters"]!.AsArray()
            .Single(parameter => parameter!["name"]!.GetValue<string>() == "size")!;
        size["in"]!.GetValue<string>().Should().Be("query");
        size["required"]?.GetValue<bool>().Should().NotBe(true);
        size["schema"]!["minimum"]!.GetValue<int>().Should().Be(1);
        size["schema"]!["maximum"]!.GetValue<int>().Should().Be(4096);
        operation["responses"]!["400"]!["content"]!["application/problem+json"].Should().NotBeNull();

        // 304 is part of the description only: Kiota would generate no body for an operation that lists it.
        operation["summary"]!.GetValue<string>().Should().Contain("If-None-Match returns 304");
        operation["responses"]!["304"].Should().BeNull();
    }

    [Test]
    public async Task GetOpenApiDocument_Always_DescribesForbiddenOnlyForTheAdminEndpointsOfTheLibrary()
    {
        string[] libraryTags = ["Artists", "Albums", "Tracks", "Genres", "Years", "Search"];

        JsonNode document = await GetOpenApiDocumentAsync();

        List<JsonNode> library = [.. FindOperations(document)
            .Where(operation => libraryTags.Contains(operation["tags"]![0]!.GetValue<string>()))];
        library.Should().HaveCount(17);
        library.Should().AllSatisfy(operation => operation["responses"]!["401"].Should().NotBeNull());
        library.Where(operation => operation["responses"]!["403"] is not null)
            .Select(operation => operation["operationId"]!.GetValue<string>())
            .Should().Equal("ListTrackFiles", "GetTrackMetadata", "UpdateTrackMetadata");
    }

    [Test]
    public async Task GetOpenApiDocument_Always_DescribesForbiddenOnlyForTheVideoOffset()
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        List<JsonNode> videos = [.. FindOperations(document)
            .Where(operation => operation["tags"]![0]!.GetValue<string>() == "TrackVideos")];
        videos.Should().AllSatisfy(operation => operation["responses"]!["401"].Should().NotBeNull());
        videos.Where(operation => operation["responses"]!["403"] is not null)
            .Select(operation => operation["operationId"]!.GetValue<string>())
            .Should().BeEquivalentTo("GetTrackVideoOffset", "SetTrackVideoOffset");
    }

    [Test]
    public async Task GetOpenApiDocument_Always_DescribesThePagingParameters()
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        JsonNode listProfiles = FindOperations(document)
            .Single(operation => operation["operationId"]!.GetValue<string>() == "ListProfiles");
        JsonArray parameters = listProfiles["parameters"]!.AsArray();

        JsonNode offset = parameters.Single(parameter => parameter!["name"]!.GetValue<string>() == "offset")!;
        JsonNode limit = parameters.Single(parameter => parameter!["name"]!.GetValue<string>() == "limit")!;
        offset["in"]!.GetValue<string>().Should().Be("query");
        offset["schema"]!["minimum"]!.GetValue<int>().Should().Be(0);
        limit["schema"]!["minimum"]!.GetValue<int>().Should().Be(1);
        limit["schema"]!["maximum"]!.GetValue<int>().Should().Be(100);
    }

    [Test]
    public async Task GetOpenApiDocument_Always_JoinsWrappedXmlCommentLines()
    {
        JsonNode document = await GetOpenApiDocumentAsync();

        // The summary is wrapped in the XML comment; the document must not depend on the line break of the platform.
        FindOperations(document)
            .Single(operation => operation["operationId"]!.GetValue<string>() == "ListJobRuns")["summary"]!
            .GetValue<string>().Should().Be("Lists the runs of the job by the time they were requested, newest first "
                + "by default, optionally filtered. Runs older than 30 days and beyond the 100 newest are removed "
                + "automatically.");
        FindStrings(document).Should().NotContain(text => text.Contains('\r'));
    }

    private static IEnumerable<string> FindStrings(JsonNode? node)
    {
        return node switch
        {
            JsonObject jsonObject => jsonObject.SelectMany(entry => FindStrings(entry.Value)),
            JsonArray jsonArray => jsonArray.SelectMany(FindStrings),
            JsonValue jsonValue when jsonValue.GetValueKind() == JsonValueKind.String => [jsonValue.GetValue<string>()],
            _ => [],
        };
    }

    private static IEnumerable<JsonNode> FindOperations(JsonNode document)
    {
        return document["paths"]!.AsObject()
            .SelectMany(path => path.Value!.AsObject())
            .Select(operation => operation.Value!);
    }

    private async Task<JsonNode> GetOpenApiDocumentAsync()
    {
        HttpResponseMessage response = await _client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

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
