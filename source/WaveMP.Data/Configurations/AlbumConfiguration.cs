using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        builder.ToTable("albums");
        builder.HasKey(album => album.Id);

        builder.Property(album => album.Id).ValueGeneratedNever();
        builder.Property(album => album.Title).HasMaxLength(LibraryColumns.TextMaxLength);
        builder.Property(album => album.NormalizedTitle).HasMaxLength(LibraryColumns.TextMaxLength);
        builder.Property(album => album.AlbumKey).HasMaxLength(LibraryColumns.AlbumKeyLength);

        builder.HasIndex(album => album.AlbumKey).IsUnique();
        builder.HasIndex(album => album.AlbumArtistId);

        builder.HasOne(album => album.AlbumArtist)
            .WithMany()
            .HasForeignKey(album => album.AlbumArtistId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
