using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;

namespace WaveMP.Service.Tests.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class SessionsApiTests
{
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private ProfileDto _admin = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_sessions",
            freshDatabase: true);
        _client = _factory.CreateClient();
        _admin = await _client.GetBuiltInAdminAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client?.Dispose();
        await _factory.DeleteDatabaseAsync();
        await _factory.DisposeAsync();
    }

    [Test]
    public async Task CreateSession_WithProfile_ReturnsTokenAndLocationOfTheCurrentSession()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/v1/sessions",
            new { profileId = _admin.Id },
            HttpClientExtensions.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.AbsolutePath.Should().Be("/api/v1/sessions/current");
        CreatedSessionDto created = (await response.Content.ReadFromJsonAsync<CreatedSessionDto>(
            HttpClientExtensions.JsonOptions))!;
        created.Token.Should().NotBeNullOrWhiteSpace();
        created.Session.Profile.Id.Should().Be(_admin.Id);
        created.Session.Profile.Role.Should().Be(RoleDto.Admin);
        created.Session.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(365), TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task CreateSession_WithUnknownProfile_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/v1/sessions",
            new { profileId = Guid.NewGuid() },
            HttpClientExtensions.JsonOptions);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "profile.not_found");
    }

    [TestCase("{}")]
    [TestCase("{\"profileId\":\"not-a-guid\"}")]
    [TestCase("")]
    public async Task CreateSession_WithInvalidBody_ReturnsBadRequest(string body)
    {
        using StringContent content = new(body, System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _client.PostAsync("/api/v1/sessions", content);

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "request.invalid");
    }

    [Test]
    public async Task GetCurrentSession_WithToken_ReturnsTheSessionWithItsProfile()
    {
        CreatedSessionDto created = await _client.CreateSessionAsync(_admin.Id);
        using HttpClient client = _factory.CreateAuthorizedClient(created.Token);

        SessionDto session = await client.GetJsonAsync<SessionDto>("/api/v1/sessions/current");

        session.Id.Should().Be(created.Session.Id);
        session.Profile.Id.Should().Be(_admin.Id);
    }

    [Test]
    public async Task GetCurrentSession_WithoutToken_ReturnsUnauthorizedProblem()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/v1/sessions/current");

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        response.Headers.WwwAuthenticate.Should().ContainSingle().Which.Scheme.Should().Be("Bearer");
    }

    [TestCase("not-a-token")]
    [TestCase("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIn0.c2lnbmF0dXJl")]
    public async Task GetCurrentSession_WithInvalidToken_ReturnsUnauthorized(string token)
    {
        using HttpClient client = _factory.CreateAuthorizedClient(token);

        HttpResponseMessage response = await client.GetAsync("/api/v1/sessions/current");

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task DeleteCurrentSession_WithToken_EndsTheSession()
    {
        CreatedSessionDto created = await _client.CreateSessionAsync(_admin.Id);
        using HttpClient client = _factory.CreateAuthorizedClient(created.Token);

        HttpResponseMessage deleted = await client.DeleteAsync("/api/v1/sessions/current");
        HttpResponseMessage afterwards = await client.GetAsync("/api/v1/sessions/current");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await afterwards.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task DeleteCurrentSession_WithoutToken_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.DeleteAsync("/api/v1/sessions/current");

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task CreateSession_ForAnotherProfile_KeepsOtherSessionsActive()
    {
        CreatedSessionDto first = await _client.CreateSessionAsync(_admin.Id);
        await _client.CreateSessionAsync(_admin.Id);
        using HttpClient client = _factory.CreateAuthorizedClient(first.Token);

        HttpResponseMessage response = await client.GetAsync("/api/v1/sessions/current");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Token_OfDeletedProfile_IsRejected()
    {
        using HttpClient admin = await _factory.CreateAdminClientAsync();
        UserDto user = await admin.CreateUserAsync(new { name = $"Gone {HttpClientExtensions.Unique()}" });
        CreatedSessionDto session = await _client.CreateSessionAsync(user.Id);
        using HttpClient client = _factory.CreateAuthorizedClient(session.Token);
        (await client.GetAsync("/api/v1/sessions/current")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await admin.DeleteAsync($"/api/v1/users/{user.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage response = await client.GetAsync("/api/v1/sessions/current");

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }
}
