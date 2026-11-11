using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class ServerSecretConfiguration : IEntityTypeConfiguration<ServerSecret>
{
    public const int NameMaxLength = 64;

    public void Configure(EntityTypeBuilder<ServerSecret> builder)
    {
        builder.ToTable("server_secrets");
        builder.HasKey(secret => secret.Name);

        builder.Property(secret => secret.Name).HasMaxLength(NameMaxLength);
    }
}
