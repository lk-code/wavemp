using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// The format of validation problems: every error has its field, a code and the limits that apply, never a text.
/// </summary>
[TestFixture]
[Category("Integration")]
public class ValidationProblemApiTests
{
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_validation",
            freshDatabase: true);
        _anonymous = _factory.CreateClient();
        _admin = await _factory.CreateAdminClientAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _admin?.Dispose();
        _anonymous?.Dispose();
        await _factory.DeleteDatabaseAsync();
        await _factory.DisposeAsync();
    }

    private static IEnumerable<TestCaseData> InvalidUserBodies()
    {
        yield return new TestCaseData("{}", Error("name", ValidationErrorCodeDto.Required))
            .SetArgDisplayNames("MissingName");
        yield return new TestCaseData("""{"name":"   "}""", Error("name", ValidationErrorCodeDto.Required))
            .SetArgDisplayNames("WhiteSpaceName");
        yield return new TestCaseData(
                $$"""{"name":"{{new string('n', 51)}}"}""",
                Error("name", ValidationErrorCodeDto.MaxLength, max: 50))
            .SetArgDisplayNames("TooLongName");
        yield return new TestCaseData(
                """{"name":"Valid","color":"blue"}""",
                Error("color", ValidationErrorCodeDto.InvalidFormat))
            .SetArgDisplayNames("Color");
        yield return new TestCaseData(
                """{"name":"Valid","initials":"ABCD"}""",
                Error("initials", ValidationErrorCodeDto.InvalidValue))
            .SetArgDisplayNames("Initials");
        yield return new TestCaseData(
                """{"name":"Valid","role":"Owner"}""",
                Error("role", ValidationErrorCodeDto.InvalidFormat))
            .SetArgDisplayNames("UnknownRole");
    }

    [TestCaseSource(nameof(InvalidUserBodies))]
    public async Task CreateUser_WithInvalidBody_ReportsTheCodeOfTheField(string body, ValidationErrorDto expected)
    {
        HttpResponseMessage response = await _admin.SendJsonAsync("POST", "/api/v1/users", body);

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainEquivalentOf(expected);
        errors.Should().OnlyContain(error => error.Field == expected.Field || error.Field == string.Empty);
    }

    [Test]
    public async Task UpdateUser_WithEmptyName_ReportsTheMinimumLength()
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"Short {HttpClientExtensions.Unique()}" });

        HttpResponseMessage response = await _admin.SendJsonAsync("PATCH", $"/api/v1/users/{user.Id}", """{"name":""}""");

        (await response.ShouldBeValidationProblemAsync()).Should().BeEquivalentTo(
            [Error("name", ValidationErrorCodeDto.MinLength, min: 1)]);
    }

    private static IEnumerable<TestCaseData> InvalidQueries()
    {
        yield return new TestCaseData("limit=0", Error("limit", ValidationErrorCodeDto.OutOfRange, 1, 100))
            .SetArgDisplayNames("LimitTooSmall");
        yield return new TestCaseData("offset=-1", Error("offset", ValidationErrorCodeDto.OutOfRange, 0, int.MaxValue))
            .SetArgDisplayNames("NegativeOffset");
        yield return new TestCaseData("offset=abc", Error("offset", ValidationErrorCodeDto.InvalidFormat))
            .SetArgDisplayNames("OffsetNoNumber");
        yield return new TestCaseData("sort=Age", Error("sort", ValidationErrorCodeDto.InvalidFormat))
            .SetArgDisplayNames("UnknownSortName");
        // Model binding already rejects numbers that are no member of the enum.
        yield return new TestCaseData("sort=5", Error("sort", ValidationErrorCodeDto.InvalidFormat))
            .SetArgDisplayNames("UndefinedSortNumber");
    }

    [TestCaseSource(nameof(InvalidQueries))]
    public async Task ListUsers_WithInvalidQuery_ReportsTheCodeOfTheParameter(string query, ValidationErrorDto expected)
    {
        HttpResponseMessage response = await _admin.GetAsync($"/api/v1/users?{query}");

        (await response.ShouldBeValidationProblemAsync()).Should().BeEquivalentTo([expected]);
    }

    [Test]
    public async Task SearchLibrary_WithWhiteSpaceOnly_ReportsTheSearchTextAsRequired()
    {
        HttpResponseMessage response = await _admin.GetAsync("/api/v1/search?q=%20%20");

        (await response.ShouldBeValidationProblemAsync()).Should().BeEquivalentTo(
            [Error("q", ValidationErrorCodeDto.Required)]);
    }

    [Test]
    public async Task CreateSession_WithoutProfileId_ReportsItAsRequired()
    {
        HttpResponseMessage response = await _anonymous.SendJsonAsync("POST", "/api/v1/sessions", "{}");

        (await response.ShouldBeValidationProblemAsync()).Should().BeEquivalentTo(
            [Error("profileId", ValidationErrorCodeDto.Required)]);
    }

    [Test]
    public async Task CreateSession_WithoutBody_ReportsTheBodyAsRequired()
    {
        HttpResponseMessage response = await _anonymous.SendJsonAsync("POST", "/api/v1/sessions", string.Empty);

        (await response.ShouldBeValidationProblemAsync()).Should().BeEquivalentTo(
            [Error(string.Empty, ValidationErrorCodeDto.Required)]);
    }

    [Test]
    public async Task CreateSession_WithMalformedJson_ReportsTheBodyAsInvalidFormat()
    {
        HttpResponseMessage response = await _anonymous.SendJsonAsync("POST", "/api/v1/sessions", "not json");

        (await response.ShouldBeValidationProblemAsync()).Should()
            .ContainEquivalentOf(Error(string.Empty, ValidationErrorCodeDto.InvalidFormat))
            .And.OnlyContain(error => error.Field == string.Empty);
    }

    [Test]
    public async Task CreateSession_WithValueOfWrongType_ReportsTheMemberAsInvalidFormat()
    {
        HttpResponseMessage response = await _anonymous.SendJsonAsync(
            "POST",
            "/api/v1/sessions",
            """{"profileId":"not-a-guid"}""");

        (await response.ShouldBeValidationProblemAsync()).Should()
            .ContainEquivalentOf(Error("profileId", ValidationErrorCodeDto.InvalidFormat));
    }

    private static IEnumerable<TestCaseData> InvalidLibrarySettings()
    {
        string tooMany = string.Join(",", Enumerable.Range(0, 51).Select(index => $"\"a{index}\""));
        yield return new TestCaseData(
                """{"scanIntervalMinutes":5}""",
                new[] { Error("scanIntervalMinutes", ValidationErrorCodeDto.OutOfRange, 15, 1440) })
            .SetArgDisplayNames("Interval");
        yield return new TestCaseData(
                $$"""{"audioExtensions":[{{tooMany}}]}""",
                new[] { Error("audioExtensions", ValidationErrorCodeDto.MaxItems, max: 50) })
            .SetArgDisplayNames("TooManyExtensions");
        yield return new TestCaseData(
                """{"audioExtensions":["mp-3"]}""",
                new[] { Error("audioExtensions", ValidationErrorCodeDto.InvalidFormat) })
            .SetArgDisplayNames("MalformedExtension");
        yield return new TestCaseData(
                """{"audioExtensions":["mp3","mp4"]}""",
                new[] { Error("videoExtensions", ValidationErrorCodeDto.Duplicate) })
            .SetArgDisplayNames("ExtensionInTwoLists");
        yield return new TestCaseData(
                """{"audioExtensions":[],"videoExtensions":[]}""",
                new[]
                {
                    Error("audioExtensions", ValidationErrorCodeDto.Required),
                    Error("videoExtensions", ValidationErrorCodeDto.Required),
                })
            .SetArgDisplayNames("NoExtensions");
    }

    [TestCaseSource(nameof(InvalidLibrarySettings))]
    public async Task UpdateLibrarySettings_WithInvalidValues_ReportsCodesAndLimits(
        string body,
        ValidationErrorDto[] expected)
    {
        HttpResponseMessage response = await _admin.SendJsonAsync("PATCH", "/api/v1/settings/library", body);

        (await response.ShouldBeValidationProblemAsync()).Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task UpdateCacheSettings_OutOfRange_ReportsTheLimits()
    {
        HttpResponseMessage response = await _admin.SendJsonAsync(
            "PATCH",
            "/api/v1/settings/cache",
            """{"maxSizeMegabytes":99}""");

        (await response.ShouldBeValidationProblemAsync()).Should().BeEquivalentTo(
            [Error("maxSizeMegabytes", ValidationErrorCodeDto.OutOfRange, 100, 1_000_000)]);
    }

    [Test]
    public async Task ValidationProblem_Always_IsProblemDetailsWithoutErrorTexts()
    {
        HttpResponseMessage response = await _admin.SendJsonAsync("POST", "/api/v1/users", """{"color":"blue"}""");

        string content = await response.Content.ReadAsStringAsync();
        JsonObject problem = JsonNode.Parse(content)!.AsObject();
        problem["status"]!.GetValue<int>().Should().Be((int)HttpStatusCode.BadRequest);
        problem["code"]!.GetValue<string>().Should().Be("request.invalid");
        problem["type"]!.GetValue<string>().Should().NotBeNullOrEmpty();
        problem["errors"]!.AsArray().Select(error => error!["code"]!.GetValue<string>())
            .Should().BeEquivalentTo("required", "invalid_format");
        content.Should().NotContain("field is required").And.NotContain("is not valid");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetOpenApiDocument_Always_DescribesValidationErrorsWithNamedCodes()
    {
        string[] expectedCodes = [.. Enum.GetValues<ValidationErrorCodeDto>()
            .Select(code => JsonSerializer.Serialize(code).Trim('"'))];

        JsonNode document = JsonNode.Parse(await _anonymous.GetStringAsync("/openapi/v1.json"))!;

        JsonNode schemas = document["components"]!["schemas"]!;
        schemas.AsObject().Should().NotContainKeys("ValidationProblemDetails", "HttpValidationProblemDetails");
        JsonNode problem = schemas["ValidationProblem"]!;
        problem["required"]!.AsArray().Select(node => node!.GetValue<string>())
            .Should().Contain(["code", "errors"]);
        problem["properties"]!["errors"]!["items"]!["$ref"]!.GetValue<string>()
            .Should().Be("#/components/schemas/ValidationError");
        JsonNode error = schemas["ValidationError"]!;
        error["required"]!.AsArray().Select(node => node!.GetValue<string>())
            .Should().BeEquivalentTo("field", "code");
        error["properties"]!["code"]!["$ref"]!.GetValue<string>()
            .Should().Be("#/components/schemas/ValidationErrorCode");
        error["properties"]!.AsObject().Select(property => property.Key)
            .Should().BeEquivalentTo("field", "code", "min", "max");
        schemas["ValidationErrorCode"]!["enum"]!.AsArray().Select(node => node!.GetValue<string>())
            .Should().BeEquivalentTo(expectedCodes);
    }

    [Test]
    public async Task GetOpenApiDocument_Always_DescribesEveryBadRequestAsValidationProblem()
    {
        JsonNode document = JsonNode.Parse(await _anonymous.GetStringAsync("/openapi/v1.json"))!;

        List<string> references = [.. document["paths"]!.AsObject()
            .SelectMany(path => path.Value!.AsObject())
            .Select(operation => operation.Value!["responses"]?["400"])
            .OfType<JsonNode>()
            .Select(response => response["content"]!["application/problem+json"]!["schema"]!["$ref"]!
                .GetValue<string>())];

        references.Should().NotBeEmpty();
        references.Should().AllBe("#/components/schemas/ValidationProblem");
    }

    private static ValidationErrorDto Error(string field, ValidationErrorCodeDto code, int? min = null, int? max = null)
    {
        return new ValidationErrorDto { Field = field, Code = code, Min = min, Max = max };
    }
}
