using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Collection.HomePages;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The home page of the current profile.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me/home")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class HomeController(IHomePageService homePages) : ControllerBase
{
    /// <summary>
    /// Returns all rows of the home page of the current profile in one call: own playlists, recently played tracks,
    /// recently added albums, favorites and top rated tracks. Every row is the first page of its list.
    /// </summary>
    /// <param name="limit">Maximum number of items per row.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The rows of the home page.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "GetMyHome")]
    [ProducesResponseType<HomeDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<HomeDto>> GetMyHomeAsync(
        [FromQuery, Range(1, HomePageLimits.MaxLimit)] int limit = HomePageLimits.DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        HomePage page = await homePages.GetAsync(User.GetUserId(), limit, cancellationToken);
        return Ok(page.ToDto());
    }
}
