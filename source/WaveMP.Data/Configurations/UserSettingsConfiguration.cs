using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public const int LanguageMaxLength = 16;

    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("user_settings");
        builder.HasKey(settings => settings.UserId);

        builder.Property(settings => settings.Language)
            .HasConversion<string>()
            .HasMaxLength(LanguageMaxLength);
    }
}
