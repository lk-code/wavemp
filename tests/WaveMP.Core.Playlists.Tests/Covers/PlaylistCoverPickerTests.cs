using AwesomeAssertions;
using WaveMP.Core.Playlists.Covers;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Playlists.Tests.Covers;

[TestFixture]
public class PlaylistCoverPickerTests
{
    private static readonly Guid _playlist = Guid.NewGuid();

    [Test]
    public void Pick_Always_TakesTracksWithACoverFromDifferentAlbumsInTheOrderOfThePlaylist()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        PlaylistCoverCandidateRow[] candidates =
        [
            Candidate(3, album: second),
            Candidate(0, album: first),
            Candidate(1, album: first),
            Candidate(2, album: null, hasCover: false),
            Candidate(4, album: null),
        ];

        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> picked = PlaylistCoverPicker.Pick(candidates, 4);

        picked[_playlist].Should().Equal(candidates[1].TrackId, candidates[0].TrackId, candidates[4].TrackId);
    }

    [Test]
    public void Pick_MoreAlbumsThanCovers_StopsAtTheCount()
    {
        PlaylistCoverCandidateRow[] candidates = [.. Enumerable.Range(0, 6).Select(index => Candidate(index))];

        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> picked = PlaylistCoverPicker.Pick(candidates, 4);

        picked[_playlist].Should().Equal(candidates.Take(4).Select(candidate => candidate.TrackId));
    }

    [Test]
    public void Pick_SameTrackTwiceWithoutAlbum_TakesItOnce()
    {
        var track = Guid.NewGuid();
        PlaylistCoverCandidateRow[] candidates =
        [
            new(_playlist, 0, track, null, true),
            new(_playlist, 1, track, null, true),
        ];

        PlaylistCoverPicker.Pick(candidates, 4)[_playlist].Should().Equal(track);
    }

    [Test]
    public void Pick_SeveralPlaylists_PicksForEach()
    {
        var other = Guid.NewGuid();
        PlaylistCoverCandidateRow own = Candidate(0);
        PlaylistCoverCandidateRow foreign = Candidate(0) with { PlaylistId = other };

        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> picked = PlaylistCoverPicker.Pick([own, foreign], 4);

        picked[_playlist].Should().Equal(own.TrackId);
        picked[other].Should().Equal(foreign.TrackId);
    }

    private static PlaylistCoverCandidateRow Candidate(int position)
    {
        return Candidate(position, Guid.NewGuid());
    }

    private static PlaylistCoverCandidateRow Candidate(int position, Guid? album, bool hasCover = true)
    {
        return new PlaylistCoverCandidateRow(_playlist, position, Guid.NewGuid(), album, hasCover);
    }
}
