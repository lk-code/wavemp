using WaveMP.Data.Entities;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Library.Covers;

/// <summary>
/// Orders the covers that may be shown for a track or an album, the best first. The rule decides between the cover of
/// the album (a picture in its folder) and the cover of the track; the other one is the fallback, the covers of the
/// other tracks of the album come last. Within a group the covers of the first track (disc and track number) come
/// first, then the primary cover, picture files before embedded pictures.
/// </summary>
public static class CoverResolver
{
    public static IReadOnlyList<CoverCandidateRow> OrderForTrack(
        Guid trackId,
        CoverPriority priority,
        IEnumerable<CoverCandidateRow> candidates)
    {
        return Order(candidates, candidate =>
        {
            if (candidate.AlbumId is not null)
            {
                return priority == CoverPriority.AlbumFirst ? 0 : 1;
            }

            if (candidate.TrackId == trackId)
            {
                return priority == CoverPriority.AlbumFirst ? 1 : 0;
            }

            return 2;
        });
    }

    /// <summary>
    /// For an album the rule decides between its own cover and the covers of its tracks.
    /// </summary>
    public static IReadOnlyList<CoverCandidateRow> OrderForAlbum(
        CoverPriority priority,
        IEnumerable<CoverCandidateRow> candidates)
    {
        return Order(candidates, candidate =>
        {
            bool albumCover = candidate.AlbumId is not null;
            return albumCover == (priority == CoverPriority.AlbumFirst) ? 0 : 1;
        });
    }

    private static CoverCandidateRow[] Order(
        IEnumerable<CoverCandidateRow> candidates,
        Func<CoverCandidateRow, int> group)
    {
        return [.. candidates
            .OrderBy(group)
            .ThenBy(candidate => candidate.DiscNumber ?? int.MaxValue)
            .ThenBy(candidate => candidate.TrackNumber ?? int.MaxValue)
            .ThenBy(candidate => candidate.TrackTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.TrackId)
            .ThenByDescending(candidate => candidate.IsPrimary)
            .ThenBy(candidate => candidate.Source == CoverSource.File ? 0 : 1)
            .ThenBy(candidate => candidate.CoverId)];
    }
}
