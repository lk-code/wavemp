using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Playlists.Histories;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The automatic list "Recently played" of the current profile, built from its play history.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me/recently-played")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class RecentlyPlayedController(IRecentlyPlayedService recentlyPlayed) : ControllerBase
{
    /// <summary>
    /// Lists the tracks the current profile played last, newest first: every track once, at its last play, at most
    /// 100 tracks by default. Tracks that left the library are gone.
    /// </summary>
    /// <param name="offset">Number of tracks to skip.</param>
    /// <param name="limit">Maximum number of tracks of the page.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of tracks.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListMyRecentlyPlayedTracks")]
    [ProducesResponseType<PageDto<RecentlyPlayedTrackDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<RecentlyPlayedTrackDto>>> ListMyRecentlyPlayedTracksAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        Page<RecentlyPlayedItem> page = await recentlyPlayed.ListAsync(
            User.GetUserId(),
            offset,
            limit,
            cancellationToken);
        return Ok(page.ToDto(item => item.ToDto()));
    }
}
