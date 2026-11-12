using System.Globalization;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Streaming.Decisions;
using WaveMP.Core.Streaming.Formats;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Streaming.Sources;

internal sealed class StreamService(ITrackRepository tracks, IMediaFileLocator locator) : IStreamService
{
    public async Task<StreamSourceResult> GetAudioSourceAsync(
        Guid trackId,
        CancellationToken cancellationToken = default)
    {
        TrackFileRow? row = await tracks.FindPrimaryFileAsync(trackId, MediaFileKind.Audio, cancellationToken);
        if (row is null)
        {
            return StreamSourceResult.Of(StreamSourceStatus.TrackNotFound);
        }

        return row.File is { } file ? FromAudioFile(trackId, file) : StreamSourceResult.Of(StreamSourceStatus.NoAudio);
    }

    public async Task<StreamSourceResult> GetSoundSourceAsync(
        Guid trackId,
        CancellationToken cancellationToken = default)
    {
        StreamSourceResult audio = await GetAudioSourceAsync(trackId, cancellationToken);
        if (audio.Status != StreamSourceStatus.NoAudio)
        {
            return audio;
        }

        VideoSourceResult video = await GetVideoSourceAsync(trackId, cancellationToken);
        return video.Status switch
        {
            StreamSourceStatus.Found => StreamSourceResult.Found(FromVideo(video.Source!)),
            StreamSourceStatus.NoVideo => StreamSourceResult.Of(StreamSourceStatus.NoAudio),
            _ => StreamSourceResult.Of(video.Status),
        };
    }

    public async Task<PlaybackResult> DecideAsync(
        Guid trackId,
        IReadOnlySet<AudioFormat> supported,
        CancellationToken cancellationToken = default)
    {
        StreamSourceResult result = await GetSoundSourceAsync(trackId, cancellationToken);
        if (!result.IsFound)
        {
            return new PlaybackResult(result.Status, null, null);
        }

        PlaybackDecision decision = result.Source.Origin == MediaFileKind.Video
            ? new PlaybackDecision(PlaybackMethod.Transcode, null, TranscodeReason.AudioFromVideo)
            : PlaybackDecider.Decide(result.Source.Format, supported);
        return new PlaybackResult(result.Status, result.Source, decision);
    }

    public async Task<VideoSourceResult> GetVideoSourceAsync(
        Guid trackId,
        CancellationToken cancellationToken = default)
    {
        TrackFileRow? row = await tracks.FindPrimaryFileAsync(trackId, MediaFileKind.Video, cancellationToken);
        if (row is null)
        {
            return VideoSourceResult.Of(StreamSourceStatus.TrackNotFound);
        }

        if (row.File is not { } file)
        {
            return VideoSourceResult.Of(StreamSourceStatus.NoVideo);
        }

        if (!TryGetFile(file, out string fullPath, out FileInfo? info))
        {
            return VideoSourceResult.Of(StreamSourceStatus.FileMissing);
        }

        VideoContainer? container = VideoFormats.ContainerOf(file.Format, file.Extension);
        DateTimeOffset modified = new(info.LastWriteTimeUtc, TimeSpan.Zero);
        return VideoSourceResult.Found(new VideoSource(
            trackId,
            file.Id,
            fullPath,
            container,
            VideoFormats.VideoCodecOf(file.Codec),
            VideoFormats.AudioCodecOf(file.AudioCodec),
            file.AudioCodec is not null,
            file.PixelFormat,
            file.Width,
            file.Height,
            file.SampleRate,
            VideoFormats.ContentTypeOf(container),
            info.Length,
            TruncateToSeconds(modified),
            CreateETag(file.Id, info.Length, modified),
            file.Duration,
            file.MetadataVersion >= VideoFormats.MetadataVersion));
    }

    public async Task<VideoPlaybackResult> DecideVideoAsync(
        Guid trackId,
        VideoCapabilities capabilities,
        CancellationToken cancellationToken = default)
    {
        VideoSourceResult result = await GetVideoSourceAsync(trackId, cancellationToken);
        return result.IsFound
            ? new VideoPlaybackResult(
                result.Status,
                result.Source,
                VideoPlaybackDecider.Decide(result.Source, capabilities))
            : new VideoPlaybackResult(result.Status, null, null);
    }

    private StreamSourceResult FromAudioFile(Guid trackId, MediaFile file)
    {
        if (!TryGetFile(file, out string fullPath, out FileInfo? info))
        {
            return StreamSourceResult.Of(StreamSourceStatus.FileMissing);
        }

        AudioFormat? format = AudioFormats.FromFile(file.Format, file.Codec, file.Extension);
        DateTimeOffset modified = new(info.LastWriteTimeUtc, TimeSpan.Zero);
        return StreamSourceResult.Found(new StreamSource(
            trackId,
            file.Id,
            fullPath,
            format,
            format is { } known ? AudioFormats.ContentTypeOf(known) : AudioFormats.UnknownContentType,
            info.Length,
            TruncateToSeconds(modified),
            CreateETag(file.Id, info.Length, modified),
            file.Duration,
            ReplayGainValues.From(file.Tags),
            MediaFileKind.Audio,
            file.SampleRate));
    }

    /// <summary>
    /// The sound of a video: never played directly, without ReplayGain (tags are only read from audio files).
    /// </summary>
    private static StreamSource FromVideo(VideoSource video)
    {
        return new StreamSource(
            video.TrackId,
            video.FileId,
            video.FullPath,
            null,
            video.ContentType,
            video.Length,
            video.LastModified,
            video.ETag,
            video.Duration,
            ReplayGainValues.None,
            MediaFileKind.Video,
            video.SampleRate);
    }

    private bool TryGetFile(
        MediaFile file,
        out string fullPath,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out FileInfo? info)
    {
        info = null;
        if (!locator.TryGetFullPath(file.RelativePath, out fullPath))
        {
            return false;
        }

        FileInfo candidate = new(fullPath);
        if (!candidate.Exists)
        {
            return false;
        }

        info = candidate;
        return true;
    }

    /// <summary>
    /// A new version of the file (other size or time of the last change) or another file gets another tag.
    /// </summary>
    private static string CreateETag(Guid fileId, long length, DateTimeOffset modified)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{fileId:N}-{length:x}-{modified.UtcTicks:x}");
    }

    private static DateTimeOffset TruncateToSeconds(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond), value.Offset);
    }
}
