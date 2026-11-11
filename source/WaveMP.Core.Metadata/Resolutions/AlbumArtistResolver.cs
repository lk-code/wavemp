using WaveMP.Core.Metadata.Texts;

namespace WaveMP.Core.Metadata.Resolutions;

/// <summary>
/// Decides the album artist of the tracks of one folder, per album: the most frequent album artist of the tracks; if
/// none has one, their artist if all share it; several artists make a compilation. No display texts such as
/// "Various Artists" are invented; clients show their own text for compilations and albums without artist.
/// </summary>
public static class AlbumArtistResolver
{
    /// <summary>
    /// The album of each track, in the order of <paramref name="tracks"/>; <c>null</c> for tracks without album.
    /// </summary>
    public static IReadOnlyList<AlbumAssignment?> Resolve(IReadOnlyList<ResolvedTrackMetadata> tracks)
    {
        var assignments = new AlbumAssignment?[tracks.Count];
        IEnumerable<IGrouping<string, int>> albums = Enumerable.Range(0, tracks.Count)
            .Where(index => MetadataText.ToKey(tracks[index].Album) is not null)
            .GroupBy(index => MetadataText.ToKey(tracks[index].Album)!);

        foreach (IGrouping<string, int> album in albums)
        {
            ResolvedTrackMetadata[] members = [.. album.Select(index => tracks[index])];
            string title = members[0].Album!;
            int? year = members.Select(track => track.Year).FirstOrDefault(value => value is not null);

            string? albumArtist = MostFrequent(members.Select(track => track.AlbumArtist));
            bool isCompilation = false;
            if (albumArtist is null)
            {
                string[] artists = [.. members
                    .Select(track => track.Artist)
                    .OfType<string>()
                    .DistinctBy(MetadataText.ToKey)];
                isCompilation = artists.Length > 1;
                albumArtist = artists.Length == 1 ? artists[0] : null;
            }

            AlbumAssignment assignment = new(
                title,
                albumArtist,
                isCompilation,
                year,
                AlbumKeys.Create(title, albumArtist, isCompilation));
            foreach (int index in album)
            {
                assignments[index] = assignment;
            }
        }

        return assignments;
    }

    private static string? MostFrequent(IEnumerable<string?> values)
    {
        return values
            .OfType<string>()
            .GroupBy(value => MetadataText.ToKey(value))
            .OrderByDescending(group => group.Count())
            .Select(group => group.First())
            .FirstOrDefault();
    }
}
