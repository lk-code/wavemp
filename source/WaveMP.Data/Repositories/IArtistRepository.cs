using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Reads the artists of the library. Nothing is tracked.
/// </summary>
public interface IArtistRepository
{
    /// <summary>
    /// Lists the artists sorted and filtered as requested; ties are sorted by id.
    /// </summary>
    Task<Page<ArtistRow>> ListAsync(ArtistListQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// The artist with the favorite of <paramref name="userId"/>.
    /// </summary>
    Task<ArtistRow?> FindAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// The artists with these ids, in no particular order, with the favorites of <paramref name="userId"/>; ids of
    /// missing artists are left out.
    /// </summary>
    Task<IReadOnlyList<ArtistRow>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
