using WaveMP.Data.Projections;

namespace WaveMP.Core.Library.Covers;

/// <summary>
/// The covers that may be shown for a track or an album, the best first, or that the track or album does not exist.
/// </summary>
/// <param name="OwnerExists">Whether the track or album exists.</param>
/// <param name="Ordered">The covers ordered by the cover rule; the first one that can be read is shown.</param>
public sealed record CoverCandidates(bool OwnerExists, IReadOnlyList<CoverCandidateRow> Ordered)
{
    public static CoverCandidates OwnerNotFound { get; } = new(false, []);
}
