using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Library.Albums;
using WaveMP.Core.Library.Artists;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The artists of the library and their albums.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/artists")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class ArtistsController(IArtistService artists) : ControllerBase
{
    /// <summary>
    /// Lists the artists, sorted and optionally searched.
    /// </summary>
    /// <param name="offset">Number of artists to skip.</param>
    /// <param name="limit">Maximum number of artists of the page.</param>
    /// <param name="sort">Sort key; Name by default, Relevance with q. Relevance without q falls back to the default.
    /// </param>
    /// <param name="direction">Sort direction; descending by default for FirstSeenAt and Relevance, otherwise
    /// ascending.</param>
    /// <param name="q">Search text: only artists whose name contains it or is similar to it (small typos).</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of artists.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListArtists")]
    [ProducesResponseType<PageDto<ArtistDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<ArtistDto>>> ListArtistsAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] ArtistSortDto? sort = null,
        [FromQuery] SortDirectionDto? direction = null,
        [FromQuery, StringLength(LibraryLimits.TextMaxLength)] string? q = null,
        CancellationToken cancellationToken = default)
    {
        this.ValidateEnum(sort, nameof(sort));
        this.ValidateEnum(direction, nameof(direction));
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        Page<ArtistRow> page = await artists.ListAsync(
            new ArtistListRequest(offset, limit, sort?.ToEntity(), direction?.ToEntity(), q),
            cancellationToken);
        return Ok(page.ToDto(artist => artist.ToDto()));
    }

    /// <summary>
    /// Returns an artist.
    /// </summary>
    /// <param name="id">Id of the artist.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The artist.</response>
    /// <response code="404">The artist does not exist (code <c>artist.not_found</c>).</response>
    [HttpGet("{id:guid}", Name = "GetArtist")]
    [ProducesResponseType<ArtistDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ArtistDto>> GetArtistAsync(Guid id, CancellationToken cancellationToken)
    {
        ArtistRow? artist = await artists.GetAsync(id, cancellationToken);
        return artist is null
            ? this.Problem(ProblemCodeDto.ArtistNotFound, StatusCodes.Status404NotFound)
            : Ok(artist.ToDto());
    }

    /// <summary>
    /// Lists the albums of an artist: albums with the artist as album artist and albums with tracks of the artist.
    /// </summary>
    /// <param name="id">Id of the artist.</param>
    /// <param name="offset">Number of albums to skip.</param>
    /// <param name="limit">Maximum number of albums of the page.</param>
    /// <param name="sort">Sort key; Artist by default, Relevance with q. Relevance without q falls back to the default.
    /// </param>
    /// <param name="direction">Sort direction; descending by default for FirstSeenAt and Relevance, otherwise
    /// ascending.</param>
    /// <param name="q">Search text: only albums whose title or album artist contains it or is similar to it.</param>
    /// <param name="genre">Only albums with tracks of this genre, without regard to case.</param>
    /// <param name="year">Only albums of this year.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of albums.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    /// <response code="404">The artist does not exist (code <c>artist.not_found</c>).</response>
    [HttpGet("{id:guid}/albums", Name = "ListArtistAlbums")]
    [ProducesResponseType<PageDto<AlbumDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PageDto<AlbumDto>>> ListArtistAlbumsAsync(
        Guid id,
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] AlbumSortDto? sort = null,
        [FromQuery] SortDirectionDto? direction = null,
        [FromQuery, StringLength(LibraryLimits.TextMaxLength)] string? q = null,
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

        Page<AlbumRow>? page = await artists.ListAlbumsAsync(
            id,
            new AlbumListRequest(offset, limit, sort?.ToEntity(), direction?.ToEntity(), q, Genre: genre, Year: year),
            cancellationToken);
        return page is null
            ? this.Problem(ProblemCodeDto.ArtistNotFound, StatusCodes.Status404NotFound)
            : Ok(page.ToDto(album => album.ToDto()));
    }
}
