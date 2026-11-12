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

    /// <summary>
    /// The album with the favorite of <paramref name="userId"/>.
    /// </summary>
    Task<AlbumRow?> FindAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// The albums with these ids, in no particular order, with the favorites of <paramref name="userId"/>; ids of
    /// missing albums are left out.
    /// </summary>
    Task<IReadOnlyList<AlbumRow>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
