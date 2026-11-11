using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class PlayHistoryEntryConfiguration : IEntityTypeConfiguration<PlayHistoryEntry>
{
    public void Configure(EntityTypeBuilder<PlayHistoryEntry> builder)
    {
        builder.ToTable("play_history");
        builder.HasKey(entry => entry.Id);

        // Entries are created with an explicit UUIDv7.
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        // The history of a user, the newest first.
        builder.HasIndex(entry => new { entry.UserId, entry.PlayedAt, entry.Id })
            .IsDescending(false, true, true);
        builder.HasIndex(entry => entry.TrackId);

        builder.HasOne(entry => entry.User)
            .WithMany()
            .HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The scan deletes tracks with ExecuteDelete as well; the database removes their history itself.
        builder.HasOne(entry => entry.Track)
            .WithMany()
            .HasForeignKey(entry => entry.TrackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
