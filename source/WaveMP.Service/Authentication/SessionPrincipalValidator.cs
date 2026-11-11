using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WaveMP.Core.Users.Sessions;

namespace WaveMP.Service.Authentication;

/// <summary>
/// Accepts a validly signed token only while its session is active and belongs to the profile of the token, and adds
/// the current role of the profile. Ending the session, deleting the profile or changing its role applies at once.
/// </summary>
public sealed class SessionPrincipalValidator(ISessionService sessions)
{
    /// <summary>
    /// Handler of <see cref="JwtBearerEvents.OnTokenValidated"/>.
    /// </summary>
    public async Task ValidateAsync(TokenValidatedContext context)
    {
        if (context.Principal is null
            || !await ValidatePrincipalAsync(context.Principal, context.HttpContext.RequestAborted))
        {
            context.Fail("The session is not active.");
        }
    }

    /// <summary>
    /// Checks the session of the principal and adds the role claim. Returns <c>false</c> if the principal must be
    /// rejected.
    /// </summary>
    public async Task<bool> ValidatePrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (principal.Identity is not ClaimsIdentity identity
            || !Guid.TryParse(principal.FindFirstValue(WaveMpClaimTypes.SessionId), out Guid sessionId)
            || !Guid.TryParse(principal.FindFirstValue(WaveMpClaimTypes.Subject), out Guid userId))
        {
            return false;
        }

        ActiveSession? session = await sessions.GetActiveAsync(sessionId, cancellationToken);
        if (session is null || session.User.Id != userId)
        {
            return false;
        }

        // The role always comes from the database, never from the token.
        foreach (Claim claim in identity.FindAll(WaveMpClaimTypes.Role).ToList())
        {
            identity.RemoveClaim(claim);
        }

        identity.AddClaim(new Claim(WaveMpClaimTypes.Role, session.User.Role.ToString()));
        return true;
    }
}
