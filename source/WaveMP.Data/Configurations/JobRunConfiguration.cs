using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Configurations;

internal sealed class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public const int JobIdMaxLength = 64;
    public const int EnumMaxLength = 32;
    public const int ErrorDetailMaxLength = 4000;

    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.ToTable("job_runs");
        builder.HasKey(run => run.Id);

        // Runs are created with an explicit UUIDv7.
        builder.Property(run => run.Id).ValueGeneratedNever();
        builder.Property(run => run.JobId).HasMaxLength(JobIdMaxLength);
        builder.Property(run => run.Trigger).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(run => run.ErrorCode).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(run => run.ErrorDetail).HasMaxLength(ErrorDetailMaxLength);
        builder.Property(run => run.Result).HasColumnType("jsonb");

        builder.HasIndex(run => new { run.JobId, run.QueuedAt });
        builder.HasIndex(run => run.Status);
    }
}
