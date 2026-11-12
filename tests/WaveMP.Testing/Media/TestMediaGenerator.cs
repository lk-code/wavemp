using System.Collections.Concurrent;
using System.Globalization;

namespace WaveMP.Testing.Media;

/// <summary>
/// Creates synthetic test media with ffmpeg at test runtime: a sine tone (one second by default), a test picture and tags
/// written with <c>-metadata</c>, as CLAUDE.md asks. Encoded files are cached per process and only remuxed
/// (<c>-c copy</c>) for other tags, which keeps the tests fast.
/// </summary>
public static class TestMediaGenerator
{
    private static readonly ConcurrentDictionary<string, Lazy<Task<string>>> _encoded = new();
    private static readonly Lazy<string> _cacheDirectory = new(CreateCacheDirectory);

    /// <summary>
    /// Creates an audio file. The format comes from <paramref name="format"/>, independent of the file extension.
    /// </summary>
    /// <param name="path">Target file; missing folders are created.</param>
    /// <param name="format">Codec and container.</param>
    /// <param name="tags">Tags to write; <c>null</c> for a file without tags.</param>
    /// <param name="bitrateKbps">Bitrate of lossy formats; 128 if not set.</param>
    /// <param name="sampleRate">Sample rate; 44100 (Opus: 48000) if not set.</param>
    /// <param name="bitDepth">16 or 24 for FLAC and WAV; 16 if not set.</param>
    /// <param name="embeddedCoverPath">Picture embedded as front cover (MP3, FLAC and M4A).</param>
    /// <param name="durationSeconds">Length of the tone in seconds; 1 if not set.</param>
    public static async Task CreateAudioAsync(
        string path,
        AudioFormat format,
        TestTags? tags = null,
        int? bitrateKbps = null,
        int? sampleRate = null,
        int? bitDepth = null,
        string? embeddedCoverPath = null,
        int durationSeconds = 1)
    {
        int rate = sampleRate ?? (format == AudioFormat.Opus ? 48000 : 44100);
        int bitrate = bitrateKbps ?? 128;
        int depth = bitDepth ?? 16;
        string extension = format.ToString().ToLowerInvariant();
        string key = string.Create(
            CultureInfo.InvariantCulture,
            $"audio-{extension}-{bitrate}-{rate}-{depth}-{durationSeconds}");

        string encoded = await EncodeOnceAsync(key, extension, output =>
        [
            "-f", "lavfi", "-i", string.Create(
                CultureInfo.InvariantCulture,
                $"sine=frequency=440:duration={durationSeconds}:sample_rate={rate}"),
            "-ac", "2",
            .. AudioCodecArguments(format, bitrate, depth),
            output,
        ]);

        List<string> arguments = ["-i", encoded];
        if (embeddedCoverPath is not null)
        {
            arguments.AddRange(["-i", embeddedCoverPath, "-map", "0:a", "-map", "1:v", "-c", "copy"]);
            arguments.AddRange(["-disposition:v:0", "attached_pic"]);
        }
        else
        {
            arguments.AddRange(["-map", "0", "-c", "copy"]);
        }

        if (format == AudioFormat.Mp3)
        {
            arguments.AddRange(["-id3v2_version", "3"]);
        }

        arguments.AddRange(tags?.ToArguments() ?? []);
        arguments.AddRange(["-f", MuxerOf(format), Prepare(path)]);
        await FfmpegTool.RunAsync([.. arguments]);
    }

    /// <summary>
    /// Creates a music video with the given resolution: by default MPEG-4 part 2 video with AAC audio, which browsers
    /// cannot play directly.
    /// </summary>
    /// <param name="path">Target file; missing folders are created.</param>
    /// <param name="format">Container.</param>
    /// <param name="width">Width of the picture.</param>
    /// <param name="height">Height of the picture.</param>
    /// <param name="tags">Tags to write; <c>null</c> for a file without tags.</param>
    /// <param name="codec">Codecs of the video and its sound.</param>
    /// <param name="withAudio">Whether the video has sound.</param>
    /// <param name="durationSeconds">Length in seconds; 1 if not set.</param>
    public static async Task CreateVideoAsync(
        string path,
        VideoFormat format,
        int width,
        int height,
        TestTags? tags = null,
        TestVideoCodec codec = TestVideoCodec.Mpeg4,
        bool withAudio = true,
        int durationSeconds = 1)
    {
        string extension = format.ToString().ToLowerInvariant();
        string size = string.Create(CultureInfo.InvariantCulture, $"{width}x{height}");
        string duration = durationSeconds.ToString(CultureInfo.InvariantCulture);
        string key = $"video-{extension}-{size}-{codec}-{(withAudio ? "a" : "na")}-{duration}".ToLowerInvariant();
        string encoded = await EncodeOnceAsync(key, extension, output =>
        [
            "-f", "lavfi", "-i", $"testsrc=size={size}:rate=10:duration={duration}",
            .. withAudio ? (string[])["-f", "lavfi", "-i", $"sine=frequency=440:duration={duration}"] : [],
            .. VideoCodecArguments(codec),
            .. withAudio ? AudioOfVideoArguments(codec) : ["-an"],
            .. withAudio ? (string[])["-shortest"] : [],
            "-f", MuxerOf(format),
            output,
        ]);

        await FfmpegTool.RunAsync(
        [
            "-i", encoded, "-map", "0", "-c", "copy",
            .. tags?.ToArguments() ?? [],
            "-f", MuxerOf(format),
            Prepare(path),
        ]);
    }

    /// <summary>
    /// Creates a picture; JPEG or PNG after the extension of <paramref name="path"/>.
    /// </summary>
    public static async Task CreateImageAsync(string path, int width, int height)
    {
        string extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        string size = string.Create(CultureInfo.InvariantCulture, $"{width}x{height}");
        string encoded = await EncodeOnceAsync($"image-{extension}-{size}", extension, output =>
        [
            "-f", "lavfi", "-i", $"testsrc=size={size}:rate=1",
            "-frames:v", "1", "-update", "1",
            output,
        ]);

        File.Copy(encoded, Prepare(path), overwrite: true);
    }

    /// <summary>
    /// Creates a file of random bytes that no tool can read, e.g. a broken <c>.mp3</c>.
    /// </summary>
    public static void CreateCorruptFile(string path, int length = 4096)
    {
        byte[] bytes = new byte[length];
        new Random(length).NextBytes(bytes);
        File.WriteAllBytes(Prepare(path), bytes);
    }

    private static IEnumerable<string> AudioCodecArguments(AudioFormat format, int bitrate, int depth)
    {
        string bitrateArgument = string.Create(CultureInfo.InvariantCulture, $"{bitrate}k");
        return format switch
        {
            AudioFormat.Mp3 => ["-c:a", "libmp3lame", "-b:a", bitrateArgument],
            AudioFormat.M4a => ["-c:a", "aac", "-b:a", bitrateArgument],
            AudioFormat.Opus => ["-c:a", "libopus", "-b:a", bitrateArgument],
            AudioFormat.Flac => ["-c:a", "flac", "-sample_fmt", depth > 16 ? "s32" : "s16"],
            AudioFormat.Wav => ["-c:a", depth > 16 ? "pcm_s24le" : "pcm_s16le"],
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown audio format."),
        };
    }

    private static string[] VideoCodecArguments(TestVideoCodec codec)
    {
        return codec switch
        {
            TestVideoCodec.Mpeg4 => ["-c:v", "mpeg4", "-q:v", "5", "-pix_fmt", "yuv420p"],
            TestVideoCodec.H264 => ["-c:v", "libx264", "-preset", "ultrafast", "-pix_fmt", "yuv420p"],
            TestVideoCodec.Vp9 => ["-c:v", "libvpx-vp9", "-deadline", "realtime", "-b:v", "200k", "-pix_fmt", "yuv420p"],
            _ => throw new ArgumentOutOfRangeException(nameof(codec), codec, "Unknown video codec."),
        };
    }

    private static string[] AudioOfVideoArguments(TestVideoCodec codec)
    {
        return codec == TestVideoCodec.Vp9
            ? ["-c:a", "libopus", "-b:a", "64k"]
            : ["-c:a", "aac", "-b:a", "64k"];
    }

    private static string MuxerOf(VideoFormat format)
    {
        return format switch
        {
            VideoFormat.Mp4 => "mp4",
            VideoFormat.Mkv => "matroska",
            VideoFormat.Webm => "webm",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown video format."),
        };
    }

    private static string MuxerOf(AudioFormat format)
    {
        return format switch
        {
            AudioFormat.Mp3 => "mp3",
            AudioFormat.Flac => "flac",
            AudioFormat.M4a => "ipod",
            AudioFormat.Opus => "ogg",
            AudioFormat.Wav => "wav",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown audio format."),
        };
    }

    private static async Task<string> EncodeOnceAsync(
        string key,
        string extension,
        Func<string, IEnumerable<string>> arguments)
    {
        Lazy<Task<string>> encoding = _encoded.GetOrAdd(key, _ => new Lazy<Task<string>>(async () =>
        {
            string output = Path.Combine(_cacheDirectory.Value, $"{key}.{extension}");
            await FfmpegTool.RunAsync([.. arguments(output)]);
            return output;
        }));

        try
        {
            return await encoding.Value;
        }
        catch
        {
            // A failed encoding is tried again by the next test instead of failing all of them.
            _encoded.TryRemove(key, out _);
            throw;
        }
    }

    private static string Prepare(string path)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        return fullPath;
    }

    private static string CreateCacheDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "wavemp-tests", "media-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        };
        return directory;
    }
}
