using System.Text.Json;
using AwesomeAssertions;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Tests.Mappings;

[TestFixture]
public class JobMappingExtensionsTests
{
    [TestCase(JobIds.LibraryScan, JobIdDto.LibraryScan, "library-scan")]
    [TestCase(JobIds.CoverCache, JobIdDto.CoverCache, "cover-cache")]
    public void ToJobIdDto_EveryJob_HasItsIdInTheApi(string jobId, JobIdDto expected, string json)
    {
        jobId.ToJobIdDto().Should().Be(expected);
        JsonSerializer.Serialize(expected).Should().Be($"\"{json}\"");
    }

    [Test]
    public void ToDto_EveryTrigger_MapsBothWays()
    {
        foreach (JobRunTrigger trigger in Enum.GetValues<JobRunTrigger>())
        {
            trigger.ToDto().ToEntity().Should().Be(trigger);
        }

        JobRunTrigger.LibraryScan.ToDto().Should().Be(JobTriggerDto.LibraryScan);
    }

    [Test]
    public void ToDto_EveryErrorCode_HasAStableCode()
    {
        Enum.GetValues<JobErrorCode>().Select(code => code.ToDto()).Should().OnlyHaveUniqueItems();
        JsonSerializer.Serialize(JobErrorCode.FfmpegUnavailable.ToDto()).Should().Be("\"cache.ffmpeg_unavailable\"");
        JsonSerializer.Serialize(JobErrorCode.CacheFolderUnwritable.ToDto())
            .Should().Be("\"cache.folder_unwritable\"");
    }
}
