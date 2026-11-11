using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

internal sealed class LibraryScanRepository(WaveMpDbContext dbContext) : ILibraryScanRepository
{
    public async Task<IReadOnlyList<MediaFileSnapshot>> GetFileSnapshotsAsync(
        IReadOnlyCollection<string>? directories,
        CancellationToken cancellationToken = default)
    {
        IQueryable<MediaFile> files = dbContext.MediaFiles.AsNoTracking();
        if (directories is null || directories.Contains(string.Empty))
        {
            return await ToSnapshots(files).ToListAsync(cancellationToken);
        }

        Dictionary<Guid, MediaFileSnapshot> snapshots = [];
        foreach (string directory in directories)
        {
            string prefix = directory + "/";
            List<MediaFileSnapshot> inDirectory = await ToSnapshots(files.Where(file =>
                    file.DirectoryPath == directory || file.DirectoryPath.StartsWith(prefix)))
                .ToListAsync(cancellationToken);
            foreach (MediaFileSnapshot snapshot in inDirectory)
            {
                snapshots[snapshot.Id] = snapshot;
            }
        }

        return [.. snapshots.Values];
    }

    public async Task<IReadOnlyList<MediaFileSnapshot>> GetFolderSnapshotsAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        return await ToSnapshots(dbContext.MediaFiles.AsNoTracking()
                .Where(file => file.DirectoryPath == directoryPath))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountFilesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.MediaFiles.CountAsync(cancellationToken);
    }

    public async Task<StoredFolder> LoadFolderAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        List<MediaFile> files = await dbContext.MediaFiles
            .Where(file => file.DirectoryPath == directoryPath)
            .ToListAsync(cancellationToken);
        List<Track> tracks = await dbContext.Tracks
            .Include(track => track.Covers)
            .Where(track => track.DirectoryPath == directoryPath)
            .ToListAsync(cancellationToken);
        return new StoredFolder(files, tracks);
    }

    public Task<Artist?> FindArtistAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        return dbContext.Artists.FirstOrDefaultAsync(
            artist => artist.NormalizedName == normalizedName,
            cancellationToken);
    }

    public Task<Album?> FindAlbumAsync(string albumKey, CancellationToken cancellationToken = default)
    {
        return dbContext.Albums.FirstOrDefaultAsync(album => album.AlbumKey == albumKey, cancellationToken);
    }

    public void Add<TEntity>(TEntity entity)
        where TEntity : class
    {
        dbContext.Add(entity);
    }

    public void Remove<TEntity>(TEntity entity)
        where TEntity : class
    {
        dbContext.Remove(entity);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    public async Task<(int Files, int Tracks)> DeleteDirectoriesAsync(
        IReadOnlyCollection<string> directories,
        CancellationToken cancellationToken = default)
    {
        if (directories.Count == 0)
        {
            return (0, 0);
        }

        string[] paths = [.. directories];
        int files = await dbContext.MediaFiles
            .Where(file => paths.Contains(file.DirectoryPath))
            .ExecuteDeleteAsync(cancellationToken);
        int tracks = await dbContext.Tracks
            .Where(track => paths.Contains(track.DirectoryPath))
            .ExecuteDeleteAsync(cancellationToken);
        return (files, tracks);
    }

    public async Task<IReadOnlyList<AlbumFolder>> GetAlbumFoldersAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Tracks.AsNoTracking()
            .Where(track => track.AlbumId != null)
            .Select(track => new AlbumFolder(track.AlbumId!.Value, track.DirectoryPath))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlbumCoverImage>> GetAlbumCoverImagesAsync(
        IReadOnlyCollection<string> normalizedBaseNames,
        CancellationToken cancellationToken = default)
    {
        string[] names = [.. normalizedBaseNames];
        return await dbContext.MediaFiles.AsNoTracking()
            .Where(file => file.Kind == MediaFileKind.Image && names.Contains(file.NormalizedBaseName))
            .Select(file => new AlbumCoverImage(
                file.Id,
                file.RelativePath,
                file.DirectoryPath,
                file.NormalizedBaseName,
                file.Extension,
                file.Size,
                file.Width,
                file.Height))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cover>> LoadAlbumCoversAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Covers
            .Where(cover => cover.AlbumId != null)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int Albums, int Artists)> DeleteOrphansAsync(CancellationToken cancellationToken = default)
    {
        int albums = await dbContext.Albums
            .Where(album => !dbContext.Tracks.Any(track => track.AlbumId == album.Id))
            .ExecuteDeleteAsync(cancellationToken);
        int artists = await dbContext.Artists
            .Where(artist => !dbContext.Tracks.Any(track => track.ArtistId == artist.Id)
                && !dbContext.Albums.Any(album => album.AlbumArtistId == artist.Id))
            .ExecuteDeleteAsync(cancellationToken);
        return (albums, artists);
    }

    public async Task<LibraryTotals> CountLibraryAsync(CancellationToken cancellationToken = default)
    {
        return new LibraryTotals(
            await dbContext.Tracks.CountAsync(cancellationToken),
            await dbContext.Albums.CountAsync(cancellationToken),
            await dbContext.Artists.CountAsync(cancellationToken));
    }

    private static IQueryable<MediaFileSnapshot> ToSnapshots(IQueryable<MediaFile> files)
    {
        return files.Select(file => new MediaFileSnapshot(
            file.Id,
            file.RelativePath,
            file.DirectoryPath,
            file.Kind,
            file.Size,
            file.ModifiedAt,
            file.MetadataVersion));
    }
}
