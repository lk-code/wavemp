using System.Text.Json;
using AwesomeAssertions;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Validations;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Profiles;
using WaveMP.Core.Transcoding.Services;
using WaveMP.Core.Transcoding.Transcodes;
using WaveMP.Data.Projections;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Tests.Mappings;

[TestFixture]
public class TranscodingMappingExtensionsTests
{
    [Test]
    public void ToDto_Status_CombinesHardwareAndJobs()
    {
        DateTimeOffset detectedAt = DateTimeOffset.UnixEpoch.AddDays(1);
        TranscodingStatus status = new(
            new HardwareAccelerationStatus(
                HardwareAccelerationState.Unavailable,
                HardwareAccelerator.None,
                "/dev/dri/renderD128",
                HardwareUnavailableReason.TestFailed,
                true,
                detectedAt),
            true,
            1,
            2);

        TranscodingStatusDto dto = status.ToDto();

        dto.Should().BeEquivalentTo(new TranscodingStatusDto
        {
            Available = true,
            FfmpegAvailable = true,
            CacheAvailable = true,
            Hardware = new HardwareAccelerationDto
            {
                State = HardwareAccelerationStateDto.Unavailable,
                Accelerator = HardwareAcceleratorDto.None,
                Device = "/dev/dri/renderD128",
                Reason = HardwareUnavailableReasonDto.TestFailed,
                DetectedAt = detectedAt,
            },
            RunningJobs = 1,
            MaxParallelJobs = 2,
        });
    }

    [Test]
    public void ToDto_Job_CarriesTheTrack()
    {
        TranscodeJobInfo job = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            HlsMedia.Video,
            HardwareAccelerator.Qsv,
            3,
            7,
            40,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            Guid.NewGuid());
        TrackRow track = new(
            job.TrackId,
            "Clip",
            null,
            "Band",
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            null,
            TimeSpan.FromSeconds(240),
            false,
            true,
            false,
            DateTimeOffset.UnixEpoch);

        TranscodeJobDto dto = new TranscodeJobItem(job, track).ToDto();

        dto.Should().BeEquivalentTo(new
        {
            job.Id,
            job.TrackId,
            Media = HlsMediaDto.Video,
            Accelerator = HardwareAcceleratorDto.Qsv,
            StartSegment = 3,
            CompletedSegments = 7,
            SegmentCount = 40,
            job.StartedAt,
            job.LastAccessAt,
            job.UserId,
        });
        dto.Track!.Title.Should().Be("Clip");
        new TranscodeJobItem(job, null).ToDto().Track.Should().BeNull();
    }

    [Test]
    public void Settings_MapBothWays()
    {
        TranscodingSettings settings = new()
        {
            HardwareAcceleration = HardwareAccelerationMode.Off,
            MaxParallelJobs = 3,
            MaxVideoHeight = 720,
            AudioBitrateKbps = 192,
        };

        TranscodingSettingsDto dto = settings.ToDto();
        UpdateTranscodingSettingsCommand command = new UpdateTranscodingSettingsDto
        {
            HardwareAcceleration = HardwareAccelerationModeDto.Auto,
            MaxVideoHeight = 1440,
        }.ToCommand();

        dto.Should().BeEquivalentTo(new TranscodingSettingsDto
        {
            HardwareAcceleration = HardwareAccelerationModeDto.Off,
            MaxParallelJobs = 3,
            MaxVideoHeight = 720,
            AudioBitrateKbps = 192,
        });
        command.Should().Be(new UpdateTranscodingSettingsCommand(HardwareAccelerationMode.Auto, null, 1440, null));
    }

    [Test]
    public void Enums_MapEveryValue()
    {
        Enum.GetValues<HardwareAccelerationState>().Select(value => value.ToDto().ToString())
            .Should().Equal(Enum.GetValues<HardwareAccelerationState>().Select(value => value.ToString()));
        Enum.GetValues<HardwareAccelerator>().Select(value => value.ToDto().ToString())
            .Should().Equal(Enum.GetValues<HardwareAccelerator>().Select(value => value.ToString()));
        Enum.GetValues<HardwareUnavailableReason>().Select(value => value.ToDto().ToString())
            .Should().Equal(Enum.GetValues<HardwareUnavailableReason>().Select(value => value.ToString()));
        Enum.GetValues<HardwareAccelerationMode>().Select(value => value.ToDto().ToEntity())
            .Should().Equal(Enum.GetValues<HardwareAccelerationMode>());
        Enum.GetValues<HlsMedia>().Select(value => value.ToDto().ToEntity())
            .Should().Equal(Enum.GetValues<HlsMedia>());
    }

    [Test]
    public void UnavailableReason_IsSerializedAsCode()
    {
        JsonSerializer.Serialize(HardwareUnavailableReasonDto.FfmpegMissing).Should().Be("\"ffmpeg_missing\"");
        JsonSerializer.Serialize(HardwareUnavailableReasonDto.NoDevice).Should().Be("\"no_device\"");
    }

    [Test]
    public void ToJsonName_EveryField_IsThePropertyOfTheRequest()
    {
        Enum.GetValues<TranscodingSettingsField>().Select(field => field.ToJsonName())
            .Should().Equal("hardwareAcceleration", "maxParallelJobs", "maxVideoHeight", "audioBitrateKbps");
    }
}
