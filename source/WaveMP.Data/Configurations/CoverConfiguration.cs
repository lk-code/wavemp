using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class CoverConfiguration : IEntityTypeConfiguration<Cover>
{
    public const string OwnerCheckConstraint = "ck_covers_owner";

    public void Configure(EntityTypeBuilder<Cover> builder)
    {
        // A cover belongs to an album or to a track, never to both. Raw SQL, so it names the columns itself.
        builder.ToTable("covers", table => table.HasCheckConstraint(
            OwnerCheckConstraint,
            "(album_id IS NULL) <> (track_id IS NULL)"));
        builder.HasKey(cover => cover.Id);

        builder.Property(cover => cover.Id).ValueGeneratedNever();
        builder.Property(cover => cover.Source).HasConversion<string>().HasMaxLength(LibraryColumns.EnumMaxLength);
        builder.Property(cover => cover.MimeType).HasMaxLength(LibraryColumns.TechnicalMaxLength);

        builder.HasOne(cover => cover.Album)
            .WithMany(album => album.Covers)
            .HasForeignKey(cover => cover.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cover => cover.Track)
            .WithMany(track => track.Covers)
            .HasForeignKey(cover => cover.TrackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cover => cover.MediaFile)
            .WithMany()
            .HasForeignKey(cover => cover.MediaFileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
