using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Library.Albums;
using WaveMP.Core.Library.Covers;
using WaveMP.Core.Library.Tracks;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The albums of the library, their tracks and covers.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/albums")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class AlbumsController(IAlbumService albums, ICoverService covers) : ControllerBase
{
    /// <summary>
    /// Lists the albums, sorted and optionally searched and filtered.
    /// </summary>
    /// <param name="offset">Number of albums to skip.</param>
    /// <param name="limit">Maximum number of albums of the page.</param>
    /// <param name="sort">Sort key; Artist by default, Relevance with q. Relevance without q falls back to the default.
    /// </param>
    /// <param name="direction">Sort direction; descending by default for FirstSeenAt and Relevance, otherwise
    /// ascending.</param>
    /// <param name="q">Search text: only albums whose title or album artist contains it or is similar to it (small
    /// typos).</param>
    /// <param name="artistId">Only albums with this album artist or with tracks of this artist.</param>
    /// <param name="genre">Only albums with tracks of this genre, without regard to case.</param>
    /// <param name="year">Only albums of this year.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of albums.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListAlbums")]
    [ProducesResponseType<PageDto<AlbumDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<AlbumDto>>> ListAlbumsAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] AlbumSortDto? sort = null,
        [FromQuery] SortDirectionDto? direction = null,
        [FromQuery, StringLength(LibraryLimits.TextMaxLength)] string? q = null,
        [FromQuery] Guid? artistId = null,
        [FromQuery, StringLength(LibraryLimits.GenreMaxLength)] string? genre = null,
        [FromQuery, Range(LibraryLimits.MinYear, LibraryLimits.MaxYear)] int? year = null,
        CancellationToken cancellationToken = default)
    {
        this.ValidateEnum(sort, nameof(sort));
        this.ValidateEnum(direction, nameof(direction));
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        Page<AlbumRow> page = await albums.ListAsync(
            new AlbumListRequest(offset, limit, sort?.ToEntity(), direction?.ToEntity(), q, artistId, genre, year),
            cancellationToken);
        return Ok(page.ToDto(album => album.ToDto()));
    }

    /// <summary>
    /// Returns an album.
    /// </summary>
    /// <param name="id">Id of the album.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The album.</response>
    /// <response code="404">The album does not exist (code <c>album.not_found</c>).</response>
    [HttpGet("{id:guid}", Name = "GetAlbum")]
    [ProducesResponseType<AlbumDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AlbumDto>> GetAlbumAsync(Guid id, CancellationToken cancellationToken)
    {
        AlbumRow? album = await albums.GetAsync(id, cancellationToken);
        return album is null
            ? this.Problem(ProblemCodeDto.AlbumNotFound, StatusCodes.Status404NotFound)
            : Ok(album.ToDto());
    }

    /// <summary>
    /// Lists the tracks of an album, by default in the order of disc and track number.
    /// </summary>
    /// <param name="id">Id of the album.</param>
    /// <param name="offset">Number of tracks to skip.</param>
    /// <param name="limit">Maximum number of tracks of the page.</param>
    /// <param name="sort">Sort key; Album (disc and track number) by default, Relevance with q. Relevance without q
    /// falls back to the default.</param>
    /// <param name="direction">Sort direction; descending by default for FirstSeenAt and Relevance, otherwise
    /// ascending.</param>
    /// <param name="q">Search text: only tracks whose title, artist or album contains it or is similar to it.</param>
    /// <param name="hasAudio">Only tracks with (true) or without (false) an audio file.</param>
    /// <param name="hasVideo">Only tracks with (true) or without (false) a video file.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of tracks.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    /// <response code="404">The album does not exist (code <c>album.not_found</c>).</response>
    [HttpGet("{id:guid}/tracks", Name = "ListAlbumTracks")]
    [ProducesResponseType<PageDto<TrackDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PageDto<TrackDto>>> ListAlbumTracksAsync(
        Guid id,
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] TrackSortDto? sort = null,
        [FromQuery] SortDirectionDto? direction = null,
        [FromQuery, StringLength(LibraryLimits.TextMaxLength)] string? q = null,
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
            HasAudio: hasAudio,
            HasVideo: hasVideo);
        Page<TrackRow>? page = await albums.ListTracksAsync(id, request, cancellationToken);
        return page is null
            ? this.Problem(ProblemCodeDto.AlbumNotFound, StatusCodes.Status404NotFound)
            : Ok(page.ToDto(track => track.ToDto()));
    }

    /// <summary>
    /// Returns the cover of an album as picture: the picture in its folder, otherwise the cover of one of its tracks.
    /// The picture is delivered as stored (JPEG, PNG, WebP, GIF or BMP); scaled sizes follow later.
    /// </summary>
    /// <param name="id">Id of the album.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The picture.</response>
    /// <response code="404">The album does not exist (code <c>album.not_found</c>) or has no cover (code
    /// <c>cover.not_found</c>).</response>
    [HttpGet("{id:guid}/cover", Name = "GetAlbumCover")]
    [ProducesResponseType<Stream>(
        StatusCodes.Status200OK,
        ImageFormats.Jpeg,
        ImageFormats.Png,
        ImageFormats.WebP,
        ImageFormats.Gif,
        ImageFormats.Bmp)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetAlbumCoverAsync(Guid id, CancellationToken cancellationToken)
    {
        CoverResult result = await covers.GetAlbumCoverAsync(id, cancellationToken);
        return this.Cover(result, ProblemCodeDto.AlbumNotFound);
    }
}
