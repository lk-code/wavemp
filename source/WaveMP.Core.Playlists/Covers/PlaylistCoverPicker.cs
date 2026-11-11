using WaveMP.Data.Projections;

namespace WaveMP.Core.Playlists.Covers;

/// <summary>
/// Picks the tracks whose covers a playlist without an own cover shows: tracks with a cover in the order of the
/// playlist, at most one per album (tracks without an album count as an album of their own).
/// </summary>
internal static class PlaylistCoverPicker
{
    public static IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> Pick(
        IEnumerable<PlaylistCoverCandidateRow> candidates,
        int count)
    {
        Dictionary<Guid, IReadOnlyList<Guid>> picked = [];
        foreach (IGrouping<Guid, PlaylistCoverCandidateRow> playlist in candidates.GroupBy(row => row.PlaylistId))
        {
            HashSet<Guid> albums = [];
            List<Guid> tracks = [];
            foreach (PlaylistCoverCandidateRow candidate in playlist.OrderBy(row => row.Position))
            {
                if (tracks.Count >= count)
                {
                    break;
                }

                if (candidate.HasCover && albums.Add(candidate.AlbumId ?? candidate.TrackId))
                {
                    tracks.Add(candidate.TrackId);
                }
            }

            picked[playlist.Key] = tracks;
        }

        return picked;
    }
}
