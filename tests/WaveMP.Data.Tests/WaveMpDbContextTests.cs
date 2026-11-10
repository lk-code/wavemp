using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;

namespace WaveMP.Data.Tests;

[TestFixture]
public class WaveMpDbContextTests
{
    [Test]
    public void Model_ComparedToMigrations_HasNoPendingChanges()
    {
        using WaveMpDbContext dbContext = new WaveMpDbContextFactory().CreateDbContext([]);

        bool hasPendingChanges = dbContext.Database.HasPendingModelChanges();

        hasPendingChanges.Should().BeFalse("every model change needs a migration (dotnet ef migrations add)");
    }

    [Test]
    public void Migrations_Always_ContainInitialCreate()
    {
        using WaveMpDbContext dbContext = new WaveMpDbContextFactory().CreateDbContext([]);

        IEnumerable<string> migrations = dbContext.Database.GetMigrations();

        migrations.Should().ContainSingle(migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }
}
