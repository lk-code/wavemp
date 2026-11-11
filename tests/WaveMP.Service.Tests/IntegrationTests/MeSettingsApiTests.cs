using System.Net;
using System.Net.Http.Json;
using System.Text;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;

namespace WaveMP.Service.Tests.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class MeSettingsApiTests
{
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_settings",
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

    [Test]
    public async Task GetMySettings_ForNewProfile_ReturnsEnglish()
    {
        (Guid _, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            UserSettingsDto settings = await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings");

            settings.Language.Should().Be(LanguageDto.En);
            settings.ReplayGain.Should().Be(ReplayGainModeDto.Off);
        }
    }

    [TestCase("Track", ReplayGainModeDto.Track)]
    [TestCase("Album", ReplayGainModeDto.Album)]
    [TestCase("Off", ReplayGainModeDto.Off)]
    public async Task UpdateMySettings_WithReplayGain_KeepsItAndTheLanguage(string value, ReplayGainModeDto expected)
    {
        (Guid _, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            await client.PatchAsJsonAsync("/api/v1/me/settings", new { language = "de" }, HttpClientExtensions.JsonOptions);

            HttpResponseMessage response = await client.PatchAsJsonAsync(
                "/api/v1/me/settings",
                new { replayGain = value },
                HttpClientExtensions.JsonOptions);
            UserSettingsDto read = await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain($"\"replayGain\":\"{value}\"");
            read.ReplayGain.Should().Be(expected);
            read.Language.Should().Be(LanguageDto.De);
        }
    }

    [TestCase("\"Loud\"")]
    [TestCase("1")]
    public async Task UpdateMySettings_WithInvalidReplayGain_ReturnsBadRequest(string value)
    {
        (Guid _, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            using StringContent body = new($"{{\"replayGain\":{value}}}", Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PatchAsync("/api/v1/me/settings", body);

            IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
            errors.Should().Contain(error => error.Field == "replayGain");
            (await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings")).ReplayGain
                .Should().Be(ReplayGainModeDto.Off);
        }
    }

    [Test]
    public async Task UpdateMySettings_WithLanguage_KeepsItForTheProfileAcrossSessions()
    {
        (Guid profileId, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            HttpResponseMessage response = await client.PatchAsJsonAsync(
                "/api/v1/me/settings",
                new { language = "de" },
                HttpClientExtensions.JsonOptions);
            UserSettingsDto read = await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings");
            using HttpClient otherSession = _factory.CreateAuthorizedClient(
                (await _anonymous.CreateSessionAsync(profileId)).Token);
            UserSettingsDto otherRead = await otherSession.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain("\"language\":\"de\"");
            read.Language.Should().Be(LanguageDto.De);
            otherRead.Language.Should().Be(LanguageDto.De);
        }
    }

    [Test]
    public async Task UpdateMySettings_WithoutLanguage_KeepsIt()
    {
        (Guid _, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            await client.PatchAsJsonAsync("/api/v1/me/settings", new { language = "de" }, HttpClientExtensions.JsonOptions);

            HttpResponseMessage response = await client.PatchAsJsonAsync(
                "/api/v1/me/settings",
                new { },
                HttpClientExtensions.JsonOptions);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings")).Language.Should().Be(LanguageDto.De);
        }
    }

    [Test]
    public async Task UpdateMySettings_WithUnsupportedLanguage_ReturnsBadRequest()
    {
        (Guid _, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            HttpResponseMessage response = await client.PatchAsJsonAsync(
                "/api/v1/me/settings",
                new { language = "fr" },
                HttpClientExtensions.JsonOptions);

            await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "request.invalid");
        }
    }

    [TestCase("0")]
    [TestCase("1")]
    [TestCase("7")]
    public async Task UpdateMySettings_WithLanguageAsNumber_ReturnsBadRequestAndKeepsTheLanguage(string language)
    {
        (Guid _, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            using StringContent body = new($"{{\"language\":{language}}}", Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PatchAsync("/api/v1/me/settings", body);

            await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "request.invalid");
            (await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings")).Language.Should().Be(LanguageDto.En);
        }
    }

    [TestCase("GET")]
    [TestCase("PATCH")]
    public async Task MySettings_WithoutToken_ReturnUnauthorized(string method)
    {
        using HttpRequestMessage request = new(new HttpMethod(method), "/api/v1/me/settings")
        {
            Content = method == "PATCH" ? JsonContent.Create(new { language = "de" }) : null,
        };

        HttpResponseMessage response = await _anonymous.SendAsync(request);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    private async Task<(Guid ProfileId, HttpClient Client)> CreateProfileAsync()
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"Settings {HttpClientExtensions.Unique()}" });
        CreatedSessionDto session = await _anonymous.CreateSessionAsync(user.Id);
        return (user.Id, _factory.CreateAuthorizedClient(session.Token));
    }
}
