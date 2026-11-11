using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Streaming.Histories;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The play history of the profile of the current session. Clients report each play once it has played long enough.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me/history")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class HistoryController(IPlayHistoryService history) : ControllerBase
{
    private const string GetEntryRoute = "GetMyHistoryEntry";

    /// <summary>
    /// Records a play of a track. Clients send it once the track has played for half its duration or four minutes,
    /// whichever comes first, and may send it later (up to seven days) with the time the play started.
    /// </summary>
    /// <param name="request">The play.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="201">The new entry; Location points to it.</response>
    /// <response code="400">The request is invalid, for example playedAt in the future or older than seven days
    /// (code <c>request.invalid</c>).</response>
    /// <response code="404">The track does not exist (code <c>track.not_found</c>).</response>
    [HttpPost(Name = "CreateMyHistoryEntry")]
    [Consumes("application/json")]
    [ProducesResponseType<PlayHistoryEntryDto>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PlayHistoryEntryDto>> CreateMyHistoryEntryAsync(
        CreatePlayHistoryEntryDto request,
        CancellationToken cancellationToken)
    {
        PlayHistoryResult result = await history.RecordAsync(
            User.GetUserId(),
            request.TrackId,
            request.PlayedAt,
            cancellationToken);
        if (result.IsCreated)
        {
            return CreatedAtRoute(GetEntryRoute, new { id = result.Item.Entry.Id }, result.Item.ToDto());
        }

        if (result.Status == PlayHistoryStatus.InvalidPlayedAt)
        {
            ModelState.AddValidationError("playedAt", ValidationErrorCodeDto.InvalidValue);
            return ValidationProblem();
        }

        return this.Problem(ProblemCodeDto.TrackNotFound, StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Lists the plays of the current profile, the newest first. Plays of tracks that left the library are gone.
    /// </summary>
    /// <param name="offset">Number of entries to skip.</param>
    /// <param name="limit">Maximum number of entries of the page.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of entries.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListMyHistory")]
    [ProducesResponseType<PageDto<PlayHistoryEntryDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<PlayHistoryEntryDto>>> ListMyHistoryAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        Page<PlayHistoryItem> page = await history.ListAsync(User.GetUserId(), offset, limit, cancellationToken);
        return Ok(page.ToDto(item => item.ToDto()));
    }

    /// <summary>
    /// Returns an entry of the play history of the current profile.
    /// </summary>
    /// <param name="id">Id of the entry.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The entry.</response>
    /// <response code="404">The entry does not exist or belongs to another profile (code
    /// <c>history.not_found</c>).</response>
    [HttpGet("{id:guid}", Name = GetEntryRoute)]
    [ProducesResponseType<PlayHistoryEntryDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PlayHistoryEntryDto>> GetMyHistoryEntryAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        PlayHistoryItem? item = await history.GetAsync(User.GetUserId(), id, cancellationToken);
        return item is null
            ? this.Problem(ProblemCodeDto.HistoryNotFound, StatusCodes.Status404NotFound)
            : Ok(item.ToDto());
    }
}
