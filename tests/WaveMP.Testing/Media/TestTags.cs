namespace WaveMP.Testing.Media;

/// <summary>
/// Tags written into synthetic test media with <c>-metadata</c>. <c>null</c> values are not written.
/// </summary>
public sealed record TestTags
{
    public string? Title { get; init; }

    public string? Artist { get; init; }

    public string? Album { get; init; }

    public string? AlbumArtist { get; init; }

    public string? Track { get; init; }

    public string? Disc { get; init; }

    public string? Date { get; init; }

    public string? Genre { get; init; }

    internal IEnumerable<string> ToArguments()
    {
        (string Key, string? Value)[] values =
        [
            ("title", Title),
            ("artist", Artist),
            ("album", Album),
            ("album_artist", AlbumArtist),
            ("track", Track),
            ("disc", Disc),
            ("date", Date),
            ("genre", Genre),
        ];

        foreach ((string key, string? value) in values)
        {
            if (value is not null)
            {
                yield return "-metadata";
                yield return $"{key}={value}";
            }
        }
    }
}
