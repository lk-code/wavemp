using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace WaveMP.Data.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Word similarity (pg_trgm) from which the library search finds a text despite typos. The default of pg_trgm (0.6)
    /// misses single typos in short words, e.g. "Closng" for "Closing" (0.57).
    /// </summary>
    public const double SearchSimilarityThreshold = 0.5;

    /// <summary>
    /// Configures PostgreSQL with the conventions of WaveMP: snake_case names for tables, columns, keys and indexes,
    /// while the migrations history table keeps its standard names. Every connection sets the threshold of the word
    /// similarity (<see cref="SearchSimilarityThreshold"/>), so the operator <c>&lt;%</c> of the search keeps using
    /// the trigram indexes. Used by the service and by the design-time factory, so that migrations match the runtime
    /// model.
    /// </summary>
    public static DbContextOptionsBuilder UseWaveMpNpgsql(
        this DbContextOptionsBuilder builder,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? npgsql = null)
    {
        return builder
            .UseNpgsql(WithSearchThreshold(connectionString), npgsql)
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IHistoryRepository, WaveMpHistoryRepository>();
    }

    private static string WithSearchThreshold(string connectionString)
    {
        NpgsqlConnectionStringBuilder connection = new(connectionString);
        string option = string.Create(
            CultureInfo.InvariantCulture,
            $"-c pg_trgm.word_similarity_threshold={SearchSimilarityThreshold}");
        connection.Options = string.IsNullOrWhiteSpace(connection.Options)
            ? option
            : $"{connection.Options} {option}";
        return connection.ConnectionString;
    }
}
