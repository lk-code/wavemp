using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class PlaylistEntryConfiguration : IEntityTypeConfiguration<PlaylistEntry>
{
    public void Configure(EntityTypeBuilder<PlaylistEntry> builder)
    {
        builder.ToTable("playlist_entries");
        builder.HasKey(entry => entry.Id);

        // Entries are created with an explicit UUIDv7.
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        // The entries of a playlist in their order. Not unique: moving entries shifts the positions of the others one
        // by one, which would collide with a unique index in between.
        builder.HasIndex(entry => new { entry.PlaylistId, entry.Position, entry.Id });

        // Removing a track removes its entries; also the base of filters by track later on.
        builder.HasIndex(entry => entry.TrackId);

        // The scan deletes tracks with ExecuteDelete; the database removes their entries itself.
        builder.HasOne(entry => entry.Track)
            .WithMany()
            .HasForeignKey(entry => entry.TrackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
