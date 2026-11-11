using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class QueryMappingExtensions
{
    public static PageDto<TDto> ToDto<T, TDto>(this Page<T> page, Func<T, TDto> map)
    {
        return new PageDto<TDto>
        {
            Items = [.. page.Items.Select(map)],
            Offset = page.Offset,
            Limit = page.Limit,
            Total = page.Total,
        };
    }

    public static UserSortField ToEntity(this UserSortDto sort)
    {
        return sort switch
        {
            UserSortDto.Name => UserSortField.Name,
            UserSortDto.CreatedAt => UserSortField.CreatedAt,
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unknown sort key."),
        };
    }

    public static SortDirection ToEntity(this SortDirectionDto direction)
    {
        return direction switch
        {
            SortDirectionDto.Asc => SortDirection.Asc,
            SortDirectionDto.Desc => SortDirection.Desc,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown sort direction."),
        };
    }
}
