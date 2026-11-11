using System.Security.Claims;
using WaveMP.Service.Authentication;

namespace WaveMP.Service.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Id of the profile of the authenticated session.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        return GetGuid(principal, WaveMpClaimTypes.Subject);
    }

    /// <summary>
    /// Id of the authenticated session.
    /// </summary>
    public static Guid GetSessionId(this ClaimsPrincipal principal)
    {
        return GetGuid(principal, WaveMpClaimTypes.SessionId);
    }

    private static Guid GetGuid(ClaimsPrincipal principal, string claimType)
    {
        return Guid.TryParse(principal.FindFirstValue(claimType), out Guid value)
            ? value
            : throw new InvalidOperationException($"The principal has no valid claim '{claimType}'.");
    }
}
