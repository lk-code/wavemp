using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Library.Tracks;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The genres of the tracks of the library.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/genres")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class GenresController(ITrackService tracks) : ControllerBase
{
    /// <summary>
    /// Lists the genres of the tracks with the number of their tracks and albums. Genres that differ only in case are
    /// one genre; use its name as genre filter of the track and album lists.
    /// </summary>
    /// <param name="offset">Number of genres to skip.</param>
    /// <param name="limit">Maximum number of genres of the page.</param>
    /// <param name="sort">Sort key; Name by default.</param>
    /// <param name="direction">Sort direction; descending by default for TrackCount, otherwise ascending.</param>
    /// <param name="q">Search text: only genres that contain it or are similar to it.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of genres.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListGenres")]
    [ProducesResponseType<PageDto<GenreDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<GenreDto>>> ListGenresAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] GenreSortDto? sort = null,
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

        Page<GenreRow> page = await tracks.ListGenresAsync(
            new GenreListRequest(offset, limit, sort?.ToEntity(), direction?.ToEntity(), q),
            cancellationToken);
        return Ok(page.ToDto(genre => genre.ToDto()));
    }
}
