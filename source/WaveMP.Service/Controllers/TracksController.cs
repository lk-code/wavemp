using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Library.Covers;
using WaveMP.Core.Library.Tracks;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The tracks of the library, their covers and, for admins, their files.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/tracks")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class TracksController(ITrackService tracks, ICoverService covers) : ControllerBase
{
    /// <summary>
    /// Lists the tracks, sorted and optionally searched and filtered.
    /// </summary>
    /// <param name="offset">Number of tracks to skip.</param>
    /// <param name="limit">Maximum number of tracks of the page.</param>
    /// <param name="sort">Sort key; AlbumArtist by default, Relevance with q. Relevance without q falls back to the
    /// default.</param>
    /// <param name="direction">Sort direction of the key; descending by default for FirstSeenAt and Relevance,
    /// otherwise ascending. Disc and track number always ascend.</param>
    /// <param name="q">Search text: only tracks whose title, artist or album contains it or is similar to it (small
    /// typos).</param>
    /// <param name="artistId">Only tracks of this artist or on albums of this album artist.</param>
    /// <param name="albumId">Only tracks of this album.</param>
    /// <param name="genre">Only tracks of this genre, without regard to case.</param>
    /// <param name="year">Only tracks of this year.</param>
    /// <param name="hasAudio">Only tracks with (true) or without (false) an audio file.</param>
    /// <param name="hasVideo">Only tracks with (true) or without (false) a video file.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of tracks.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListTracks")]
    [ProducesResponseType<PageDto<TrackDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<TrackDto>>> ListTracksAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] TrackSortDto? sort = null,
        [FromQuery] SortDirectionDto? direction = null,
        [FromQuery, StringLength(LibraryLimits.TextMaxLength)] string? q = null,
        [FromQuery] Guid? artistId = null,
        [FromQuery] Guid? albumId = null,
        [FromQuery, StringLength(LibraryLimits.GenreMaxLength)] string? genre = null,
        [FromQuery, Range(LibraryLimits.MinYear, LibraryLimits.MaxYear)] int? year = null,
        [FromQuery] bool? hasAudio = null,
        [FromQuery] bool? hasVideo = null,
        CancellationToken cancellationToken = default)
    {
        this.ValidateEnum(sort, nameof(sort));
        this.ValidateEnum(direction, nameof(direction));
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        TrackListRequest request = new(
            offset,
            limit,
            sort?.ToEntity(),
            direction?.ToEntity(),
            q,
            artistId,
            albumId,
            genre,
            year,
            hasAudio,
            hasVideo);
        Page<TrackRow> page = await tracks.ListAsync(request, cancellationToken);
        return Ok(page.ToDto(track => track.ToDto()));
    }

    /// <summary>
    /// Returns a track.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The track.</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>).</response>
    [HttpGet("{id:guid}", Name = "GetTrack")]
    [ProducesResponseType<TrackDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<TrackDto>> GetTrackAsync(Guid id, CancellationToken cancellationToken)
    {
        TrackRow? track = await tracks.GetAsync(id, cancellationToken);
        return track is null
            ? this.Problem(ProblemCodeDto.TrackNotFound, StatusCodes.Status404NotFound)
            : Ok(track.ToDto());
    }

    /// <summary>
    /// Returns the cover of a track as picture. By default the cover of its album comes before its own cover; if the
    /// preferred cover is missing, the other one is used, and finally the cover of another track of the album. The
    /// picture is delivered as stored (JPEG, PNG, WebP, GIF or BMP); scaled sizes follow later.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The picture.</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>) or has no cover (code
    /// <c>cover.not_found</c>).</response>
    [HttpGet("{id:guid}/cover", Name = "GetTrackCover")]
    [ProducesResponseType<Stream>(
        StatusCodes.Status200OK,
        ImageFormats.Jpeg,
        ImageFormats.Png,
        ImageFormats.WebP,
        ImageFormats.Gif,
        ImageFormats.Bmp)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetTrackCoverAsync(Guid id, CancellationToken cancellationToken)
    {
        CoverResult result = await covers.GetTrackCoverAsync(id, cancellationToken);
        return this.Cover(result, ProblemCodeDto.TrackNotFound);
    }

    /// <summary>
    /// Lists the files of a track, for admins only: audio, video and its own pictures, also duplicates of lower
    /// quality, with their technical details.
    /// </summary>
    /// <param name="id">Id of the track.</param>
    /// <param name="offset">Number of files to skip.</param>
    /// <param name="limit">Maximum number of files of the page.</param>
    /// <param name="sort">Sort key; Kind (audio, video, pictures, the primary file first) by default.</param>
    /// <param name="direction">Sort direction; ascending by default.</param>
    /// <param name="kind">Only files of this kind.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of files.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>).</response>
    [HttpGet("{id:guid}/files", Name = "ListTrackFiles")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [ProducesResponseType<PageDto<MediaFileDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PageDto<MediaFileDto>>> ListTrackFilesAsync(
        Guid id,
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] MediaFileSortDto? sort = null,
        [FromQuery] SortDirectionDto? direction = null,
        [FromQuery] MediaFileKindDto? kind = null,
        CancellationToken cancellationToken = default)
    {
        this.ValidateEnum(sort, nameof(sort));
        this.ValidateEnum(direction, nameof(direction));
        this.ValidateEnum(kind, nameof(kind));
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        Page<MediaFile>? page = await tracks.ListFilesAsync(
            id,
            new TrackFileListRequest(offset, limit, sort?.ToEntity(), direction?.ToEntity(), kind?.ToEntity()),
            cancellationToken);
        return page is null
            ? this.Problem(ProblemCodeDto.TrackNotFound, StatusCodes.Status404NotFound)
            : Ok(page.ToDto(file => file.ToDto()));
    }
}
