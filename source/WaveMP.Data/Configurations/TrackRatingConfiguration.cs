using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class TrackRatingConfiguration : IEntityTypeConfiguration<TrackRating>
{
    public const string RatingCheckConstraint = "ck_track_ratings_rating";

    public const int MinRating = 1;
    public const int MaxRating = 5;

    public void Configure(EntityTypeBuilder<TrackRating> builder)
    {
        // A track without rating has no row; raw SQL, so it names the column itself.
        builder.ToTable("track_ratings", table => table.HasCheckConstraint(
            RatingCheckConstraint,
            $"rating BETWEEN {MinRating} AND {MaxRating}"));
        builder.HasKey(rating => new { rating.UserId, rating.TrackId });

        // The ratings of a user, the best and then the latest first.
        builder.HasIndex(rating => new { rating.UserId, rating.Rating, rating.UpdatedAt })
            .IsDescending(false, true, true);
        builder.HasIndex(rating => rating.TrackId);

        builder.HasOne(rating => rating.User)
            .WithMany()
            .HasForeignKey(rating => rating.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The scan deletes tracks with ExecuteDelete; the database removes their ratings itself.
        builder.HasOne(rating => rating.Track)
            .WithMany()
            .HasForeignKey(rating => rating.TrackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
