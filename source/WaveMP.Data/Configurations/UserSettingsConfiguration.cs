using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public const int LanguageMaxLength = 16;
    public const int ReplayGainMaxLength = 16;
    public const int PlayerViewModeMaxLength = 16;

    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("user_settings");
        builder.HasKey(settings => settings.UserId);

        builder.Property(settings => settings.Language)
            .HasConversion<string>()
            .HasMaxLength(LanguageMaxLength);

        // The column default gives existing rows "Off". The entity always writes its value (ValueGeneratedNever),
        // so the default of the column never replaces a value set in the code.
        builder.Property(settings => settings.ReplayGain)
            .HasConversion<string>()
            .HasMaxLength(ReplayGainMaxLength)
            .HasDefaultValue(ReplayGainMode.Off)
            .ValueGeneratedNever();

        // The column default gives existing rows "Video". The default of the enum (Cover) differs from it, so the
        // entity always writes its value (ValueGeneratedNever); otherwise EF would leave out "Cover" on insert.
        builder.Property(settings => settings.PlayerViewMode)
            .HasConversion<string>()
            .HasMaxLength(PlayerViewModeMaxLength)
            .HasDefaultValue(PlayerViewMode.Video)
            .ValueGeneratedNever();
    }
}
