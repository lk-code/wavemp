using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;

namespace WaveMP.Service.Tests.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class SettingsApiTests
{
    private const string DatabaseName = WaveMpWebApplicationFactory.TestDatabaseName + "_settings";

    [Test]
    public async Task LibrarySettings_OfANewInstallation_AreTheDefaults()
    {
        await using WaveMpWebApplicationFactory factory = new(DatabaseName, freshDatabase: true);
        using HttpClient admin = await factory.CreateAdminClientAsync();

        LibrarySettingsDto settings = await admin.GetJsonAsync<LibrarySettingsDto>("/api/v1/settings/library");

        settings.ScanIntervalMinutes.Should().Be(360);
        settings.FileWatcherEnabled.Should().BeTrue();
        settings.AudioExtensions.Should().Contain(["mp3", "flac", "m4a", "opus"]);
        settings.VideoExtensions.Should().Contain(["mp4", "mkv"]);
        settings.ImageExtensions.Should().Contain(["jpg", "png"]);
        settings.AlbumCoverNames.Should().Equal("cover", "folder", "front", "album", "albumart");
        await factory.DeleteDatabaseAsync();
    }

    [Test]
    public async Task UpdateLibrarySettings_WithValidValues_NormalizesThemAndKeepsThemOverARestart()
    {
        LibrarySettingsDto updated;
        await using (WaveMpWebApplicationFactory first = new(DatabaseName, freshDatabase: true))
        {
            using HttpClient admin = await first.CreateAdminClientAsync();
            HttpResponseMessage response = await admin.PatchAsJsonAsync(
                "/api/v1/settings/library",
                new
                {
                    scanIntervalMinutes = 720,
                    fileWatcherEnabled = false,
                    audioExtensions = new[] { " .MP3", "Flac", "mp3" },
                    albumCoverNames = new[] { "Front", "cover" },
                },
                HttpClientExtensions.JsonOptions);
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            updated = (await response.Content.ReadFromJsonAsync<LibrarySettingsDto>(HttpClientExtensions.JsonOptions))!;
        }

        await using WaveMpWebApplicationFactory second = new(DatabaseName);
        using HttpClient restarted = await second.CreateAdminClientAsync();
        LibrarySettingsDto loaded = await restarted.GetJsonAsync<LibrarySettingsDto>("/api/v1/settings/library");

        updated.ScanIntervalMinutes.Should().Be(720);
        updated.FileWatcherEnabled.Should().BeFalse();
        updated.AudioExtensions.Should().Equal("mp3", "flac");
        updated.AlbumCoverNames.Should().Equal("front", "cover");
        updated.VideoExtensions.Should().Contain("mkv");
        loaded.Should().BeEquivalentTo(updated);
        (await restarted.GetJsonAsync<PageDto<JobDto>>("/api/v1/jobs")).Items
            .Single(job => job.Id == JobIdDto.LibraryScan).IntervalMinutes
            .Should().Be(720);
        await second.DeleteDatabaseAsync();
    }

    [TestCase("{\"scanIntervalMinutes\":5}", "scanIntervalMinutes")]
    [TestCase("{\"scanIntervalMinutes\":1441}", "scanIntervalMinutes")]
    [TestCase("{\"audioExtensions\":[\"mp-3\"]}", "audioExtensions")]
    [TestCase("{\"audioExtensions\":[\"mp3\",\"mp4\"]}", "videoExtensions")]
    [TestCase("{\"imageExtensions\":[\"jpg\",\"mkv\"]}", "imageExtensions")]
    [TestCase("{\"albumCoverNames\":[\"covers/front\"]}", "albumCoverNames")]
    [TestCase("{\"audioExtensions\":[],\"videoExtensions\":[]}", "audioExtensions")]
    public async Task UpdateLibrarySettings_WithInvalidValues_ReportsTheField(string body, string field)
    {
        await using WaveMpWebApplicationFactory factory = new(DatabaseName + "_invalid", freshDatabase: true);
        using HttpClient admin = await factory.CreateAdminClientAsync();

        HttpResponseMessage response = await admin.SendJsonAsync("PATCH", "/api/v1/settings/library", body);
        LibrarySettingsDto unchanged = await admin.GetJsonAsync<LibrarySettingsDto>("/api/v1/settings/library");

        JsonObject errors = await response.ShouldBeValidationProblemAsync();
        errors.Select(error => error.Key).Should().Contain(field);
        unchanged.ScanIntervalMinutes.Should().Be(360);
        unchanged.AudioExtensions.Should().Contain("mp3");
        await factory.DeleteDatabaseAsync();
    }
}
