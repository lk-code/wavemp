using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Reads the albums of the library. Nothing is tracked.
/// </summary>
public interface IAlbumRepository
{
    /// <summary>
    /// Lists the albums sorted and filtered as requested; ties are sorted by id.
    /// </summary>
    Task<Page<AlbumRow>> ListAsync(AlbumListQuery query, CancellationToken cancellationToken = default);

    Task<AlbumRow?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
