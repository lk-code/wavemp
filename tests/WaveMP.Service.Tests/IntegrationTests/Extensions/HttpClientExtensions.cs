using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Tests.IntegrationTests.Extensions;

/// <summary>
/// Helpers for the API integration tests.
/// </summary>
internal static class HttpClientExtensions
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// A short random text for unique names, e.g. "Lars {unique} Krämer".
    /// </summary>
    public static string Unique()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    public static HttpClient CreateAuthorizedClient(this WaveMpWebApplicationFactory factory, string token)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<ProfileDto> GetBuiltInAdminAsync(this HttpClient client)
    {
        PageDto<ProfileDto> page = await client.GetJsonAsync<PageDto<ProfileDto>>("/api/v1/profiles?limit=100");
        return page.Items.Single(profile => profile.IsBuiltIn);
    }

    /// <summary>
    /// Selects the profile and returns the new session with its token.
    /// </summary>
    public static async Task<CreatedSessionDto> CreateSessionAsync(this HttpClient client, Guid profileId)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/sessions",
            new { profileId },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatedSessionDto>(JsonOptions))!;
    }

    /// <summary>
    /// Creates a client with a session of the built-in admin profile.
    /// </summary>
    public static async Task<HttpClient> CreateAdminClientAsync(this WaveMpWebApplicationFactory factory)
    {
        using HttpClient anonymous = factory.CreateClient();
        ProfileDto admin = await anonymous.GetBuiltInAdminAsync();
        CreatedSessionDto session = await anonymous.CreateSessionAsync(admin.Id);
        return factory.CreateAuthorizedClient(session.Token);
    }

    /// <summary>
    /// Creates a user through the API of an admin client.
    /// </summary>
    public static async Task<UserDto> CreateUserAsync(this HttpClient adminClient, object request)
    {
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/v1/users", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions))!;
    }

    public static async Task<T> GetJsonAsync<T>(this HttpClient client, string path)
    {
        HttpResponseMessage response = await client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    /// <summary>
    /// Checks that the response is a Problem Details response with the status and the code.
    /// </summary>
    public static async Task ShouldBeProblemAsync(
        this HttpResponseMessage response,
        HttpStatusCode status,
        string code)
    {
        string content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, content);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        JsonNode problem = JsonNode.Parse(content)!;
        problem["code"]!.GetValue<string>().Should().Be(code);
        problem["status"]!.GetValue<int>().Should().Be((int)status);
    }

    /// <summary>
    /// Checks that the response is a validation problem (400, <c>request.invalid</c>) and returns its errors by field.
    /// </summary>
    public static async Task<JsonObject> ShouldBeValidationProblemAsync(this HttpResponseMessage response)
    {
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "request.invalid");
        JsonNode problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        return problem["errors"]!.AsObject();
    }
}
