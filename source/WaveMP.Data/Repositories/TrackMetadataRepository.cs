using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

internal sealed class TrackMetadataRepository(WaveMpDbContext dbContext) : ITrackMetadataRepository
{
    public async Task<TrackEditTarget?> FindEditTargetAsync(
        Guid trackId,
        CancellationToken cancellationToken = default)
    {
        string? directoryPath = await dbContext.Tracks.AsNoTracking()
            .Where(track => track.Id == trackId)
            .Select(track => track.DirectoryPath)
            .FirstOrDefaultAsync(cancellationToken);
        if (directoryPath is null)
        {
            return null;
        }

        MediaFile? audioFile = await dbContext.MediaFiles.AsNoTracking()
            .FirstOrDefaultAsync(
                file => file.TrackId == trackId && file.Kind == MediaFileKind.Audio && file.IsPrimary,
                cancellationToken);
        return new TrackEditTarget(trackId, directoryPath, audioFile);
    }

    public async Task AddTagBackupAsync(TagBackup backup, CancellationToken cancellationToken = default)
    {
        dbContext.TagBackups.Add(backup);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            dbContext.Entry(backup).State = EntityState.Detached;
        }
    }

    public Task SetWritableAsync(Guid mediaFileId, bool isWritable, CancellationToken cancellationToken = default)
    {
        return dbContext.MediaFiles
            .Where(file => file.Id == mediaFileId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(file => file.IsWritable, isWritable), cancellationToken);
    }

    public Task<int> RenameArtistAsync(
        string normalizedName,
        string name,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Artists
            .Where(artist => artist.NormalizedName == normalizedName && artist.Name != name)
            .ExecuteUpdateAsync(setters => setters.SetProperty(artist => artist.Name, name), cancellationToken);
    }
}
