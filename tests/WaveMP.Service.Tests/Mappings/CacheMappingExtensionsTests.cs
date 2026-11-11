using AwesomeAssertions;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Validations;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Tests.Mappings;

[TestFixture]
public class CacheMappingExtensionsTests
{
    [Test]
    public void ToDto_Usage_MapsTheTotalsAndAreas()
    {
        CacheUsage usage = new(true, 150, 3, 2_000_000_000, new CacheAreaUsage(100, 2), new CacheAreaUsage(50, 1));

        usage.ToDto().Should().BeEquivalentTo(new CacheUsageDto
        {
            IsAvailable = true,
            SizeBytes = 150,
            EntryCount = 3,
            MaxSizeBytes = 2_000_000_000,
            Covers = new CacheAreaUsageDto { SizeBytes = 100, EntryCount = 2 },
            Transcodes = new CacheAreaUsageDto { SizeBytes = 50, EntryCount = 1 },
        });
    }

    [Test]
    public void ToDtoAndToCommand_Settings_MapTheSizeLimit()
    {
        new CacheSettings { MaxSizeMegabytes = 750 }.ToDto().MaxSizeMegabytes.Should().Be(750);
        new UpdateCacheSettingsDto { MaxSizeMegabytes = 900 }.ToCommand()
            .Should().Be(new UpdateCacheSettingsCommand(900));
        new UpdateCacheSettingsDto().ToCommand().Should().Be(new UpdateCacheSettingsCommand());
    }

    [Test]
    public void ToJsonName_EveryField_IsTheNameInTheRequest()
    {
        Enum.GetValues<CacheSettingsField>().Select(field => field.ToJsonName())
            .Should().Equal("maxSizeMegabytes");
    }
}
