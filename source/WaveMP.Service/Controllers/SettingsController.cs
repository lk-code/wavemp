using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Services;
using WaveMP.Core.Settings.Validations;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.Controllers;

/// <summary>
/// Settings of the server, for admins only. Each group of settings is a resource of its own.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Admin)]
[Route("api/v1/settings")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class SettingsController(ILibrarySettingsService librarySettings) : ControllerBase
{
    /// <summary>
    /// Returns the settings of the media library.
    /// </summary>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The library settings.</response>
    [HttpGet("library", Name = "GetLibrarySettings")]
    [ProducesResponseType<LibrarySettingsDto>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<LibrarySettingsDto>> GetLibrarySettingsAsync(CancellationToken cancellationToken)
    {
        LibrarySettings settings = await librarySettings.GetAsync(cancellationToken);
        return Ok(settings.ToDto());
    }

    /// <summary>
    /// Changes the settings of the media library. Missing or null values stay unchanged. A new scan interval and the
    /// file watcher apply at once, the lists with the next scan.
    /// </summary>
    /// <param name="request">The changes.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The changed settings, normalized (e.g. extensions in lower case without dot).</response>
    /// <response code="400">The request is invalid (code <c>request.invalid</c>, errors by field).</response>
    [HttpPatch("library", Name = "UpdateLibrarySettings")]
    [Consumes("application/json")]
    [ProducesResponseType<LibrarySettingsDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<LibrarySettingsDto>> UpdateLibrarySettingsAsync(
        UpdateLibrarySettingsDto request,
        CancellationToken cancellationToken)
    {
        LibrarySettingsResult result = await librarySettings.UpdateAsync(request.ToCommand(), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Settings.ToDto());
        }

        foreach (SettingsValidationError error in result.Errors)
        {
            ModelState.AddModelError(error.Field.ToJsonName(), error.Reason.ToMessage());
        }

        return ValidationProblem();
    }
}
