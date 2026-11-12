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
    private static readonly double[] _zeros = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    private static readonly double[] _ownCurve = [6, 4.5, 3, 1, 0, -1, -2.5, 0, 2, 4];

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
            settings.PlayerViewMode.Should().Be(PlayerViewModeDto.Video);
        }
    }

    [TestCase("Cover", PlayerViewModeDto.Cover)]
    [TestCase("Visualization", PlayerViewModeDto.Visualization)]
    [TestCase("Video", PlayerViewModeDto.Video)]
    public async Task UpdateMySettings_WithPlayerViewMode_KeepsItAndTheOtherSettings(
        string value,
        PlayerViewModeDto expected)
    {
        (Guid profileId, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            await client.PatchAsJsonAsync(
                "/api/v1/me/settings",
                new { language = "de", replayGain = "Album" },
                HttpClientExtensions.JsonOptions);

            HttpResponseMessage response = await client.PatchAsJsonAsync(
                "/api/v1/me/settings",
                new { playerViewMode = value },
                HttpClientExtensions.JsonOptions);
            UserSettingsDto read = await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings");
            using HttpClient otherSession = _factory.CreateAuthorizedClient(
                (await _anonymous.CreateSessionAsync(profileId)).Token);
            UserSettingsDto otherRead = await otherSession.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain($"\"playerViewMode\":\"{value}\"");
            read.PlayerViewMode.Should().Be(expected);
            read.Language.Should().Be(LanguageDto.De);
            read.ReplayGain.Should().Be(ReplayGainModeDto.Album);
            otherRead.PlayerViewMode.Should().Be(expected);
        }
    }

    [TestCase("\"Lyrics\"")]
    [TestCase("1")]
    public async Task UpdateMySettings_WithInvalidPlayerViewMode_ReturnsBadRequest(string value)
    {
        (Guid _, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            using StringContent body = new($"{{\"playerViewMode\":{value}}}", Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PatchAsync("/api/v1/me/settings", body);

            IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
            errors.Should().ContainSingle(error => error.Field == "playerViewMode")
                .Which.Code.Should().Be(ValidationErrorCodeDto.InvalidFormat);
            (await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings")).PlayerViewMode
                .Should().Be(PlayerViewModeDto.Video);
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

    [Test]
    public async Task GetMySettings_ForNewProfile_ReturnsThePlaybackDefaults()
    {
        (Guid _, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            UserSettingsDto settings = await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings");

            settings.Equalizer.Enabled.Should().BeFalse();
            settings.Equalizer.Preset.Should().Be(EqualizerPresetDto.Flat);
            settings.Equalizer.PreampDb.Should().Be(0);
            settings.Equalizer.GainsDb.Should().Equal(_zeros);
            settings.Equalizer.CustomGainsDb.Should().Equal(_zeros);
            settings.Crossfade.Seconds.Should().Be(0);
            settings.Crossfade.AlbumsGapless.Should().BeTrue();
            settings.SleepTimer.FadeSeconds.Should().Be(10);
        }
    }

    [Test]
    public async Task UpdateMySettings_WithPreset_AppliesItsCurveAndKeepsTheOwnCurve()
    {
        (Guid _, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            await PatchAsync(client, new { equalizer = new { customGainsDb = _ownCurve } });

            HttpResponseMessage response = await PatchAsync(
                client,
                new { equalizer = new { enabled = true, preset = "Rock" } });
            UserSettingsDto read = await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain("\"preset\":\"Rock\"");
            read.Equalizer.Enabled.Should().BeTrue();
            read.Equalizer.Preset.Should().Be(EqualizerPresetDto.Rock);
            read.Equalizer.GainsDb.Should().Equal(5, 4, 3, 1, -1, -1, 1, 3, 4, 5);
            read.Equalizer.CustomGainsDb.Should().Equal(_ownCurve);
        }
    }

    [Test]
    public async Task UpdateMySettings_WithOwnCurve_AppliesItRoundedForTheProfileAcrossSessions()
    {
        (Guid profileId, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            HttpResponseMessage response = await PatchAsync(
                client,
                new
                {
                    equalizer = new
                    {
                        preset = "Custom",
                        preampDb = -2.04,
                        customGainsDb = new[] { 1.26, 0, 0, 0, 0, 0, 0, 0, 0, -11.96 },
                    },
                });
            using HttpClient otherSession = _factory.CreateAuthorizedClient(
                (await _anonymous.CreateSessionAsync(profileId)).Token);
            UserSettingsDto read = await otherSession.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            read.Equalizer.Preset.Should().Be(EqualizerPresetDto.Custom);
            read.Equalizer.PreampDb.Should().Be(-2);
            read.Equalizer.GainsDb.Should().Equal(1.3, 0, 0, 0, 0, 0, 0, 0, 0, -12);
            read.Equalizer.CustomGainsDb.Should().Equal(1.3, 0, 0, 0, 0, 0, 0, 0, 0, -12);
        }
    }

    [Test]
    public async Task UpdateMySettings_WithOneGroup_KeepsTheOtherSettings()
    {
        (Guid _, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            await PatchAsync(
                client,
                new
                {
                    language = "de",
                    replayGain = "Track",
                    playerViewMode = "Cover",
                    equalizer = new { enabled = true, preset = "Jazz", preampDb = -3 },
                    crossfade = new { seconds = 6, albumsGapless = false },
                    sleepTimer = new { fadeSeconds = 30 },
                });

            HttpResponseMessage response = await PatchAsync(client, new { crossfade = new { seconds = 12 } });
            UserSettingsDto read = await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            read.Crossfade.Seconds.Should().Be(12);
            read.Crossfade.AlbumsGapless.Should().BeFalse();
            read.Equalizer.Enabled.Should().BeTrue();
            read.Equalizer.Preset.Should().Be(EqualizerPresetDto.Jazz);
            read.Equalizer.PreampDb.Should().Be(-3);
            read.SleepTimer.FadeSeconds.Should().Be(30);
            read.Language.Should().Be(LanguageDto.De);
            read.ReplayGain.Should().Be(ReplayGainModeDto.Track);
            read.PlayerViewMode.Should().Be(PlayerViewModeDto.Cover);
        }
    }

    private static IEnumerable<TestCaseData> InvalidPlaybackSettings()
    {
        yield return new TestCaseData(
                "{\"equalizer\":{\"customGainsDb\":[0,0,0,0,0,0,0,0,0]}}",
                "equalizer.customGainsDb",
                ValidationErrorCodeDto.MinItems,
                10,
                null)
            .SetArgDisplayNames("NineGains");
        yield return new TestCaseData(
                "{\"equalizer\":{\"customGainsDb\":[0,0,0,0,0,0,0,0,0,0,0]}}",
                "equalizer.customGainsDb",
                ValidationErrorCodeDto.MaxItems,
                null,
                10)
            .SetArgDisplayNames("ElevenGains");
        yield return new TestCaseData(
                "{\"equalizer\":{\"customGainsDb\":[0,0,0,0,12.5,0,0,0,0,0]}}",
                "equalizer.customGainsDb",
                ValidationErrorCodeDto.OutOfRange,
                -12,
                12)
            .SetArgDisplayNames("GainOutOfRange");
        yield return new TestCaseData(
                "{\"equalizer\":{\"preampDb\":13}}",
                "equalizer.preampDb",
                ValidationErrorCodeDto.OutOfRange,
                -12,
                12)
            .SetArgDisplayNames("Preamp");
        yield return new TestCaseData(
                "{\"crossfade\":{\"seconds\":13}}",
                "crossfade.seconds",
                ValidationErrorCodeDto.OutOfRange,
                0,
                12)
            .SetArgDisplayNames("CrossfadeTooLong");
        yield return new TestCaseData(
                "{\"crossfade\":{\"seconds\":-1}}",
                "crossfade.seconds",
                ValidationErrorCodeDto.OutOfRange,
                0,
                12)
            .SetArgDisplayNames("CrossfadeNegative");
        yield return new TestCaseData(
                "{\"sleepTimer\":{\"fadeSeconds\":0}}",
                "sleepTimer.fadeSeconds",
                ValidationErrorCodeDto.OutOfRange,
                1,
                60)
            .SetArgDisplayNames("Fade");
        yield return new TestCaseData(
                "{\"equalizer\":{\"preset\":\"Loudness\"}}",
                "equalizer.preset",
                ValidationErrorCodeDto.InvalidFormat,
                null,
                null)
            .SetArgDisplayNames("UnknownPreset");
    }

    [TestCaseSource(nameof(InvalidPlaybackSettings))]
    public async Task UpdateMySettings_WithInvalidPlaybackSetting_ReturnsTheCodeAndKeepsTheSettings(
        string json,
        string field,
        ValidationErrorCodeDto code,
        int? min,
        int? max)
    {
        (Guid _, HttpClient client) = await CreateProfileAsync();
        using (client)
        {
            using StringContent body = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PatchAsync("/api/v1/me/settings", body);

            IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
            errors.Should().ContainSingle(error => error.Field == field)
                .Which.Should().BeEquivalentTo(new { Code = code, Min = min, Max = max });
            UserSettingsDto read = await client.GetJsonAsync<UserSettingsDto>("/api/v1/me/settings");
            read.Equalizer.Preset.Should().Be(EqualizerPresetDto.Flat);
            read.Equalizer.PreampDb.Should().Be(0);
            read.Equalizer.CustomGainsDb.Should().Equal(_zeros);
            read.Crossfade.Seconds.Should().Be(0);
            read.SleepTimer.FadeSeconds.Should().Be(10);
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

    private static Task<HttpResponseMessage> PatchAsync(HttpClient client, object request)
    {
        return client.PatchAsJsonAsync("/api/v1/me/settings", request, HttpClientExtensions.JsonOptions);
    }

    private async Task<(Guid ProfileId, HttpClient Client)> CreateProfileAsync()
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"Settings {HttpClientExtensions.Unique()}" });
        CreatedSessionDto session = await _anonymous.CreateSessionAsync(user.Id);
        return (user.Id, _factory.CreateAuthorizedClient(session.Token));
    }
}
