using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Users.Equalizers;
using WaveMP.Service.Dtos;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Controllers;

/// <summary>
/// The equalizer of the players.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/equalizer")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class EqualizerController : ControllerBase
{
    /// <summary>
    /// Returns the equalizer: its bands, the limits of the gains and the curves of the presets. A single resource the
    /// same for every profile; the settings of a profile are part of <c>GET /api/v1/me/settings</c>.
    /// </summary>
    /// <response code="200">The equalizer.</response>
    [HttpGet(Name = "GetEqualizer")]
    [ProducesResponseType<EqualizerDto>(StatusCodes.Status200OK, "application/json")]
    public ActionResult<EqualizerDto> GetEqualizer()
    {
        return Ok(EqualizerMappingExtensions.ToEqualizerDto(EqualizerBands.All, EqualizerPresets.All));
    }
}
