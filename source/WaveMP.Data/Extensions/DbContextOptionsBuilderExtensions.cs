using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace WaveMP.Data.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures PostgreSQL with the conventions of WaveMP: snake_case names for tables, columns, keys and indexes,
    /// while the migrations history table keeps its standard names. Used by the service and by the design-time
    /// factory, so that migrations match the runtime model.
    /// </summary>
    public static DbContextOptionsBuilder UseWaveMpNpgsql(
        this DbContextOptionsBuilder builder,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? npgsql = null)
    {
        return builder
            .UseNpgsql(connectionString, npgsql)
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IHistoryRepository, WaveMpHistoryRepository>();
    }
}
