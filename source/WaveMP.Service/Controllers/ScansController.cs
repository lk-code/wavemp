using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Jobs.Services;
using WaveMP.Core.Scanning.Scans;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.Controllers;

/// <summary>
/// Scans of the media folder, for admins only: start a scan, follow its progress and read the history.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Admin)]
[Route("api/v1/scans")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class ScansController(IScanService scans) : ControllerBase
{
    private const string GetScanRoute = "GetScan";

    /// <summary>
    /// Starts a scan of the whole media folder. The scan runs in the background; follow it with the current scan.
    /// </summary>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="202">The scan is queued; Location points to it.</response>
    /// <response code="409">A scan is already queued or running (code <c>scan.already_running</c>).</response>
    [HttpPost(Name = "StartScan")]
    [ProducesResponseType<ScanDto>(StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<ScanDto>> StartScanAsync(CancellationToken cancellationToken)
    {
        JobRunStartResult result = await scans.StartAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            return this.Problem(ProblemCodeDto.ScanAlreadyRunning, StatusCodes.Status409Conflict);
        }

        ScanDto scan = new ScanOverview(result.Run, null, null).ToDto();
        return AcceptedAtRoute(GetScanRoute, new { id = scan.Id }, scan);
    }

    /// <summary>
    /// Returns the queued or running scan with its progress.
    /// </summary>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The current scan.</response>
    /// <response code="404">No scan is queued or running (code <c>scan.not_running</c>).</response>
    [HttpGet("current", Name = "GetCurrentScan")]
    [ProducesResponseType<ScanDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ScanDto>> GetCurrentScanAsync(CancellationToken cancellationToken)
    {
        ScanOverview? scan = await scans.GetCurrentAsync(cancellationToken);
        return scan is null
            ? this.Problem(ProblemCodeDto.ScanNotRunning, StatusCodes.Status404NotFound)
            : Ok(scan.ToDto());
    }

    /// <summary>
    /// Lists the scans by the time they were requested, newest first by default, optionally filtered.
    /// </summary>
    /// <param name="offset">Number of scans to skip.</param>
    /// <param name="limit">Maximum number of scans of the page.</param>
    /// <param name="direction">Sort direction; ties are sorted by id.</param>
    /// <param name="status">Only scans in this state.</param>
    /// <param name="trigger">Only scans started this way.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of scans.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListScans")]
    [ProducesResponseType<PageDto<ScanDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<ScanDto>>> ListScansAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        [FromQuery] SortDirectionDto direction = SortDirectionDto.Desc,
        [FromQuery] JobRunStatusDto? status = null,
        [FromQuery] JobTriggerDto? trigger = null,
        CancellationToken cancellationToken = default)
    {
        if (!this.ValidateRunQuery(direction, status, trigger))
        {
            return ValidationProblem();
        }

        Page<ScanOverview> page = await scans.ListAsync(
            offset,
            limit,
            direction.ToEntity(),
            status?.ToEntity(),
            trigger?.ToEntity(),
            cancellationToken);
        return Ok(page.ToDto(scan => scan.ToDto()));
    }

    /// <summary>
    /// Returns a scan with its result, or its progress while it runs.
    /// </summary>
    /// <param name="id">Id of the scan.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The scan.</response>
    /// <response code="404">The scan does not exist (code <c>scan.not_found</c>).</response>
    [HttpGet("{id:guid}", Name = GetScanRoute)]
    [ProducesResponseType<ScanDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ScanDto>> GetScanAsync(Guid id, CancellationToken cancellationToken)
    {
        ScanOverview? scan = await scans.GetAsync(id, cancellationToken);
        return scan is null
            ? this.Problem(ProblemCodeDto.ScanNotFound, StatusCodes.Status404NotFound)
            : Ok(scan.ToDto());
    }
}
