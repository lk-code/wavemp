using WaveMP.Core.Users.Sessions;

namespace WaveMP.Service.Tokens;

/// <summary>
/// Issues the token of a session.
/// </summary>
public interface ITokenIssuer
{
    string Issue(ActiveSession session);
}
