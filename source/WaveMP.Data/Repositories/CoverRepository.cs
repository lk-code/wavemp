using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;

namespace WaveMP.Data.Repositories;

internal sealed class CoverRepository(WaveMpDbContext dbContext) : ICoverRepository
{
    public async Task<IReadOnlyList<CoverCandidateRow>?> GetTrackCandidatesAsync(
        Guid trackId,
        CancellationToken cancellationToken = default)
    {
        var track = await dbContext.Tracks.AsNoTracking()
            .Where(track => track.Id == trackId)
            .Select(track => new { track.AlbumId })
            .FirstOrDefaultAsync(cancellationToken);
        if (track is null)
        {
            return null;
        }

        IQueryable<Cover> covers = track.AlbumId is { } albumId
            ? dbContext.Covers.Where(cover =>
                cover.TrackId == trackId
                || cover.AlbumId == albumId
                || (cover.Track != null && cover.Track.AlbumId == albumId))
            : dbContext.Covers.Where(cover => cover.TrackId == trackId);
        return await Project(covers).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CoverCandidateRow>?> GetAlbumCandidatesAsync(
        Guid albumId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Albums.AnyAsync(album => album.Id == albumId, cancellationToken))
        {
            return null;
        }

        return await Project(dbContext.Covers.Where(cover =>
                cover.AlbumId == albumId || (cover.Track != null && cover.Track.AlbumId == albumId)))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<CoverCandidateRow> Project(IQueryable<Cover> covers)
    {
        return covers.AsNoTracking().Select(cover => new CoverCandidateRow(
            cover.Id,
            cover.AlbumId,
            cover.TrackId,
            cover.Track != null ? cover.Track.DiscNumber : null,
            cover.Track != null ? cover.Track.TrackNumber : null,
            cover.Track != null ? cover.Track.Title : null,
            cover.IsPrimary,
            cover.Source,
            cover.MediaFile!.RelativePath,
            cover.MimeType,
            cover.MediaFile.Size,
            cover.MediaFile.ModifiedAt));
    }
}
