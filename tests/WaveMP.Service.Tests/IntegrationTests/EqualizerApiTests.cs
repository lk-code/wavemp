using System.Net;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;

namespace WaveMP.Service.Tests.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class EqualizerApiTests
{
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_equalizer",
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
    public async Task GetEqualizer_WithoutToken_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _anonymous.GetAsync("/api/v1/equalizer");

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task GetEqualizer_WithSession_ReturnsTenOctaveBands()
    {
        EqualizerDto equalizer = await _admin.GetJsonAsync<EqualizerDto>("/api/v1/equalizer");

        equalizer.MinGainDb.Should().Be(-12);
        equalizer.MaxGainDb.Should().Be(12);
        equalizer.Bands.Select(band => band.FrequencyHz)
            .Should().Equal(31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000);
        equalizer.Bands[0].FilterType.Should().Be(EqualizerFilterTypeDto.LowShelf);
        equalizer.Bands[^1].FilterType.Should().Be(EqualizerFilterTypeDto.HighShelf);
        equalizer.Bands.Skip(1).SkipLast(1).Should().OnlyContain(band =>
            band.FilterType == EqualizerFilterTypeDto.Peaking && band.Q > 0);
    }

    [Test]
    public async Task GetEqualizer_WithSession_ReturnsEveryPresetExceptCustom()
    {
        EqualizerDto equalizer = await _admin.GetJsonAsync<EqualizerDto>("/api/v1/equalizer");

        equalizer.Presets.Select(curve => curve.Preset).Should().BeEquivalentTo(
            Enum.GetValues<EqualizerPresetDto>().Where(preset => preset != EqualizerPresetDto.Custom));
        equalizer.Presets.Should().AllSatisfy(curve =>
        {
            curve.GainsDb.Should().HaveCount(10);
            curve.GainsDb.Should().OnlyContain(gain => gain >= -12 && gain <= 12);
        });
        equalizer.Presets.Single(curve => curve.Preset == EqualizerPresetDto.Flat).GainsDb
            .Should().OnlyContain(gain => gain == 0);
    }
}
