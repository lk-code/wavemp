using WaveMP.Core.Users.Sessions;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class SessionMappingExtensions
{
    public static SessionDto ToDto(this ActiveSession session)
    {
        return new SessionDto
        {
            Id = session.Session.Id,
            Profile = session.User.ToProfileDto(),
            CreatedAt = session.Session.CreatedAt,
            ExpiresAt = session.Session.ExpiresAt,
        };
    }
}
