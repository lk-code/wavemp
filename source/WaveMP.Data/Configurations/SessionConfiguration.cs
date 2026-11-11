using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");
        builder.HasKey(session => session.Id);

        // Sessions are created with an explicit UUIDv7, which is also the token's session id.
        builder.Property(session => session.Id).ValueGeneratedNever();

        builder.HasIndex(session => session.UserId);
        builder.HasIndex(session => session.ExpiresAt);
    }
}
