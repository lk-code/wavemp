using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Jobs.Services;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The background jobs, for admins only: their schedules and runs, and manual starts.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Admin)]
[Route("api/v1/jobs")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class JobsController(IJobService jobs) : ControllerBase
{
    private const string GetJobRunRoute = "GetJobRun";

    /// <summary>
    /// Lists the jobs with their state, schedule, next and last run.
    /// </summary>
    /// <param name="offset">Number of jobs to skip.</param>
    /// <param name="limit">Maximum number of jobs of the page.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of jobs, sorted by id.</response>
    /// <response code="400">Invalid paging parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListJobs")]
    [ProducesResponseType<PageDto<JobDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<JobDto>>> ListJobsAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        Page<JobOverview> page = await jobs.ListAsync(offset, limit, cancellationToken);
        return Ok(page.ToDto(job => job.ToDto()));
    }

    /// <summary>
    /// Starts a run of the job now. It runs in the background.
    /// </summary>
    /// <param name="id">Id of the job, e.g. <c>library-scan</c>.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="202">The run is queued; Location points to it.</response>
    /// <response code="404">The job does not exist (code <c>job.not_found</c>).</response>
    /// <response code="409">The job is already queued or running (code <c>job.already_running</c>).</response>
    [HttpPost("{id}/runs", Name = "StartJobRun")]
    [ProducesResponseType<JobRunDto>(StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<JobRunDto>> StartJobRunAsync(string id, CancellationToken cancellationToken)
    {
        JobRunStartResult result = await jobs.StartRunAsync(
            new JobRunRequest(id, JobRunTrigger.Manual),
            cancellationToken);
        return result.Error switch
        {
            JobRunStartError.NotFound => this.Problem(ProblemCodeDto.JobNotFound, StatusCodes.Status404NotFound),
            JobRunStartError.AlreadyActive => this.Problem(
                ProblemCodeDto.JobAlreadyRunning,
                StatusCodes.Status409Conflict),
            _ => AcceptedAtRoute(GetJobRunRoute, new { id, runId = result.Run!.Id }, result.Run.ToDto()),
        };
    }

    /// <summary>
    /// Lists the runs of the job by the time they were requested, newest first by default, optionally filtered.
    /// Runs older than 30 days and beyond the 100 newest are removed automatically.
    /// </summary>
    /// <param name="id">Id of the job, e.g. <c>library-scan</c>.</param>
    /// <param name="offset">Number of runs to skip.</param>
    /// <param name="limit">Maximum number of runs of the page.</param>
    /// <param name="direction">Sort direction; ties are sorted by id.</param>
    /// <param name="status">Only runs in this state.</param>
    /// <param name="trigger">Only runs started this way.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of runs.</response>
    /// <response code="400">Invalid query parameters (code <c>request.invalid</c>).</response>
    /// <response code="404">The job does not exist (code <c>job.not_found</c>).</response>
    [HttpGet("{id}/runs", Name = "ListJobRuns")]
    [ProducesResponseType<PageDto<JobRunDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PageDto<JobRunDto>>> ListJobRunsAsync(
        string id,
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

        if (!jobs.Exists(id))
        {
            return this.Problem(ProblemCodeDto.JobNotFound, StatusCodes.Status404NotFound);
        }

        JobRunListQuery query = new(id, offset, limit, direction.ToEntity(), status?.ToEntity(), trigger?.ToEntity());
        Page<JobRun> page = await jobs.ListRunsAsync(query, cancellationToken);
        return Ok(page.ToDto(run => run.ToDto()));
    }

    /// <summary>
    /// Returns a run of the job.
    /// </summary>
    /// <param name="id">Id of the job, e.g. <c>library-scan</c>.</param>
    /// <param name="runId">Id of the run.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The run.</response>
    /// <response code="404">No such job or run (code <c>job.not_found</c> or <c>job.run_not_found</c>).</response>
    [HttpGet("{id}/runs/{runId:guid}", Name = GetJobRunRoute)]
    [ProducesResponseType<JobRunDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<JobRunDto>> GetJobRunAsync(
        string id,
        Guid runId,
        CancellationToken cancellationToken)
    {
        if (!jobs.Exists(id))
        {
            return this.Problem(ProblemCodeDto.JobNotFound, StatusCodes.Status404NotFound);
        }

        JobRun? run = await jobs.GetRunAsync(id, runId, cancellationToken);
        return run is null
            ? this.Problem(ProblemCodeDto.JobRunNotFound, StatusCodes.Status404NotFound)
            : Ok(run.ToDto());
    }
}
