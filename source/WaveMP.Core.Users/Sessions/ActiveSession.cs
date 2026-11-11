using WaveMP.Data.Entities;

namespace WaveMP.Core.Users.Sessions;

/// <summary>
/// A session that has not expired, with the user it belongs to.
/// </summary>
public sealed record ActiveSession(Session Session, User User);
