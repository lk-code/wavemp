using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

/// <summary>
/// Backups keep the raw values of the tags, so texts have no length limit and lists are PostgreSQL arrays
/// (<c>text[]</c>).
/// </summary>
internal sealed class TagBackupConfiguration : IEntityTypeConfiguration<TagBackup>
{
    public void Configure(EntityTypeBuilder<TagBackup> builder)
    {
        builder.ToTable("tag_backups");
        builder.HasKey(backup => backup.Id);

        builder.Property(backup => backup.Id).ValueGeneratedNever();

        builder.HasIndex(backup => backup.MediaFileId);

        // The scan deletes files with ExecuteDelete as well; the database sets the reference to null itself.
        builder.HasOne(backup => backup.MediaFile)
            .WithMany()
            .HasForeignKey(backup => backup.MediaFileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
