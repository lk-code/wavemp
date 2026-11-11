using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Users.Settings;
using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Controllers;

/// <summary>
/// Data of the profile of the current session.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class MeController(IUserSettingsService settings) : ControllerBase
{
    /// <summary>
    /// Returns the settings of the current profile.
    /// </summary>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The settings.</response>
    /// <response code="404">The profile was deleted in the meantime (code <c>user.not_found</c>).</response>
    [HttpGet("settings", Name = "GetMySettings")]
    [ProducesResponseType<UserSettingsDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<UserSettingsDto>> GetMySettingsAsync(CancellationToken cancellationToken)
    {
        UserSettings? result = await settings.GetAsync(User.GetUserId(), cancellationToken);
        return result is null
            ? this.Problem(ProblemCodeDto.UserNotFound, StatusCodes.Status404NotFound)
            : Ok(result.ToDto());
    }

    /// <summary>
    /// Changes the settings of the current profile. Missing or null values stay unchanged.
    /// </summary>
    /// <param name="request">The changes.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The changed settings.</response>
    /// <response code="400">The request is invalid (code <c>request.invalid</c>).</response>
    /// <response code="404">The profile was deleted in the meantime (code <c>user.not_found</c>).</response>
    [HttpPatch("settings", Name = "UpdateMySettings")]
    [Consumes("application/json")]
    [ProducesResponseType<UserSettingsDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDto>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<UserSettingsDto>> UpdateMySettingsAsync(
        UpdateUserSettingsDto request,
        CancellationToken cancellationToken)
    {
        UpdateUserSettingsCommand command = new(request.Language?.ToEntity());
        UserSettings? result = await settings.UpdateAsync(User.GetUserId(), command, cancellationToken);
        return result is null
            ? this.Problem(ProblemCodeDto.UserNotFound, StatusCodes.Status404NotFound)
            : Ok(result.ToDto());
    }
}
