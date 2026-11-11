using System.Globalization;
using System.Text;
using WaveMP.Core.Library.Covers;

namespace WaveMP.Core.Caching.Images;

/// <summary>
/// The arguments of ffmpeg that scale a picture read from standard input to WebP pictures: the input is split once
/// per target, optionally cropped to a square around its center, and scaled to fit into a square of the target size,
/// keeping the aspect ratio and never enlarging it. Metadata (e.g. EXIF) is not copied.
/// </summary>
internal static class FfmpegCoverArguments
{
    private const string SquareCrop = "crop='min(iw,ih)':'min(iw,ih)',";

    /// <summary>
    /// The demuxer of ffmpeg for a picture format, so that the content decides and never a file extension;
    /// <c>null</c> for other formats.
    /// </summary>
    public static string? DemuxerOf(string contentType)
    {
        return contentType switch
        {
            ImageFormats.Jpeg => "jpeg_pipe",
            ImageFormats.Png => "png_pipe",
            ImageFormats.WebP => "webp_pipe",
            ImageFormats.Gif => "gif",
            ImageFormats.Bmp => "bmp_pipe",
            _ => null,
        };
    }

    public static IReadOnlyList<string> Build(string demuxer, IReadOnlyList<CoverEncodeTarget> targets, int quality)
    {
        ArgumentOutOfRangeException.ThrowIfZero(targets.Count);

        // The quotes belong to the filter syntax of ffmpeg: min(iw,96) contains a comma.
        StringBuilder filter = new($"[0:v]split={targets.Count.ToString(CultureInfo.InvariantCulture)}");
        for (int index = 0; index < targets.Count; index++)
        {
            filter.Append(CultureInfo.InvariantCulture, $"[s{index}]");
        }

        for (int index = 0; index < targets.Count; index++)
        {
            int size = targets[index].MaxSize;
            filter.Append(CultureInfo.InvariantCulture, $";[s{index}]");
            if (targets[index].Square)
            {
                // Without a position, crop keeps the center.
                filter.Append(SquareCrop);
            }

            filter.Append(CultureInfo.InvariantCulture, $"scale='min(iw,{size})':'min(ih,{size})'");
            filter.Append(
                CultureInfo.InvariantCulture,
                $":force_original_aspect_ratio=decrease:flags=lanczos[o{index}]");
        }

        List<string> arguments =
        [
            "-hide_banner", "-loglevel", "error", "-nostdin", "-y",
            "-f", demuxer, "-i", "pipe:0",
            "-filter_complex", filter.ToString(),
        ];
        for (int index = 0; index < targets.Count; index++)
        {
            arguments.AddRange(
            [
                "-map", $"[o{index.ToString(CultureInfo.InvariantCulture)}]",
                "-frames:v", "1",
                "-map_metadata", "-1",
                "-c:v", "libwebp",
                "-quality", quality.ToString(CultureInfo.InvariantCulture),
                "-f", "webp",
                targets[index].OutputPath,
            ]);
        }

        return arguments;
    }
}
