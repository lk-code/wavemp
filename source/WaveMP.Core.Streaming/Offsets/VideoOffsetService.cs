using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Streaming.Offsets;

internal sealed class VideoOffsetService(ITrackRepository tracks) : IVideoOffsetService
{
    public async Task<VideoOffsetResult> GetAsync(Guid trackId, CancellationToken cancellationToken = default)
    {
        // The file on the disk is not checked: the admin sees the offset also while the video is missing for a moment.
        TrackFileRow? row = await tracks.FindPrimaryFileAsync(trackId, MediaFileKind.Video, cancellationToken);
        if (row is null)
        {
            return new VideoOffsetResult(VideoOffsetStatus.TrackNotFound, trackId);
        }

        return row.File is null
            ? new VideoOffsetResult(VideoOffsetStatus.NoVideo, trackId)
            : new VideoOffsetResult(VideoOffsetStatus.Found, trackId, row.VideoOffsetMilliseconds);
    }

    public async Task<VideoOffsetResult> SetAsync(
        Guid trackId,
        int milliseconds,
        CancellationToken cancellationToken = default)
    {
        if (!VideoOffsets.IsValid(milliseconds))
        {
            return new VideoOffsetResult(VideoOffsetStatus.Invalid, trackId);
        }

        VideoOffsetResult current = await GetAsync(trackId, cancellationToken);
        if (!current.IsFound)
        {
            return current;
        }

        // A scan may have removed the track in the meantime.
        return await tracks.SetVideoOffsetAsync(trackId, milliseconds, cancellationToken)
            ? new VideoOffsetResult(VideoOffsetStatus.Found, trackId, milliseconds)
            : new VideoOffsetResult(VideoOffsetStatus.TrackNotFound, trackId);
    }
}
