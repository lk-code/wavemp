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

    public async Task<IReadOnlyList<Guid>> ListAlbumIdsWithCoversAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Albums.AsNoTracking()
            .Where(album => album.Covers.Any() || album.Tracks.Any(track => track.Covers.Any()))
            .OrderBy(album => album.Id)
            .Select(album => album.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListLooseTrackIdsWithCoversAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Tracks.AsNoTracking()
            .Where(track => track.AlbumId == null && track.Covers.Any())
            .OrderBy(track => track.Id)
            .Select(track => track.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CoverCandidateRow>> GetAlbumBatchCandidatesAsync(
        IReadOnlyCollection<Guid> albumIds,
        CancellationToken cancellationToken = default)
    {
        Guid[] ids = [.. albumIds];
        return await Project(dbContext.Covers.Where(cover =>
                (cover.AlbumId != null && ids.Contains(cover.AlbumId.Value))
                || (cover.Track != null && cover.Track.AlbumId != null && ids.Contains(cover.Track.AlbumId.Value))))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CoverCandidateRow>> GetTrackBatchCandidatesAsync(
        IReadOnlyCollection<Guid> trackIds,
        CancellationToken cancellationToken = default)
    {
        Guid[] ids = [.. trackIds];
        return await Project(dbContext.Covers.Where(cover =>
                cover.TrackId != null && ids.Contains(cover.TrackId.Value)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CoverSourceRow>> ListCoverSourcesAsync(
        CancellationToken cancellationToken = default)
    {
        var sources = await dbContext.Covers.AsNoTracking()
            .Select(cover => new
            {
                cover.MediaFileId,
                cover.Source,
                cover.MediaFile!.Size,
                cover.MediaFile.ModifiedAt,
            })
            .Distinct()
            .ToListAsync(cancellationToken);
        return [.. sources.Select(source =>
            new CoverSourceRow(source.MediaFileId, source.Source, source.Size, source.ModifiedAt))];
    }

    private static IQueryable<CoverCandidateRow> Project(IQueryable<Cover> covers)
    {
        return covers.AsNoTracking().Select(cover => new CoverCandidateRow(
            cover.Id,
            cover.MediaFileId,
            cover.AlbumId,
            cover.TrackId,
            cover.Track != null ? cover.Track.AlbumId : null,
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
