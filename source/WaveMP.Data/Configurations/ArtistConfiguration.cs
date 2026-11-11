using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    /// <summary>
    /// Trigram index of the name for the fault-tolerant search (pg_trgm).
    /// </summary>
    public const string NameTrigramIndex = "ix_artists_name_trgm";

    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.ToTable("artists");
        builder.HasKey(artist => artist.Id);

        builder.Property(artist => artist.Id).ValueGeneratedNever();
        builder.Property(artist => artist.Name).HasMaxLength(LibraryColumns.TextMaxLength);
        builder.Property(artist => artist.NormalizedName).HasMaxLength(LibraryColumns.TextMaxLength);

        builder.HasIndex(artist => artist.NormalizedName).IsUnique();
        builder.HasIndex(artist => artist.Name)
            .HasDatabaseName(NameTrigramIndex)
            .HasMethod(LibraryColumns.TrigramIndexMethod)
            .HasOperators(LibraryColumns.TrigramOperators);
    }
}
