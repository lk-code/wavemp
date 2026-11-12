using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public const int LanguageMaxLength = 16;
    public const int ReplayGainMaxLength = 16;
    public const int PlayerViewModeMaxLength = 16;
    public const int EqualizerPresetMaxLength = 16;

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

        // The columns below give existing rows the defaults of a new profile. The entity always writes its values
        // (ValueGeneratedNever), so the default of a column never replaces a value set in the code. For
        // CrossfadeAlbumsGapless and SleepTimerFadeSeconds the defaults of the CLR types (false, 0) differ from the
        // column defaults; EF would otherwise leave them out on insert.
        builder.Property(settings => settings.EqualizerEnabled)
            .HasDefaultValue(false)
            .ValueGeneratedNever();

        builder.Property(settings => settings.EqualizerPreset)
            .HasConversion<string>()
            .HasMaxLength(EqualizerPresetMaxLength)
            .HasDefaultValue(EqualizerPreset.Flat)
            .ValueGeneratedNever();

        builder.Property(settings => settings.EqualizerPreampDb)
            .HasDefaultValue(0d)
            .ValueGeneratedNever();

        builder.Property(settings => settings.EqualizerCustomGainsDb)
            .HasColumnType("double precision[]")
            .HasDefaultValueSql("'{0,0,0,0,0,0,0,0,0,0}'::double precision[]")
            .ValueGeneratedNever();

        builder.Property(settings => settings.CrossfadeSeconds)
            .HasDefaultValue(0)
            .ValueGeneratedNever();

        builder.Property(settings => settings.CrossfadeAlbumsGapless)
            .HasDefaultValue(true)
            .ValueGeneratedNever();

        builder.Property(settings => settings.SleepTimerFadeSeconds)
            .HasDefaultValue(10)
            .ValueGeneratedNever();
    }
}
