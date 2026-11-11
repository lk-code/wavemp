using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using WaveMP.Core.Streaming.Signatures;
using WaveMP.Core.Users.Sessions;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.Signatures;

/// <summary>
/// Authenticates requests of signed media URLs (query <c>uid</c>, <c>sid</c>, <c>exp</c> and <c>sig</c>), for
/// players that cannot send a token header. The signature must match the kind of media of the endpoint (see
/// <see cref="SignedMediaAttribute"/>), the track of the route, the user, the session and the expiry; the URL must not
/// have expired, and the session must still be active and belong to the user. Ending the session or deleting the
/// profile therefore ends all its URLs at once. Like the session token, the role always comes from the database.
/// Requests without <c>sig</c> are left to the other schemes.
/// </summary>
public sealed class MediaSignatureAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IMediaUrlSigner signer,
    ISessionService sessions,
    TimeProvider timeProvider) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        IQueryCollection query = Request.Query;
        if (!query.ContainsKey(MediaSignatureDefaults.SignatureParameter))
        {
            return AuthenticateResult.NoResult();
        }

        SignedMediaAttribute? media = Context.GetEndpoint()?.Metadata.GetMetadata<SignedMediaAttribute>();
        if (media is null)
        {
            return AuthenticateResult.Fail("The endpoint does not accept signed media URLs.");
        }

        if (!TryReadClaims(query, media.Resource, out MediaUrlClaims? claims, out string? signature))
        {
            return AuthenticateResult.Fail("The signed media URL is malformed.");
        }

        MediaUrlVerification verification = signer.Verify(claims, signature, timeProvider.GetUtcNow());
        if (verification != MediaUrlVerification.Valid)
        {
            return AuthenticateResult.Fail($"The signed media URL is not valid ({verification}).");
        }

        ActiveSession? session = await sessions.GetActiveAsync(claims.SessionId, Context.RequestAborted);
        if (session is null || session.User.Id != claims.UserId)
        {
            return AuthenticateResult.Fail("The session of the signed media URL is not active.");
        }

        ClaimsIdentity identity = new(
            [
                new Claim(WaveMpClaimTypes.Subject, claims.UserId.ToString("D", CultureInfo.InvariantCulture)),
                new Claim(WaveMpClaimTypes.SessionId, claims.SessionId.ToString("D", CultureInfo.InvariantCulture)),
                new Claim(WaveMpClaimTypes.Role, session.User.Role.ToString()),
            ],
            Scheme.Name,
            WaveMpClaimTypes.Subject,
            WaveMpClaimTypes.Role);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    private bool TryReadClaims(
        IQueryCollection query,
        MediaResource resource,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out MediaUrlClaims? claims,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? signature)
    {
        claims = null;
        signature = Single(query, MediaSignatureDefaults.SignatureParameter);
        string? trackId = Convert.ToString(
            Context.GetRouteValue(MediaSignatureDefaults.TrackIdRouteValue),
            CultureInfo.InvariantCulture);
        string? userId = Single(query, MediaSignatureDefaults.UserIdParameter);
        string? sessionId = Single(query, MediaSignatureDefaults.SessionIdParameter);
        string? expires = Single(query, MediaSignatureDefaults.ExpiresParameter);

        if (signature is null
            || !Guid.TryParse(trackId, CultureInfo.InvariantCulture, out Guid track)
            || !Guid.TryParse(userId, CultureInfo.InvariantCulture, out Guid user)
            || !Guid.TryParse(sessionId, CultureInfo.InvariantCulture, out Guid session)
            || !long.TryParse(expires, NumberStyles.None, CultureInfo.InvariantCulture, out long seconds)
            || seconds > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
        {
            return false;
        }

        claims = new MediaUrlClaims(resource, track, user, session, DateTimeOffset.FromUnixTimeSeconds(seconds));
        return true;
    }

    private static string? Single(IQueryCollection query, string name)
    {
        return query.TryGetValue(name, out StringValues values) && values.Count == 1 ? values[0] : null;
    }
}
