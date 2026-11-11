using WaveMP.Data.Entities;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Library.Covers;

/// <summary>
/// The ordered lists of covers that the library shows, e.g. to prepare them in advance. For an album these are the list
/// of the album, the list of every track with covers of its own and the list of its tracks without a cover of their
/// own; identical lists come only once.
/// </summary>
public static class CoverChoices
{
    /// <summary>
    /// The lists of an album from the covers of the album and of its tracks.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<CoverCandidateRow>> ForAlbum(
        CoverPriority priority,
        IEnumerable<CoverCandidateRow> candidatesOfAlbum)
    {
        CoverCandidateRow[] candidates = [.. candidatesOfAlbum];
        List<IReadOnlyList<CoverCandidateRow>> lists = [CoverResolver.OrderForAlbum(priority, candidates)];
        foreach (Guid trackId in candidates
            .Select(candidate => candidate.TrackId)
            .OfType<Guid>()
            .Distinct()
            .Order())
        {
            lists.Add(CoverResolver.OrderForTrack(trackId, priority, candidates));
        }

        // The tracks without a cover of their own share one list.
        lists.Add(CoverResolver.OrderForTrack(Guid.Empty, priority, candidates));

        HashSet<string> seen = new(StringComparer.Ordinal);
        return [.. lists.Where(list => list.Count > 0 && seen.Add(KeyOf(list)))];
    }

    /// <summary>
    /// The list of a track from its covers and the covers of its album.
    /// </summary>
    public static IReadOnlyList<CoverCandidateRow> ForTrack(
        Guid trackId,
        CoverPriority priority,
        IEnumerable<CoverCandidateRow> candidates)
    {
        return CoverResolver.OrderForTrack(trackId, priority, candidates);
    }

    /// <summary>
    /// Identifies a list by the pictures it shows in their order, independent of the covers that refer to them.
    /// </summary>
    public static string KeyOf(IEnumerable<CoverCandidateRow> ordered)
    {
        return string.Join(
            ';',
            ordered.Select(candidate =>
                candidate.MediaFileId.ToString("N") + (candidate.Source == CoverSource.File ? "f" : "e")));
    }
}
