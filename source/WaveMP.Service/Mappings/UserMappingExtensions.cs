using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class UserMappingExtensions
{
    public static ProfileDto ToProfileDto(this User user)
    {
        return new ProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Initials = user.Initials,
            Color = user.Color,
            Role = user.Role.ToDto(),
            IsBuiltIn = user.IsBuiltIn,
        };
    }

    public static UserDto ToUserDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Initials = user.Initials,
            Color = user.Color,
            Role = user.Role.ToDto(),
            IsBuiltIn = user.IsBuiltIn,
            CreatedAt = user.CreatedAt,
        };
    }

    public static RoleDto ToDto(this UserRole role)
    {
        return role switch
        {
            UserRole.Admin => RoleDto.Admin,
            UserRole.User => RoleDto.User,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role."),
        };
    }

    public static UserRole ToEntity(this RoleDto role)
    {
        return role switch
        {
            RoleDto.Admin => UserRole.Admin,
            RoleDto.User => UserRole.User,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role."),
        };
    }
}
