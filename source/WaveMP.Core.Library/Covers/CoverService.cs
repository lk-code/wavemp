using System.Runtime.CompilerServices;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Covers;

internal sealed class CoverService(
    ICoverRepository covers,
    ISettingsSnapshot<LibrarySettings> settings) : ICoverService
{
    internal const int AlbumBatchSize = 200;
    internal const int TrackBatchSize = 500;

    public async Task<CoverCandidates> GetTrackCandidatesAsync(
        Guid trackId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CoverCandidateRow>? candidates = await covers.GetTrackCandidatesAsync(trackId, cancellationToken);
        return candidates is null
            ? CoverCandidates.OwnerNotFound
            : new CoverCandidates(true, CoverChoices.ForTrack(trackId, Priority(), candidates));
    }

    public async Task<CoverCandidates> GetAlbumCandidatesAsync(
        Guid albumId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CoverCandidateRow>? candidates = await covers.GetAlbumCandidatesAsync(albumId, cancellationToken);
        return candidates is null
            ? CoverCandidates.OwnerNotFound
            : new CoverCandidates(true, CoverResolver.OrderForAlbum(Priority(), candidates));
    }

    public async IAsyncEnumerable<IReadOnlyList<CoverCandidateRow>> EnumerateShownCoversAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CoverPriority priority = Priority();
        HashSet<string> seen = new(StringComparer.Ordinal);

        IReadOnlyList<Guid> albumIds = await covers.ListAlbumIdsWithCoversAsync(cancellationToken);
        foreach (Guid[] batch in albumIds.Chunk(AlbumBatchSize))
        {
            IReadOnlyList<CoverCandidateRow> candidates =
                await covers.GetAlbumBatchCandidatesAsync(batch, cancellationToken);
            foreach (IGrouping<Guid?, CoverCandidateRow> album in candidates
                .GroupBy(candidate => candidate.AlbumId ?? candidate.TrackAlbumId)
                .OrderBy(album => album.Key))
            {
                foreach (IReadOnlyList<CoverCandidateRow> list in CoverChoices.ForAlbum(priority, album))
                {
                    if (seen.Add(CoverChoices.KeyOf(list)))
                    {
                        yield return list;
                    }
                }
            }
        }

        IReadOnlyList<Guid> trackIds = await covers.ListLooseTrackIdsWithCoversAsync(cancellationToken);
        foreach (Guid[] batch in trackIds.Chunk(TrackBatchSize))
        {
            IReadOnlyList<CoverCandidateRow> candidates =
                await covers.GetTrackBatchCandidatesAsync(batch, cancellationToken);
            foreach (IGrouping<Guid?, CoverCandidateRow> track in candidates
                .GroupBy(candidate => candidate.TrackId)
                .OrderBy(track => track.Key))
            {
                IReadOnlyList<CoverCandidateRow> list =
                    CoverChoices.ForTrack(track.Key ?? Guid.Empty, priority, track);
                if (list.Count > 0 && seen.Add(CoverChoices.KeyOf(list)))
                {
                    yield return list;
                }
            }
        }
    }

    /// <summary>
    /// Albums and tracks have no rule of their own yet; the global one applies.
    /// </summary>
    private CoverPriority Priority()
    {
        return CoverRules.Effective(settings.Current.CoverPriority, album: null, track: null);
    }
}
