using WaveMP.Core.Transcoding.Accelerators;

namespace WaveMP.Core.Transcoding.Arguments;

/// <summary>
/// The ffmpeg arguments for the graphics hardware: opening the device and the test encode of the detection. Only the
/// encoder runs on the hardware; frames are decoded and scaled in software and uploaded right before the encoder, which
/// works with every source format. Pure functions.
/// </summary>
public static class FfmpegHardwareArguments
{
    /// <summary>
    /// Opens the render device for the encoder: VA-API directly, Quick Sync derived from a VA-API device (the usual
    /// way on Linux).
    /// </summary>
    public static IReadOnlyList<string> InitDevice(HardwareAccelerator accelerator, string? device)
    {
        string path = device ?? string.Empty;
        return accelerator switch
        {
            HardwareAccelerator.None => [],
            HardwareAccelerator.Vaapi => ["-init_hw_device", $"vaapi=va:{path}", "-filter_hw_device", "va"],
            HardwareAccelerator.Qsv =>
            [
                "-init_hw_device", $"vaapi=va:{path}",
                "-init_hw_device", "qsv=qs@va",
                "-filter_hw_device", "qs",
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(accelerator), accelerator, "Unknown accelerator."),
        };
    }

    /// <summary>
    /// The filter that hands the scaled frames to the encoder of the accelerator.
    /// </summary>
    public static string UploadFilter(HardwareAccelerator accelerator)
    {
        return accelerator switch
        {
            HardwareAccelerator.None => "format=yuv420p",
            HardwareAccelerator.Vaapi => "format=nv12,hwupload",
            HardwareAccelerator.Qsv => "format=nv12,hwupload=extra_hw_frames=64",
            _ => throw new ArgumentOutOfRangeException(nameof(accelerator), accelerator, "Unknown accelerator."),
        };
    }

    /// <summary>
    /// The H.264 encoder of the accelerator.
    /// </summary>
    public static string EncoderOf(HardwareAccelerator accelerator)
    {
        return accelerator switch
        {
            HardwareAccelerator.None => "libx264",
            HardwareAccelerator.Vaapi => "h264_vaapi",
            HardwareAccelerator.Qsv => "h264_qsv",
            _ => throw new ArgumentOutOfRangeException(nameof(accelerator), accelerator, "Unknown accelerator."),
        };
    }

    /// <summary>
    /// Encodes one second of a generated test picture with the hardware and discards the result.
    /// </summary>
    public static IReadOnlyList<string> TestEncode(HardwareAccelerator accelerator, string device)
    {
        return
        [
            "-hide_banner", "-nostdin", "-loglevel", "error",
            .. InitDevice(accelerator, device),
            "-f", "lavfi", "-i", "testsrc2=size=320x240:rate=25:duration=1",
            "-vf", UploadFilter(accelerator),
            "-c:v", EncoderOf(accelerator),
            "-f", "null", "-",
        ];
    }

    /// <summary>
    /// Only checks that ffmpeg starts.
    /// </summary>
    public static IReadOnlyList<string> Version { get; } = ["-hide_banner", "-version"];
}
