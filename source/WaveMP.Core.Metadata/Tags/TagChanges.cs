namespace WaveMP.Core.Metadata.Tags;

/// <summary>
/// Changes of the tags of an audio file. A field that is <c>null</c> stays as it is; a <see cref="TagChange{T}"/>
/// with the value <c>null</c> removes the field.
/// </summary>
public sealed record TagChanges
{
    /// <summary>
    /// No changes.
    /// </summary>
    public static TagChanges None { get; } = new();

    public TagChange<string?>? Title { get; init; }

    /// <summary>
    /// The artist; replaces all artists of the file.
    /// </summary>
    public TagChange<string?>? Artist { get; init; }

    public TagChange<string?>? Album { get; init; }

    /// <summary>
    /// The album artist; replaces all album artists of the file.
    /// </summary>
    public TagChange<string?>? AlbumArtist { get; init; }

    public TagChange<int?>? Year { get; init; }

    /// <summary>
    /// The number of the track; the number of tracks stays, unless the number is removed.
    /// </summary>
    public TagChange<int?>? TrackNumber { get; init; }

    /// <summary>
    /// The number of the disc; the number of discs stays, unless the number is removed.
    /// </summary>
    public TagChange<int?>? DiscNumber { get; init; }

    /// <summary>
    /// The genre; replaces all genres of the file.
    /// </summary>
    public TagChange<string?>? Genre { get; init; }

    /// <summary>
    /// Whether nothing changes.
    /// </summary>
    public bool IsEmpty => Title is null
        && Artist is null
        && Album is null
        && AlbumArtist is null
        && Year is null
        && TrackNumber is null
        && DiscNumber is null
        && Genre is null;
}
