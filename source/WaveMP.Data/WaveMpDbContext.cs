using Microsoft.EntityFrameworkCore;

namespace WaveMP.Data;

/// <summary>
/// Database context of WaveMP. Entities are added in later roadmap steps.
/// </summary>
public class WaveMpDbContext(DbContextOptions<WaveMpDbContext> options) : DbContext(options);
