using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files");
        builder.HasKey(file => file.Id);

        builder.Property(file => file.Id).ValueGeneratedNever();
        builder.Property(file => file.Extension).HasMaxLength(LibraryColumns.ExtensionMaxLength);
        builder.Property(file => file.Kind).HasConversion<string>().HasMaxLength(LibraryColumns.EnumMaxLength);
        builder.Property(file => file.Issue).HasConversion<string>().HasMaxLength(LibraryColumns.EnumMaxLength);
        builder.Property(file => file.Format).HasMaxLength(LibraryColumns.TechnicalMaxLength);
        builder.Property(file => file.Codec).HasMaxLength(LibraryColumns.TechnicalMaxLength);
        builder.Property(file => file.AudioCodec).HasMaxLength(LibraryColumns.TechnicalMaxLength);
        builder.Property(file => file.PixelFormat).HasMaxLength(LibraryColumns.TechnicalMaxLength);
        builder.Property(file => file.EmbeddedPictureMimeType).HasMaxLength(LibraryColumns.TechnicalMaxLength);

        builder.ComplexProperty(file => file.Tags, tags =>
        {
            tags.Property(tag => tag.Title).HasMaxLength(LibraryColumns.TextMaxLength);
            tags.Property(tag => tag.TitleSort).HasMaxLength(LibraryColumns.TextMaxLength);
            tags.Property(tag => tag.Artist).HasMaxLength(LibraryColumns.TextMaxLength);
            tags.Property(tag => tag.Artists).HasMaxLength(LibraryColumns.TextMaxLength);
            tags.Property(tag => tag.ArtistSort).HasMaxLength(LibraryColumns.TextMaxLength);
            tags.Property(tag => tag.AlbumArtist).HasMaxLength(LibraryColumns.TextMaxLength);
            tags.Property(tag => tag.AlbumArtistSort).HasMaxLength(LibraryColumns.TextMaxLength);
            tags.Property(tag => tag.Album).HasMaxLength(LibraryColumns.TextMaxLength);
            tags.Property(tag => tag.AlbumSort).HasMaxLength(LibraryColumns.TextMaxLength);
            tags.Property(tag => tag.Genre).HasMaxLength(LibraryColumns.GenreMaxLength);
            tags.Property(tag => tag.Genres).HasMaxLength(LibraryColumns.TextMaxLength);
            tags.Property(tag => tag.Composer).HasMaxLength(LibraryColumns.TextMaxLength);
            tags.Property(tag => tag.MusicBrainzTrackId).HasMaxLength(LibraryColumns.TechnicalMaxLength);
            tags.Property(tag => tag.MusicBrainzReleaseId).HasMaxLength(LibraryColumns.TechnicalMaxLength);
        });

        builder.HasIndex(file => file.RelativePath).IsUnique();
        builder.HasIndex(file => file.DirectoryPath);

        builder.HasOne(file => file.Track)
            .WithMany(track => track.Files)
            .HasForeignKey(file => file.TrackId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
