using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Transcoding.Services;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.Controllers;

/// <summary>
/// State of the transcoding, for admins only: the hardware acceleration and the running transcodes.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Admin)]
[Route("api/v1/transcoding")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class TranscodingController(ITranscodingService transcoding) : ControllerBase
{
    /// <summary>
    /// Returns whether transcoding works, the result of the detection of the graphics hardware (Intel Quick Sync) and
    /// how many transcodes run.
    /// </summary>
    /// <response code="200">The state of the transcoding.</response>
    [HttpGet("status", Name = "GetTranscodingStatus")]
    [ProducesResponseType<TranscodingStatusDto>(StatusCodes.Status200OK, "application/json")]
    public ActionResult<TranscodingStatusDto> GetTranscodingStatus()
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(transcoding.GetStatus().ToDto());
    }

    /// <summary>
    /// Lists the running transcodes, newest first.
    /// </summary>
    /// <param name="offset">Number of transcodes to skip.</param>
    /// <param name="limit">Maximum number of transcodes of the page.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">A page of running transcodes.</response>
    /// <response code="400">Invalid paging (code <c>request.invalid</c>).</response>
    [HttpGet("jobs", Name = "ListTranscodeJobs")]
    [ProducesResponseType<PageDto<TranscodeJobDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<TranscodeJobDto>>> ListTranscodeJobsAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        Page<TranscodeJobItem> page = await transcoding.ListJobsAsync(
            offset,
            limit,
            User.GetUserId(),
            cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return Ok(page.ToDto(item => item.ToDto()));
    }

    /// <summary>
    /// Stops a running transcode. A player that still needs its segments starts a new one.
    /// </summary>
    /// <param name="jobId">Id of the transcode.</param>
    /// <response code="204">The transcode was stopped.</response>
    /// <response code="404">No transcode with the id is running (code <c>transcode.job_not_found</c>).</response>
    [HttpDelete("jobs/{jobId:guid}", Name = "CancelTranscodeJob")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public IActionResult CancelTranscodeJob(Guid jobId)
    {
        return transcoding.CancelJob(jobId)
            ? NoContent()
            : this.Problem(ProblemCodeDto.TranscodeJobNotFound, StatusCodes.Status404NotFound);
    }
}
