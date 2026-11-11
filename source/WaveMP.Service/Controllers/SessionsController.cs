using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaveMP.Core.Users.Sessions;
using WaveMP.Service.Authentication;
using WaveMP.Service.Dtos;
using WaveMP.Service.Extensions;
using WaveMP.Service.Mappings;

namespace WaveMP.Service.Controllers;

/// <summary>
/// Sessions: selecting a profile creates a session with a token, switching the profile ends it.
/// </summary>
[ApiController]
[Route("api/v1/sessions")]
public sealed class SessionsController(ISessionService sessions, ITokenIssuer tokenIssuer) : ControllerBase
{
    private const string CurrentSessionRoute = "GetCurrentSession";

    /// <summary>
    /// Selects a profile: creates a session and returns its token.
    /// </summary>
    /// <param name="request">The profile to select.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="201">The session with its token; Location points to the current session.</response>
    /// <response code="400">The request is invalid (code <c>request.invalid</c>).</response>
    /// <response code="404">The profile does not exist (code <c>profile.not_found</c>).</response>
    [HttpPost(Name = "CreateSession")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType<CreatedSessionDto>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<CreatedSessionDto>> CreateSessionAsync(
        CreateSessionDto request,
        CancellationToken cancellationToken)
    {
        ActiveSession? session = await sessions.CreateAsync(request.ProfileId, cancellationToken);
        if (session is null)
        {
            return this.Problem(ProblemCodeDto.ProfileNotFound, StatusCodes.Status404NotFound);
        }

        CreatedSessionDto created = new() { Token = tokenIssuer.Issue(session), Session = session.ToDto() };
        return CreatedAtRoute(CurrentSessionRoute, null, created);
    }

    /// <summary>
    /// Returns the session of the token, with its profile.
    /// </summary>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The current session.</response>
    /// <response code="401">No valid token, or the session has ended (code <c>auth.unauthorized</c>).</response>
    [HttpGet("current", Name = CurrentSessionRoute)]
    [Authorize]
    [ProducesResponseType<SessionDto>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<SessionDto>> GetCurrentSessionAsync(CancellationToken cancellationToken)
    {
        ActiveSession? session = await sessions.GetActiveAsync(User.GetSessionId(), cancellationToken);
        if (session is null)
        {
            // The session ended after the token was checked.
            return this.Problem(ProblemCodeDto.AuthUnauthorized, StatusCodes.Status401Unauthorized);
        }

        return Ok(session.ToDto());
    }

    /// <summary>
    /// Ends the session of the token (switching the profile); the token is no longer accepted.
    /// </summary>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The session has ended.</response>
    /// <response code="401">No valid token, or the session has ended (code <c>auth.unauthorized</c>).</response>
    [HttpDelete("current", Name = "DeleteCurrentSession")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> DeleteCurrentSessionAsync(CancellationToken cancellationToken)
    {
        await sessions.DeleteAsync(User.GetSessionId(), cancellationToken);
        return NoContent();
    }
}
