using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    /// <summary>
    /// Trigram index of the title for the fault-tolerant search (pg_trgm).
    /// </summary>
    public const string TitleTrigramIndex = "ix_tracks_title_trgm";

    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("tracks");
        builder.HasKey(track => track.Id);

        builder.Property(track => track.Id).ValueGeneratedNever();
        builder.Property(track => track.Title).HasMaxLength(LibraryColumns.TextMaxLength);
        builder.Property(track => track.Genre).HasMaxLength(LibraryColumns.GenreMaxLength);
        builder.Property(track => track.MetadataSource)
            .HasConversion<string>()
            .HasMaxLength(LibraryColumns.EnumMaxLength);

        // One track per base name and folder, without regard to the case of the base name.
        builder.HasIndex(track => new { track.DirectoryPath, track.NormalizedBaseName }).IsUnique();
        builder.HasIndex(track => track.FirstSeenAt);
        builder.HasIndex(track => track.Title)
            .HasDatabaseName(TitleTrigramIndex)
            .HasMethod(LibraryColumns.TrigramIndexMethod)
            .HasOperators(LibraryColumns.TrigramOperators);

        builder.HasOne(track => track.Artist)
            .WithMany()
            .HasForeignKey(track => track.ArtistId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(track => track.Album)
            .WithMany(album => album.Tracks)
            .HasForeignKey(track => track.AlbumId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
