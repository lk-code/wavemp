using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
    public const int NameMaxLength = 100;

    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        builder.ToTable("playlists");
        builder.HasKey(playlist => playlist.Id);

        // Playlists are created with an explicit UUIDv7.
        builder.Property(playlist => playlist.Id).ValueGeneratedNever();
        builder.Property(playlist => playlist.Name).HasMaxLength(NameMaxLength);
        builder.Property(playlist => playlist.NormalizedName).HasMaxLength(NameMaxLength);

        // The playlists of a user by name.
        builder.HasIndex(playlist => new { playlist.UserId, playlist.NormalizedName, playlist.Id });

        builder.HasOne(playlist => playlist.User)
            .WithMany()
            .HasForeignKey(playlist => playlist.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(playlist => playlist.Entries)
            .WithOne(entry => entry.Playlist)
            .HasForeignKey(entry => entry.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
