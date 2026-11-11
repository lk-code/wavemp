using System.Globalization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WaveMP.Core.Users.Sessions;

namespace WaveMP.Service.Authentication;

/// <summary>
/// Issues session tokens as JWT signed with HMAC-SHA256. The token only names the profile (<c>sub</c>) and the
/// session (<c>sid</c>); every request checks the session and reads the role from the database.
/// </summary>
public sealed class JwtTokenIssuer(SigningKeyStore keys) : ITokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public string Issue(ActiveSession session)
    {
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = TokenDefaults.Issuer,
            Audience = TokenDefaults.Audience,
            Claims = new Dictionary<string, object>
            {
                [WaveMpClaimTypes.Subject] = session.User.Id.ToString("D", CultureInfo.InvariantCulture),
                [WaveMpClaimTypes.SessionId] = session.Session.Id.ToString("D", CultureInfo.InvariantCulture),
            },
            IssuedAt = session.Session.CreatedAt.UtcDateTime,
            NotBefore = session.Session.CreatedAt.UtcDateTime,
            Expires = session.Session.ExpiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(keys.Key, TokenDefaults.Algorithm),
        };

        return _handler.CreateToken(descriptor);
    }
}
