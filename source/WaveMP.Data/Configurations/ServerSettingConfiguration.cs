using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class ServerSettingConfiguration : IEntityTypeConfiguration<ServerSetting>
{
    public const int KeyMaxLength = 64;

    public void Configure(EntityTypeBuilder<ServerSetting> builder)
    {
        builder.ToTable("server_settings");
        builder.HasKey(setting => setting.Key);

        builder.Property(setting => setting.Key).HasMaxLength(KeyMaxLength);
        builder.Property(setting => setting.Value).HasColumnType("jsonb");
    }
}
