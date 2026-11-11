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

        if (row.File is not { } file)
        {
            return StreamSourceResult.Of(StreamSourceStatus.NoAudio);
        }

        if (!locator.TryGetFullPath(file.RelativePath, out string fullPath))
        {
            return StreamSourceResult.Of(StreamSourceStatus.FileMissing);
        }

        FileInfo info = new(fullPath);
        if (!info.Exists)
        {
            return StreamSourceResult.Of(StreamSourceStatus.FileMissing);
        }

        AudioFormat? format = AudioFormats.FromFile(file.Format, file.Codec, file.Extension);
        DateTimeOffset modified = new(info.LastWriteTimeUtc, TimeSpan.Zero);
        return StreamSourceResult.Found(new StreamSource(
            trackId,
            fullPath,
            format,
            format is { } known ? AudioFormats.ContentTypeOf(known) : AudioFormats.UnknownContentType,
            info.Length,
            TruncateToSeconds(modified),
            CreateETag(file.Id, info.Length, modified),
            file.Duration,
            ReplayGainValues.From(file.Tags)));
    }

    public async Task<PlaybackResult> DecideAsync(
        Guid trackId,
        IReadOnlySet<AudioFormat> supported,
        CancellationToken cancellationToken = default)
    {
        StreamSourceResult result = await GetAudioSourceAsync(trackId, cancellationToken);
        return result.IsFound
            ? new PlaybackResult(result.Status, result.Source, PlaybackDecider.Decide(result.Source.Format, supported))
            : new PlaybackResult(result.Status, null, null);
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
