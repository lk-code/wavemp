using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public const string TargetCheckConstraint = "ck_favorites_target";

    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        // Exactly the id of the type is set. Raw SQL, so it names the columns and the stored enum names itself.
        builder.ToTable("favorites", table => table.HasCheckConstraint(
            TargetCheckConstraint,
            "(type = 'Track' AND track_id IS NOT NULL AND album_id IS NULL AND artist_id IS NULL)"
            + " OR (type = 'Album' AND album_id IS NOT NULL AND track_id IS NULL AND artist_id IS NULL)"
            + " OR (type = 'Artist' AND artist_id IS NOT NULL AND track_id IS NULL AND album_id IS NULL)"));
        builder.HasKey(favorite => favorite.Id);

        // Favorites are created with an explicit UUIDv7.
        builder.Property(favorite => favorite.Id).ValueGeneratedNever();
        builder.Property(favorite => favorite.Type)
            .HasConversion<string>()
            .HasMaxLength(LibraryColumns.EnumMaxLength);

        // A target is a favorite of a user at most once; the filters are raw SQL.
        builder.HasIndex(favorite => new { favorite.UserId, favorite.TrackId })
            .IsUnique()
            .HasFilter("track_id IS NOT NULL");
        builder.HasIndex(favorite => new { favorite.UserId, favorite.AlbumId })
            .IsUnique()
            .HasFilter("album_id IS NOT NULL");
        builder.HasIndex(favorite => new { favorite.UserId, favorite.ArtistId })
            .IsUnique()
            .HasFilter("artist_id IS NOT NULL");

        // The favorites of a user, the newest first.
        builder.HasIndex(favorite => new { favorite.UserId, favorite.CreatedAt, favorite.Id })
            .IsDescending(false, true, true);

        // Removing a target removes its favorites.
        builder.HasIndex(favorite => favorite.TrackId);
        builder.HasIndex(favorite => favorite.AlbumId);
        builder.HasIndex(favorite => favorite.ArtistId);

        builder.HasOne(favorite => favorite.User)
            .WithMany()
            .HasForeignKey(favorite => favorite.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The scan deletes tracks, albums and artists with ExecuteDelete; the database removes their favorites itself.
        builder.HasOne(favorite => favorite.Track)
            .WithMany()
            .HasForeignKey(favorite => favorite.TrackId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(favorite => favorite.Album)
            .WithMany()
            .HasForeignKey(favorite => favorite.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(favorite => favorite.Artist)
            .WithMany()
            .HasForeignKey(favorite => favorite.ArtistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
