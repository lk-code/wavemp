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

    Task<ArtistRow?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
