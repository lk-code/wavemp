using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Name of the unique index on <see cref="User.NormalizedName"/>; a violation means the name is taken.
    /// </summary>
    public const string NormalizedNameIndex = "ix_users_normalized_name";

    public const int NameMaxLength = 50;
    public const int InitialsMaxLength = 16;
    public const int ColorLength = 7;
    public const int RoleMaxLength = 16;
    public const int PinHashMaxLength = 256;

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Name).HasMaxLength(NameMaxLength);
        builder.Property(user => user.NormalizedName).HasMaxLength(NameMaxLength);
        builder.Property(user => user.Initials).HasMaxLength(InitialsMaxLength);
        builder.Property(user => user.Color).HasMaxLength(ColorLength);
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(RoleMaxLength);
        builder.Property(user => user.PinHash).HasMaxLength(PinHashMaxLength);

        builder.HasIndex(user => user.NormalizedName)
            .IsUnique()
            .HasDatabaseName(NormalizedNameIndex);

        // Only one built-in admin. The filter is raw SQL, so it names the column itself.
        builder.HasIndex(user => user.IsBuiltIn)
            .IsUnique()
            .HasFilter("is_built_in")
            .HasDatabaseName("ix_users_is_built_in");

        builder.HasOne(user => user.Settings)
            .WithOne(settings => settings.User)
            .HasForeignKey<UserSettings>(settings => settings.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.Sessions)
            .WithOne(session => session.User)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
