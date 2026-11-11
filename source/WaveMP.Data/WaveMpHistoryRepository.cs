using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Internal;

namespace WaveMP.Data;

/// <summary>
/// Keeps the standard names of the migrations history table (<c>__EFMigrationsHistory</c> with <c>MigrationId</c>
/// and <c>ProductVersion</c>). The snake_case naming convention would otherwise rename its columns as well, and
/// databases created before the convention was introduced could no longer be migrated. Customizing the history
/// table by deriving from the provider's history repository is the approach documented by EF Core.
/// </summary>
#pragma warning disable EF1001 // The provider's history repository is internal API, as in the EF Core documentation.
internal sealed class WaveMpHistoryRepository(HistoryRepositoryDependencies dependencies)
    : NpgsqlHistoryRepository(dependencies)
{
    protected override void ConfigureTable(EntityTypeBuilder<HistoryRow> history)
    {
        base.ConfigureTable(history);
        history.HasKey(row => row.MigrationId).HasName("PK___EFMigrationsHistory");
        history.Property(row => row.MigrationId).HasColumnName(nameof(HistoryRow.MigrationId));
        history.Property(row => row.ProductVersion).HasColumnName(nameof(HistoryRow.ProductVersion));
    }
}
#pragma warning restore EF1001
