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
/// The years of the tracks of the library.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/years")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class YearsController(ITrackService tracks) : ControllerBase
{
    /// <summary>
    /// Lists the years of the tracks with the number of their tracks and albums.
    /// </summary>
    /// <param name="offset">Number of years to skip.</param>
    /// <param name="limit">Maximum number of years of the page.</param>
    /// <param name="direction">Sort direction; the newest year first by default.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of years.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListYears")]
    [ProducesResponseType<PageDto<YearDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<YearDto>>> ListYearsAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] SortDirectionDto? direction = null,
        CancellationToken cancellationToken = default)
    {
        this.ValidateEnum(direction, nameof(direction));
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        Page<YearRow> page = await tracks.ListYearsAsync(
            new YearListRequest(offset, limit, direction?.ToEntity()),
            cancellationToken);
        return Ok(page.ToDto(year => year.ToDto()));
    }
}
