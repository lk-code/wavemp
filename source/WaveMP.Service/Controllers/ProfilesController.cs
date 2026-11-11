using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Users.Users;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;
using WaveMP.Service.Queries;

namespace WaveMP.Service.Controllers;

/// <summary>
/// Profiles for the profile selection, available without a session.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/profiles")]
public sealed class ProfilesController(IUserService users) : ControllerBase
{
    /// <summary>
    /// Lists the profiles in the order they were created; the built-in admin profile comes last.
    /// </summary>
    /// <param name="offset">Number of profiles to skip.</param>
    /// <param name="limit">Maximum number of profiles of the page.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">One page of profiles.</response>
    /// <response code="400">Invalid paging parameters (code <c>request.invalid</c>).</response>
    [HttpGet(Name = "ListProfiles")]
    [ProducesResponseType<PageDto<ProfileDto>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PageDto<ProfileDto>>> ListProfilesAsync(
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        [FromQuery, Range(1, PageLimits.MaxLimit)] int limit = PageLimits.DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        Page<User> page = await users.ListProfilesAsync(offset, limit, cancellationToken);
        return Ok(page.ToDto(user => user.ToProfileDto()));
    }
}
