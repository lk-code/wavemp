using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Validations;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Profiles;
using WaveMP.Core.Transcoding.Services;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class TranscodingMappingExtensions
{
    public static TranscodingStatusDto ToDto(this TranscodingStatus status)
    {
        return new TranscodingStatusDto
        {
            Available = status.Available,
            FfmpegAvailable = status.Hardware.FfmpegAvailable,
            CacheAvailable = status.CacheAvailable,
            Hardware = status.Hardware.ToDto(),
            RunningJobs = status.RunningJobs,
            MaxParallelJobs = status.MaxParallelJobs,
        };
    }

    public static HardwareAccelerationDto ToDto(this HardwareAccelerationStatus status)
    {
        return new HardwareAccelerationDto
        {
            State = status.State.ToDto(),
            Accelerator = status.Accelerator.ToDto(),
            Device = status.Device,
            Reason = status.Reason?.ToDto(),
            DetectedAt = status.DetectedAt,
        };
    }

    public static TranscodeJobDto ToDto(this TranscodeJobItem item)
    {
        return new TranscodeJobDto
        {
            Id = item.Job.Id,
            TrackId = item.Job.TrackId,
            Track = item.Track?.ToDto(),
            Media = item.Job.Media.ToDto(),
            Accelerator = item.Job.Accelerator.ToDto(),
            StartSegment = item.Job.StartSegment,
            CompletedSegments = item.Job.CompletedSegments,
            SegmentCount = item.Job.SegmentCount,
            StartedAt = item.Job.StartedAt,
            LastAccessAt = item.Job.LastAccessAt,
            UserId = item.Job.UserId,
        };
    }

    public static HlsMediaDto ToDto(this HlsMedia media)
    {
        return media switch
        {
            HlsMedia.Audio => HlsMediaDto.Audio,
            HlsMedia.Video => HlsMediaDto.Video,
            _ => throw new ArgumentOutOfRangeException(nameof(media), media, "Unknown media."),
        };
    }

    public static HlsMedia ToEntity(this HlsMediaDto media)
    {
        return media switch
        {
            HlsMediaDto.Audio => HlsMedia.Audio,
            HlsMediaDto.Video => HlsMedia.Video,
            _ => throw new ArgumentOutOfRangeException(nameof(media), media, "Unknown media."),
        };
    }

    public static HardwareAccelerationStateDto ToDto(this HardwareAccelerationState state)
    {
        return state switch
        {
            HardwareAccelerationState.Detecting => HardwareAccelerationStateDto.Detecting,
            HardwareAccelerationState.Active => HardwareAccelerationStateDto.Active,
            HardwareAccelerationState.Unavailable => HardwareAccelerationStateDto.Unavailable,
            HardwareAccelerationState.Disabled => HardwareAccelerationStateDto.Disabled,
            HardwareAccelerationState.Failed => HardwareAccelerationStateDto.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown state."),
        };
    }

    public static HardwareAcceleratorDto ToDto(this HardwareAccelerator accelerator)
    {
        return accelerator switch
        {
            HardwareAccelerator.None => HardwareAcceleratorDto.None,
            HardwareAccelerator.Vaapi => HardwareAcceleratorDto.Vaapi,
            HardwareAccelerator.Qsv => HardwareAcceleratorDto.Qsv,
            _ => throw new ArgumentOutOfRangeException(nameof(accelerator), accelerator, "Unknown accelerator."),
        };
    }

    public static HardwareUnavailableReasonDto ToDto(this HardwareUnavailableReason reason)
    {
        return reason switch
        {
            HardwareUnavailableReason.FfmpegMissing => HardwareUnavailableReasonDto.FfmpegMissing,
            HardwareUnavailableReason.NoDevice => HardwareUnavailableReasonDto.NoDevice,
            HardwareUnavailableReason.TestFailed => HardwareUnavailableReasonDto.TestFailed,
            HardwareUnavailableReason.EncodeFailed => HardwareUnavailableReasonDto.EncodeFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown reason."),
        };
    }

    public static TranscodingSettingsDto ToDto(this TranscodingSettings settings)
    {
        return new TranscodingSettingsDto
        {
            HardwareAcceleration = settings.HardwareAcceleration.ToDto(),
            MaxParallelJobs = settings.MaxParallelJobs,
            MaxVideoHeight = settings.MaxVideoHeight,
            AudioBitrateKbps = settings.AudioBitrateKbps,
        };
    }

    public static UpdateTranscodingSettingsCommand ToCommand(this UpdateTranscodingSettingsDto request)
    {
        return new UpdateTranscodingSettingsCommand(
            request.HardwareAcceleration?.ToEntity(),
            request.MaxParallelJobs,
            request.MaxVideoHeight,
            request.AudioBitrateKbps);
    }

    public static HardwareAccelerationModeDto ToDto(this HardwareAccelerationMode mode)
    {
        return mode switch
        {
            HardwareAccelerationMode.Auto => HardwareAccelerationModeDto.Auto,
            HardwareAccelerationMode.Off => HardwareAccelerationModeDto.Off,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown mode."),
        };
    }

    public static HardwareAccelerationMode ToEntity(this HardwareAccelerationModeDto mode)
    {
        return mode switch
        {
            HardwareAccelerationModeDto.Auto => HardwareAccelerationMode.Auto,
            HardwareAccelerationModeDto.Off => HardwareAccelerationMode.Off,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown mode."),
        };
    }

    /// <summary>
    /// The JSON name of the field in <see cref="UpdateTranscodingSettingsDto"/>, the key of its validation errors.
    /// </summary>
    public static string ToJsonName(this TranscodingSettingsField field)
    {
        return field switch
        {
            TranscodingSettingsField.HardwareAcceleration => "hardwareAcceleration",
            TranscodingSettingsField.MaxParallelJobs => "maxParallelJobs",
            TranscodingSettingsField.MaxVideoHeight => "maxVideoHeight",
            TranscodingSettingsField.AudioBitrateKbps => "audioBitrateKbps",
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown field."),
        };
    }
}
