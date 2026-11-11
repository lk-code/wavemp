using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WaveMP.Data.Extensions;

namespace WaveMP.Data.Tests.Extensions;

[TestFixture]
public class DbContextOptionsBuilderExtensionsTests
{
    [TestCase("Host=db;Database=wavemp;Username=wavemp;Password=secret", "")]
    [TestCase(
        "Host=db;Database=wavemp;Username=wavemp;Password=secret;Options=-c statement_timeout=1000",
        "-c statement_timeout=1000 ")]
    public void UseWaveMpNpgsql_Always_SetsTheWordSimilarityThresholdOfTheSearch(
        string connectionString,
        string otherOptions)
    {
        DbContextOptionsBuilder<WaveMpDbContext> builder = new();
        builder.UseWaveMpNpgsql(connectionString);
        using WaveMpDbContext dbContext = new(builder.Options);

        NpgsqlConnectionStringBuilder connection = new(dbContext.Database.GetConnectionString());

        connection.Options.Should().Be(otherOptions + "-c pg_trgm.word_similarity_threshold=0.5");
        connection.Host.Should().Be("db");
    }
}
