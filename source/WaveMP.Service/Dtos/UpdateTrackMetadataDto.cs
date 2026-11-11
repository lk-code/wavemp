using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WaveMP.Service.Queries;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Changes the metadata of a track. Missing members stay unchanged; null or an empty text removes the value from the
/// tags, and the library derives it from the folder and file name again.
/// </summary>
/// <remarks>
/// Whether a member is present is recorded when it is set: the JSON serializer only sets the members of the request,
/// also when they are null.
/// </remarks>
public sealed class UpdateTrackMetadataDto
{
    /// <summary>
    /// New title.
    /// </summary>
    [StringLength(LibraryLimits.NameMaxLength)]
    public string? Title
    {
        get;
        init
        {
            field = value;
            HasTitle = true;
        }
    }

    /// <summary>
    /// New artist; replaces all artists of the file.
    /// </summary>
    [StringLength(LibraryLimits.NameMaxLength)]
    public string? Artist
    {
        get;
        init
        {
            field = value;
            HasArtist = true;
        }
    }

    /// <summary>
    /// New album.
    /// </summary>
    [StringLength(LibraryLimits.NameMaxLength)]
    public string? Album
    {
        get;
        init
        {
            field = value;
            HasAlbum = true;
        }
    }

    /// <summary>
    /// New album artist; replaces all album artists of the file.
    /// </summary>
    [StringLength(LibraryLimits.NameMaxLength)]
    public string? AlbumArtist
    {
        get;
        init
        {
            field = value;
            HasAlbumArtist = true;
        }
    }

    /// <summary>
    /// New year.
    /// </summary>
    [Range(LibraryLimits.MinYear, LibraryLimits.MaxYear)]
    public int? Year
    {
        get;
        init
        {
            field = value;
            HasYear = true;
        }
    }

    /// <summary>
    /// New number of the track on its disc; the number of tracks in the file stays.
    /// </summary>
    [Range(1, LibraryLimits.MaxNumber)]
    public int? TrackNumber
    {
        get;
        init
        {
            field = value;
            HasTrackNumber = true;
        }
    }

    /// <summary>
    /// New number of the disc; the number of discs in the file stays.
    /// </summary>
    [Range(1, LibraryLimits.MaxNumber)]
    public int? DiscNumber
    {
        get;
        init
        {
            field = value;
            HasDiscNumber = true;
        }
    }

    /// <summary>
    /// New genre; replaces all genres of the file.
    /// </summary>
    [StringLength(LibraryLimits.GenreMaxLength)]
    public string? Genre
    {
        get;
        init
        {
            field = value;
            HasGenre = true;
        }
    }

    [JsonIgnore]
    public bool HasTitle { get; private set; }

    [JsonIgnore]
    public bool HasArtist { get; private set; }

    [JsonIgnore]
    public bool HasAlbum { get; private set; }

    [JsonIgnore]
    public bool HasAlbumArtist { get; private set; }

    [JsonIgnore]
    public bool HasYear { get; private set; }

    [JsonIgnore]
    public bool HasTrackNumber { get; private set; }

    [JsonIgnore]
    public bool HasDiscNumber { get; private set; }

    [JsonIgnore]
    public bool HasGenre { get; private set; }
}
