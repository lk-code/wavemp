using System.Globalization;
using WaveMP.Core.Transcoding.Accelerators;
using WaveMP.Core.Transcoding.Profiles;

namespace WaveMP.Core.Transcoding.Arguments;

/// <summary>
/// The ffmpeg arguments of a transcode to HLS: MPEG-TS segments of the variant, starting at a segment. A transcode that
/// starts later (a seek) seeks in the input, numbers its segments from the start segment on and shifts its timestamps
/// to the position in the file, so its segments fit seamlessly into the playlist of the whole file. Key frames are
/// forced at every segment boundary. Pure functions.
/// </summary>
public static class FfmpegHlsArguments
{
    /// <summary>
    /// Name of the playlist ffmpeg writes into the output folder; its entries are the finished segments.
    /// </summary>
    public const string PlaylistName = "ffmpeg.m3u8";

    /// <summary>
    /// Prefix of the segment files in the output folder; followed by the five-digit index and <c>.ts</c>.
    /// </summary>
    public const string SegmentPrefix = "seg-";

    public static IReadOnlyList<string> Build(
        TranscodeVariant variant,
        int startSegment,
        HardwareAccelerator accelerator,
        string? device,
        string outputFolder,
        string x264Preset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startSegment);
        TranscodeProfile profile = variant.Profile;
        bool video = profile.Media == HlsMedia.Video;
        HardwareAccelerator encoder = video ? accelerator : HardwareAccelerator.None;
        TimeSpan start = variant.StartOf(startSegment);

        List<string> arguments = ["-hide_banner", "-nostdin", "-loglevel", "error", "-y"];
        if (video)
        {
            arguments.AddRange(FfmpegHardwareArguments.InitDevice(encoder, device));
        }

        if (start > TimeSpan.Zero)
        {
            arguments.AddRange(["-ss", Seconds(start)]);
        }

        arguments.AddRange(["-i", variant.Input.FullPath]);

        if (video)
        {
            arguments.AddRange(["-map", "0:v:0", "-map", "0:a:0?"]);
            arguments.AddRange(["-vf", VideoFilter(profile, encoder)]);
            arguments.AddRange(["-c:v", FfmpegHardwareArguments.EncoderOf(encoder)]);
            if (encoder == HardwareAccelerator.None)
            {
                arguments.AddRange(["-preset", x264Preset]);
            }

            int bitrate = profile.VideoBitrateKbps;
            arguments.AddRange(
            [
                "-profile:v", "high",
                "-b:v", Kbps(bitrate),
                "-maxrate", Kbps(bitrate * 3 / 2),
                "-bufsize", Kbps(bitrate * 2),
                "-force_key_frames", $"expr:gte(t,n_forced*{Seconds(profile.SegmentDuration)})",
            ]);
        }
        else
        {
            arguments.AddRange(["-map", "0:a:0", "-vn"]);
        }

        arguments.AddRange(
        [
            "-sn", "-dn",
            "-map_metadata", "-1",
            "-c:a", "aac",
            "-b:a", Kbps(profile.AudioBitrateKbps),
            "-ac", "2",
            "-ar", TranscodeProfile.SampleRate.ToString(CultureInfo.InvariantCulture),
            "-max_muxing_queue_size", "1024",
        ]);

        if (start > TimeSpan.Zero)
        {
            arguments.AddRange(["-output_ts_offset", Seconds(start)]);
        }

        arguments.AddRange(
        [
            "-f", "hls",
            "-hls_time", Seconds(profile.SegmentDuration),
            "-hls_list_size", "0",
            "-hls_segment_type", "mpegts",
            "-hls_flags", "temp_file+independent_segments",
            "-start_number", startSegment.ToString(CultureInfo.InvariantCulture),
            "-hls_segment_filename", Path.Combine(outputFolder, SegmentPrefix + "%05d.ts"),
            Path.Combine(outputFolder, PlaylistName),
        ]);
        return arguments;
    }

    /// <summary>
    /// Scales the picture to the size of the profile (or, with an unknown source size, down to the largest height)
    /// and hands it to the encoder.
    /// </summary>
    public static string VideoFilter(TranscodeProfile profile, HardwareAccelerator accelerator)
    {
        string scale = profile.Size is { } size
            ? string.Create(CultureInfo.InvariantCulture, $"scale={size.Width}:{size.Height}")
            : string.Create(CultureInfo.InvariantCulture, $"scale=-2:'trunc(min(ih,{profile.MaxHeight})/2)*2'");
        return scale + "," + FfmpegHardwareArguments.UploadFilter(accelerator);
    }

    /// <summary>
    /// Index of a segment file name ffmpeg wrote, e.g. <c>seg-00012.ts</c>; <c>null</c> for other names.
    /// </summary>
    public static int? SegmentIndexOf(string fileName)
    {
        string name = Path.GetFileName(fileName.Trim());
        if (!name.StartsWith(SegmentPrefix, StringComparison.Ordinal)
            || !name.EndsWith(".ts", StringComparison.Ordinal))
        {
            return null;
        }

        string digits = name[SegmentPrefix.Length..^3];
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int index) ? index : null;
    }

    private static string Seconds(TimeSpan value)
    {
        return value.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Kbps(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture) + "k";
    }
}
